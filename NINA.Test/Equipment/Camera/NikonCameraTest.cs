#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using Nikon;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System.Reflection;

namespace NINA.Test.Equipment.Camera;

[TestFixture]
public class NikonCameraTest {

    [TestCase(29.9, true)]
    [TestCase(30.0, true)]
    [TestCase(30.1, false)]
    public void UsesAutomaticShutter_AtThirtySecondBoundary_SelectsExpectedMode(double exposureTime, bool expected) {
        bool actual = (bool)typeof(NikonCamera)
            .GetMethod("UsesAutomaticShutter", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { exposureTime })!;

        actual.Should().Be(expected);
    }

    [Test]
    public async Task WaitUntilExposureIsReady_WhenImageArrivesBeforeCaptureCompletes_CompletesFromImageReady() {
        var sut = CreateCamera();
        SetDownloadExposure(sut);

        Task waitTask = sut.WaitUntilExposureIsReady(CancellationToken.None);
        RaiseImageReady(sut);

        await waitTask.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task WaitUntilExposureIsReady_WhenCaptureCompletesBeforeImageArrives_WaitsForImageReady() {
        var sut = CreateCamera();
        SetDownloadExposure(sut);

        Task waitTask = sut.WaitUntilExposureIsReady(CancellationToken.None);
        RaiseCaptureComplete(sut);

        Task firstCompletion = await Task.WhenAny(waitTask, Task.Delay(TimeSpan.FromMilliseconds(100)));
        firstCompletion.Should().NotBeSameAs(waitTask);

        RaiseImageReady(sut);

        await waitTask.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task WaitUntilExposureIsReady_WhenCanceled_ThrowsOperationCanceledException() {
        var sut = CreateCamera();
        SetDownloadExposure(sut);
        using var cancellationTokenSource = new CancellationTokenSource();

        Task waitTask = sut.WaitUntilExposureIsReady(cancellationTokenSource.Token);
        cancellationTokenSource.Cancel();

        Func<Task> wait = async () => await waitTask.WaitAsync(TimeSpan.FromSeconds(1));
        await wait.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public void AbortAndStopExposure_WhenLongExposureHasStopAction_InvokesItExactlyOnce() {
        var sut = CreateCamera();
        int stopCount = 0;
        Action stopAction = () => stopCount++;
        Invoke(sut, "SetActiveExposureStopAction", stopAction);

        sut.AbortExposure();
        sut.StopExposure();

        stopCount.Should().Be(1);
    }

    [Test]
    public void AbortAndStopExposure_WhenNoStopActionExists_DoesNotCallNikonBulbTermination() {
        var sut = CreateCamera();
        sut.Connected = true;

        Action abortAndStop = () => {
            sut.AbortExposure();
            sut.StopExposure();
        };

        abortAndStop.Should().NotThrow();
    }

    [Test]
    public void AbortAndStopExposure_WhenStopActionThrows_ContainsExceptionAndInvokesItExactlyOnce() {
        var sut = CreateCamera();
        int stopCount = 0;
        Action stopAction = () => {
            stopCount++;
            throw new InvalidOperationException("Vendor stop failed");
        };
        Invoke(sut, "SetActiveExposureStopAction", stopAction);

        Action abortAndStop = () => {
            sut.AbortExposure();
            sut.StopExposure();
        };

        abortAndStop.Should().NotThrow();
        stopCount.Should().Be(1);
    }

    [Test]
    public async Task WaitUntilExposureIsReady_WhenCanceled_AllowsLateEventsToSettleExposure() {
        var sut = CreateCamera();
        TaskCompletionSource<object> pendingExposure = SetDownloadExposure(sut);
        using var cancellationTokenSource = new CancellationTokenSource();
        Task waitTask = sut.WaitUntilExposureIsReady(cancellationTokenSource.Token);

        cancellationTokenSource.Cancel();

        Func<Task> wait = async () => await waitTask;
        await wait.Should().ThrowAsync<OperationCanceledException>();
        pendingExposure.Task.IsCompleted.Should().BeFalse();

        RaiseImageReady(sut);

        await pendingExposure.Task.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task WaitUntilExposureIsReady_WhenLongExposureIsCanceled_InvokesStopActionOnce() {
        var sut = CreateCamera();
        SetDownloadExposure(sut);
        int stopCount = 0;
        Action stopAction = () => stopCount++;
        Invoke(sut, "SetActiveExposureStopAction", stopAction);
        using var cancellationTokenSource = new CancellationTokenSource();
        Task waitTask = sut.WaitUntilExposureIsReady(cancellationTokenSource.Token);

        cancellationTokenSource.Cancel();

        Func<Task> wait = async () => await waitTask;
        await wait.Should().ThrowAsync<OperationCanceledException>();
        stopCount.Should().Be(1);
    }

    [Test]
    public async Task WaitUntilExposureIsReady_WhenVendorStopThrows_ContainsVendorException() {
        var sut = CreateCamera();
        SetDownloadExposure(sut);
        Action stopAction = () => throw new NikonException("Vendor stop failed");
        Invoke(sut, "SetActiveExposureStopAction", stopAction);
        using var cancellationTokenSource = new CancellationTokenSource();
        Task waitTask = sut.WaitUntilExposureIsReady(cancellationTokenSource.Token);

        cancellationTokenSource.Cancel();

        Func<Task> wait = async () => await waitTask;
        await wait.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public void AbortAndStopExposure_WhenCalledConcurrently_InvokesStopActionOnce() {
        var sut = CreateCamera();
        int stopCount = 0;
        Action stopAction = () => Interlocked.Increment(ref stopCount);
        Invoke(sut, "SetActiveExposureStopAction", stopAction);

        Parallel.For(0, 100, iteration => {
            if (iteration % 2 == 0) {
                sut.AbortExposure();
            } else {
                sut.StopExposure();
            }
        });

        stopCount.Should().Be(1);
    }

    [Test]
    public async Task WaitUntilExposureIsReady_WhenSdkEventsAreDuplicated_RemainsCompleted() {
        var sut = CreateCamera();
        SetDownloadExposure(sut);
        Task waitTask = sut.WaitUntilExposureIsReady(CancellationToken.None);

        RaiseImageReady(sut);
        RaiseImageReady(sut);
        RaiseCaptureComplete(sut);
        RaiseCaptureComplete(sut);

        await waitTask.WaitAsync(TimeSpan.FromSeconds(1));
    }

    private static NikonCamera CreateCamera() {
        return new NikonCamera(
            Mock.Of<IProfileService>(),
            Mock.Of<ITelescopeMediator>(),
            Mock.Of<IExposureDataFactory>());
    }

    private static TaskCompletionSource<object> SetDownloadExposure(NikonCamera camera) {
        var downloadExposure = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        SetField(camera, "_downloadExposure", downloadExposure);
        return downloadExposure;
    }

    private static void RaiseImageReady(NikonCamera camera) {
        var image = (NikonImage)Activator.CreateInstance(
            typeof(NikonImage),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: new object[] { 1, NikonImageType.Raw, 1, false },
            culture: null)!;

        Invoke(camera, "Camera_ImageReady", null, image);
    }

    private static void RaiseCaptureComplete(NikonCamera camera) {
        Invoke(camera, "_camera_CaptureComplete", null, 0);
    }

    private static void SetField(NikonCamera camera, string fieldName, object value) {
        typeof(NikonCamera).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(camera, value);
    }

    private static void Invoke(NikonCamera camera, string methodName, params object?[] arguments) {
        typeof(NikonCamera).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(camera, arguments);
    }
}
