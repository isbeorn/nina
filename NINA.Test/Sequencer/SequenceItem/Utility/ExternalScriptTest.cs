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
using NINA.Core.Model;
using NINA.Sequencer.Container;
using NINA.Sequencer.Logic;
using NINA.Sequencer.SequenceItem.Utility;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.SequenceItem.Utility {

    [TestFixture]
    public class ExternalScriptTest {

        public Mock<ISymbolBroker> symbolBrokerMock;
        public Mock<IProgress<ApplicationStatus>> progressMock;

        [SetUp]
        public void Setup() {
            symbolBrokerMock = new Mock<ISymbolBroker>();
            symbolBrokerMock.As<ISymbolBrokerProviderApi>();
            progressMock = new Mock<IProgress<ApplicationStatus>>();
        }

        [Test]
        public void ExternalScript_Clone_GoodClone() {
            var sut = new ExternalScript(symbolBrokerMock.Object);
            sut.Icon = new System.Windows.Media.GeometryGroup();
            var item2 = (ExternalScript)sut.Clone();

            item2.Should().NotBeSameAs(sut);
            item2.Name.Should().BeSameAs(sut.Name);
            item2.Description.Should().BeSameAs(sut.Description);
            item2.Icon.Should().BeSameAs(sut.Icon);
            item2.Script.Should().Be(sut.Script);
        }

        [Test]
        public void ExternalScriptTest_GetEstimatedDuration_Test() {
            var sut = new ExternalScript(symbolBrokerMock.Object);
            var estimate = sut.GetEstimatedDuration();
            estimate.Should().Be(TimeSpan.Zero);
        }

        [Test]
        public void ExternalScript_Expand_ReplacesMultipleExpressions() {
            // Arrange
            var sut = new ExternalScript(symbolBrokerMock.Object);
            sut.AttachNewParent(new SequentialContainer());

            // Setup symbol broker with test symbols
            symbolBrokerMock.Setup(x => x.TryGetValue("Camera_Name", out It.Ref<object>.IsAny))
                .Returns((string key, out object value) => {
                    value = "Test Camera";
                    return true;
                });

            symbolBrokerMock.Setup(x => x.TryGetValue("Camera_Temperature", out It.Ref<object>.IsAny))
                .Returns((string key, out object value) => {
                    value = -10.5;
                    return true;
                });

            // Script with two expressions
            sut.Script = "script.exe --camera \"{Camera_Name}\" --temp {Camera_Temperature}";

            // Act
            var result = ExpressionExpander.Expand(sut.Script, symbolBrokerMock.Object, sut);

            // Assert
            result.Should().Be("script.exe --camera \"Test Camera\" --temp -10.5");
        }

        [Test]
        public void ExternalScript_Expand_HandlesArithmeticExpression() {
            // Arrange
            var sut = new ExternalScript(symbolBrokerMock.Object);
            sut.AttachNewParent(new SequentialContainer());

            // Setup symbol broker with numeric test symbols
            symbolBrokerMock.Setup(x => x.TryGetValue("Camera_Gain", out It.Ref<object>.IsAny))
                .Returns((string key, out object value) => {
                    value = 100.0;
                    return true;
                });

            symbolBrokerMock.Setup(x => x.TryGetValue("Camera_Offset", out It.Ref<object>.IsAny))
                .Returns((string key, out object value) => {
                    value = 50.0;
                    return true;
                });

            // Script with arithmetic expression
            sut.Script = "script.exe --gain {Camera_Gain} --total {Camera_Gain + Camera_Offset}";

            // Act
            var result = ExpressionExpander.Expand(sut.Script, symbolBrokerMock.Object, sut);

            // Assert
            result.Should().Be("script.exe --gain 100 --total 150");
        }

        [Test]
        public void ExternalScript_Expand_ErrorInExpression_ReturnsError() {
            // Arrange
            var sut = new ExternalScript(symbolBrokerMock.Object);
            sut.AttachNewParent(new SequentialContainer());

            // Setup symbol Broker to return false for unknown symbols
            symbolBrokerMock.Setup(x => x.TryGetValue(It.IsAny<string>(), out It.Ref<object>.IsAny))
                .Returns((string key, out object value) => {
                    value = null;
                    return false;
                });

            // Script with invalid expression
            sut.Script = "script.exe --value {UnknownSymbol}";

            // Act
            var result = ExpressionExpander.Expand(sut.Script, symbolBrokerMock.Object, sut);

            // Assert
            result.Should().Contain("Error");
        }

        [Test]
        public async Task ExternalScript_Execute_SetsExitCodeSymbol_OnSuccess() {
            // Arrange
            var mockProvider = new Mock<ISymbolProvider>();
            var capturedExitCode = -999; // Sentinel value

            symbolBrokerMock.As<ISymbolBrokerProviderApi>()
                .Setup(x => x.GetInternalProvider("NINA"))
                .Returns(mockProvider.Object);

            mockProvider.Setup(x => x.AddOrUpdateSymbol("LastExternalScriptExitCode", It.IsAny<int>()))
                .Callback<string, object>((key, value) => {
                    capturedExitCode = (int)value;
                });

            var sut = new ExternalScript(symbolBrokerMock.Object);

            // Use full path to cmd.exe
            sut.Script = $"{Environment.GetEnvironmentVariable("SystemRoot")}\\System32\\cmd.exe /c exit 0";

            var progress = new Progress<ApplicationStatus>();
            var cts = new CancellationTokenSource();

            // Act
            await sut.Execute(progress, cts.Token);

            // Assert
            capturedExitCode.Should().Be(0);
            mockProvider.Verify(x => x.AddOrUpdateSymbol("LastExternalScriptExitCode", 0), Times.Once);
        }

        [Test]
        public async Task ExternalScript_Execute_SetsExitCodeToNegativeOne_OnError() {
            // Arrange
            var mockProvider = new Mock<ISymbolProvider>();
            var capturedExitCode = -999;

            symbolBrokerMock.As<ISymbolBrokerProviderApi>()
                .Setup(x => x.GetInternalProvider("NINA"))
                .Returns(mockProvider.Object);

            mockProvider.Setup(x => x.AddOrUpdateSymbol("LastExternalScriptExitCode", It.IsAny<int>()))
                .Callback<string, object>((key, value) => {
                    capturedExitCode = (int)value;
                });

            var sut = new ExternalScript(symbolBrokerMock.Object);

            // Use a non-existent command to trigger an exception
            sut.Script = "this_command_does_not_exist_12345.exe";

            var progress = new Progress<ApplicationStatus>();
            var cts = new CancellationTokenSource();

            // Act & Assert
            await sut.Invoking(s => s.Execute(progress, cts.Token))
                .Should().ThrowAsync<SequenceEntityFailedException>();

            // Symbol should be set to -1 on error
            capturedExitCode.Should().Be(-1);
            mockProvider.Verify(x => x.AddOrUpdateSymbol("LastExternalScriptExitCode", -1), Times.Once);
        }

        [Test]
        public async Task ExternalScript_Execute_HandlesNullProvider_Gracefully() {
            // Arrange
            symbolBrokerMock.As<ISymbolBrokerProviderApi>()
                .Setup(x => x.GetInternalProvider("NINA"))
                .Returns((ISymbolProvider)null);

            var sut = new ExternalScript(symbolBrokerMock.Object);
            sut.Script = $"{Environment.GetEnvironmentVariable("SystemRoot")}\\System32\\cmd.exe /c exit 0";

            var progress = new Progress<ApplicationStatus>();
            var cts = new CancellationTokenSource();

            // Act & Assert - should not throw even though provider is null
            await sut.Invoking(async s => await s.Execute(progress, cts.Token))
                .Should().NotThrowAsync();
        }

        [Test]
        public async Task ExternalScript_ProcessedScript_ReplacesSymbolsBeforeExecution() {
            // Arrange
            var mockProvider = new Mock<ISymbolProvider>();
            var capturedExitCode = -999;

            symbolBrokerMock.As<ISymbolBrokerProviderApi>()
                .Setup(x => x.GetInternalProvider("NINA"))
                .Returns(mockProvider.Object);

            var sut = new ExternalScript(symbolBrokerMock.Object);
            sut.AttachNewParent(new SequentialContainer());

            symbolBrokerMock.Setup(x => x.TryGetValue("TestValue", out It.Ref<object>.IsAny))
                .Returns((string key, out object value) => {
                    value = 7;
                    return true;
                });

            mockProvider.Setup(x => x.AddOrUpdateSymbol("LastExternalScriptExitCode", It.IsAny<int>()))
                .Callback<string, object>((key, value) => {
                    capturedExitCode = (int)value;
                });

            // Script with expression that resolves to exit code 7
            sut.Script = $"{Environment.GetEnvironmentVariable("SystemRoot")}\\System32\\cmd.exe /c exit {{TestValue}}";

            var progress = new Progress<ApplicationStatus>();
            var cts = new CancellationTokenSource();

            // Act & Assert
            await sut.Invoking(s => s.Execute(progress, cts.Token))
                .Should().ThrowAsync<SequenceEntityFailedException>();

            // Verify the exit code matches the symbol value
            capturedExitCode.Should().Be(7);
        }
    }
}