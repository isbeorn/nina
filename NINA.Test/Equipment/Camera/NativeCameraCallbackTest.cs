using FluentAssertions;
using NINA.Equipment.Equipment.MyCamera.ToupTekAlike;
using NINA.Equipment.Interfaces;
using System.Reflection;
using System.Threading;

namespace NINA.Test.Equipment.Camera {
    [TestFixture]
    public class NativeCameraCallbackTest {
        private static IEnumerable<Type> Wrappers => new[] {
            typeof(AltairSDKWrapper), typeof(MallinCamSDKWrapper), typeof(OgmaSDKWrapper),
            typeof(OmegonSDKWrapper), typeof(RisingcamSDKWrapper), typeof(SVBonySDKWrapper), typeof(ToupTekSDKWrapper)
        };

        [TestCaseSource(nameof(Wrappers))]
        public void Callback_CanCompleteWhileNativeCloseHoldsTheSdkGate(Type type) {
            var wrapper = Activator.CreateInstance(type);
            var gate = type.GetField("lockObj", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(wrapper);
            var callbackField = type.GetField("toupTekAlikeCallback", BindingFlags.Instance | BindingFlags.NonPublic);
            using var invoked = new ManualResetEventSlim();
            callbackField.SetValue(wrapper, new ToupTekAlikeCallback(_ => invoked.Set()));
            var callback = type.GetMethod("EventCallback", BindingFlags.Instance | BindingFlags.NonPublic);
            var imageEvent = Enum.Parse(callback.GetParameters()[0].ParameterType, "EVENT_IMAGE");
            Task dispatch;
            bool completed;
            lock (gate) {
                dispatch = Task.Run(() => callback.Invoke(wrapper, new[] { imageEvent }));
                completed = invoked.Wait(TimeSpan.FromSeconds(2));
            }
            dispatch.Wait(TimeSpan.FromSeconds(2)).Should().BeTrue();
            completed.Should().BeTrue("native Close may wait for its callback while holding the SDK gate");

            ((IToupTekAlikeCameraSDK)wrapper).Close();
            invoked.Reset();
            callback.Invoke(wrapper, new[] { imageEvent });
            invoked.IsSet.Should().BeFalse("callbacks after close must not reach the old capture");
        }

        [TestCaseSource(nameof(Wrappers))]
        public void PropertyRead_IsSerializedWithCloseAndReleasesGateOnFailure(Type type) {
            var wrapper = (IToupTekAlikeCameraSDK)Activator.CreateInstance(type);
            var gate = type.GetField("lockObj", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(wrapper);
            using var started = new ManualResetEventSlim();
            Task read;
            bool completedWhileLocked;
            lock (gate) {
                read = Task.Run(() => {
                    started.Set();
                    try { _ = wrapper.MaxSpeed; } catch (NullReferenceException) { }
                });
                started.Wait(TimeSpan.FromSeconds(2)).Should().BeTrue();
                completedWhileLocked = read.Wait(TimeSpan.FromMilliseconds(100));
            }
            read.Wait(TimeSpan.FromSeconds(2)).Should().BeTrue();
            completedWhileLocked.Should().BeFalse();
            Task.Run(wrapper.Close).Wait(TimeSpan.FromSeconds(2)).Should().BeTrue();
        }
    }
}
