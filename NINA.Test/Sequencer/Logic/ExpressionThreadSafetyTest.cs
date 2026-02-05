using FluentAssertions;
using Moq;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.Logic;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.Sequencer.Logic {

    [TestFixture]
    public class ExpressionThreadSafetyTest {

        [Test]
        public void Evaluate_ManyThreads_NoConcurrentUpdates_DoesNotThrow_AndIsDeterministic() {
            var root = new SequenceRootContainer();

            var profileServiceMock = new Mock<IProfileService>();
            var switchMediatorMock = new Mock<ISwitchMediator>();
            var weatherDataMediatorMock = new Mock<IWeatherDataMediator>();
            var cameraMediatorMock = new Mock<ICameraMediator>();
            var domeMediatorMock = new Mock<IDomeMediator>();
            var flatDeviceMediatorMock = new Mock<IFlatDeviceMediator>();
            var filterWheelMediatorMock = new Mock<IFilterWheelMediator>();
            var rotatorMediatorMock = new Mock<IRotatorMediator>();
            var safetyMonitorMediatorMock = new Mock<ISafetyMonitorMediator>();
            var focuserMediatorMock = new Mock<IFocuserMediator>();
            var telescopeMediatorMock = new Mock<ITelescopeMediator>();
            var guiderMediatorMock = new Mock<IGuiderMediator>();
            var imagingMediatorMock = new Mock<IImagingMediator>();

            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Latitude).Returns(10);
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Longitude).Returns(20);
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Elevation).Returns(30);

            var broker = new SymbolBroker(
                profileServiceMock.Object,
                switchMediatorMock.Object,
                weatherDataMediatorMock.Object,
                cameraMediatorMock.Object,
                domeMediatorMock.Object,
                flatDeviceMediatorMock.Object,
                filterWheelMediatorMock.Object,
                rotatorMediatorMock.Object,
                safetyMonitorMediatorMock.Object,
                focuserMediatorMock.Object,
                telescopeMediatorMock.Object,
                guiderMediatorMock.Object,
                imagingMediatorMock.Object);

            try {
                double temperature = -10.5;
                double coolerPower = 75.5;
                double expected = temperature + coolerPower;

                broker.UpdateDeviceInfo(new CameraInfo {
                    Connected = true,
                    Temperature = temperature,
                    CoolerPower = coolerPower
                });

                var expr = new Expression("Camera_Temperature + Camera_CoolerPower", root) {
                    SymbolBroker = broker
                };

                expr.Evaluate(true);
                expr.Error.Should().BeNull();
                expr.Value.Should().Be(expected);

                const int threadCount = 32;
                const int iterationsPerThread = 2000;

                var startGate = new ManualResetEventSlim(false);
                var exceptions = new ConcurrentQueue<Exception>();
                var wrongValues = new ConcurrentQueue<double>();

                Task[] tasks = new Task[threadCount];
                for (int t = 0; t < threadCount; t++) {
                    tasks[t] = Task.Run(() => {
                        startGate.Wait();

                        for (int i = 0; i < iterationsPerThread; i++) {
                            try {
                                expr.Evaluate(true);

                                if (expr.Error != null) {
                                    exceptions.Enqueue(new InvalidOperationException(expr.Error));
                                    return;
                                }

                                if (expr.Value != expected) {
                                    wrongValues.Enqueue(expr.Value);
                                    return;
                                }
                            } catch (Exception ex) {
                                exceptions.Enqueue(ex);
                                return;
                            }
                        }
                    });
                }

                startGate.Set();
                Task.WaitAll(tasks);

                exceptions.Should().BeEmpty();
                wrongValues.Should().BeEmpty();
            } finally {
                broker.Dispose();
            }
        }

        [Test]
        public void Evaluate_ManyThreads_WithConcurrentUpdates_DoesNotThrow_AndStaysWithinPossibleRange() {
            var root = new SequenceRootContainer();

            var profileServiceMock = new Mock<IProfileService>();
            var switchMediatorMock = new Mock<ISwitchMediator>();
            var weatherDataMediatorMock = new Mock<IWeatherDataMediator>();
            var cameraMediatorMock = new Mock<ICameraMediator>();
            var domeMediatorMock = new Mock<IDomeMediator>();
            var flatDeviceMediatorMock = new Mock<IFlatDeviceMediator>();
            var filterWheelMediatorMock = new Mock<IFilterWheelMediator>();
            var rotatorMediatorMock = new Mock<IRotatorMediator>();
            var safetyMonitorMediatorMock = new Mock<ISafetyMonitorMediator>();
            var focuserMediatorMock = new Mock<IFocuserMediator>();
            var telescopeMediatorMock = new Mock<ITelescopeMediator>();
            var guiderMediatorMock = new Mock<IGuiderMediator>();
            var imagingMediatorMock = new Mock<IImagingMediator>();

            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Latitude).Returns(10);
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Longitude).Returns(20);
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Elevation).Returns(30);

            var broker = new SymbolBroker(
                profileServiceMock.Object,
                switchMediatorMock.Object,
                weatherDataMediatorMock.Object,
                cameraMediatorMock.Object,
                domeMediatorMock.Object,
                flatDeviceMediatorMock.Object,
                filterWheelMediatorMock.Object,
                rotatorMediatorMock.Object,
                safetyMonitorMediatorMock.Object,
                focuserMediatorMock.Object,
                telescopeMediatorMock.Object,
                guiderMediatorMock.Object,
                imagingMediatorMock.Object);

            try {
                var expr = new Expression("Camera_Temperature + Camera_CoolerPower", root) {
                    SymbolBroker = broker
                };

                const int updateThreadCount = 8;
                const int evaluateThreadCount = 16;
                const int iterationsPerThread = 2000;

                double minTemperature = -20.0 + 0 + (0 * 0.01);
                double maxTemperature = -20.0 + (updateThreadCount - 1) + ((iterationsPerThread - 1) * 0.01);
                double minCoolerPower = 10.0 + (0 * 0.5) + (0 * 0.02);
                double maxCoolerPower = 10.0 + ((updateThreadCount - 1) * 0.5) + ((iterationsPerThread - 1) * 0.02);

                double minExpected = minTemperature + minCoolerPower;
                double maxExpected = maxTemperature + maxCoolerPower;

                var startGate = new ManualResetEventSlim(false);
                var exceptions = new ConcurrentQueue<Exception>();
                var outOfRangeValues = new ConcurrentQueue<double>();
                var nanValues = new ConcurrentQueue<double>();

                Task[] updaterTasks = new Task[updateThreadCount];
                for (int t = 0; t < updateThreadCount; t++) {
                    int threadIndex = t;
                    updaterTasks[t] = Task.Run(() => {
                        startGate.Wait();

                        for (int i = 0; i < iterationsPerThread; i++) {
                            double temperature = -20.0 + threadIndex + (i * 0.01);
                            double coolerPower = 10.0 + (threadIndex * 0.5) + (i * 0.02);

                            try {
                                broker.UpdateDeviceInfo(new CameraInfo {
                                    Connected = true,
                                    Temperature = temperature,
                                    CoolerPower = coolerPower
                                });

                                Thread.Yield();
                            } catch (Exception ex) {
                                exceptions.Enqueue(ex);
                                return;
                            }
                        }
                    });
                }

                Task[] evaluatorTasks = new Task[evaluateThreadCount];
                for (int t = 0; t < evaluateThreadCount; t++) {
                    evaluatorTasks[t] = Task.Run(() => {
                        startGate.Wait();

                        for (int i = 0; i < iterationsPerThread; i++) {
                            try {
                                expr.Evaluate(true);

                                if (expr.Error != null) {
                                    exceptions.Enqueue(new InvalidOperationException(expr.Error));
                                    return;
                                }

                                double value = expr.Value;

                                if (double.IsNaN(value)) {
                                    nanValues.Enqueue(value);
                                    return;
                                }

                                if (value < minExpected || value > maxExpected) {
                                    outOfRangeValues.Enqueue(value);
                                    return;
                                }

                                Thread.Yield();
                            } catch (Exception ex) {
                                exceptions.Enqueue(ex);
                                return;
                            }
                        }
                    });
                }

                startGate.Set();
                Task.WaitAll(updaterTasks);
                Task.WaitAll(evaluatorTasks);

                exceptions.Should().BeEmpty();
                nanValues.Should().BeEmpty();
                outOfRangeValues.Should().BeEmpty();
            } finally {
                broker.Dispose();
            }
        }
    }
}