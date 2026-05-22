using FluentAssertions;
using Moq;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Equipment.MyGuider;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Container;
using NINA.Sequencer.Logic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FilterWheelInfo = NINA.Equipment.Equipment.MyFilterWheel.FilterWheelInfo;

namespace NINA.Test.Sequencer.Logic {
    public class SymbolBrokerTest {
        private Mock<IProfileService> profileServiceMock;
        private Mock<ISwitchMediator> switchMediatorMock;
        private Mock<IWeatherDataMediator> weatherDataMediatorMock;
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<IDomeMediator> domeMediatorMock;
        private Mock<IFlatDeviceMediator> flatDeviceMediatorMock;
        private Mock<IFilterWheelMediator> filterWheelMediatorMock;
        private Mock<IRotatorMediator> rotatorMediatorMock;
        private Mock<ISafetyMonitorMediator> safetyMonitorMediatorMock;
        private Mock<IFocuserMediator> focuserMediatorMock;
        private Mock<ITelescopeMediator> telescopeMediatorMock;
        private Mock<IGuiderMediator> guiderMediatorMock;
        private Mock<IImagingMediator> imagingMediatorMock;
        private SymbolBroker broker;

        [SetUp]
        public void Setup() {
            profileServiceMock = new Mock<IProfileService>();
            switchMediatorMock = new Mock<ISwitchMediator>();
            weatherDataMediatorMock = new Mock<IWeatherDataMediator>();
            cameraMediatorMock = new Mock<ICameraMediator>();
            domeMediatorMock = new Mock<IDomeMediator>();
            flatDeviceMediatorMock = new Mock<IFlatDeviceMediator>();
            filterWheelMediatorMock = new Mock<IFilterWheelMediator>();
            rotatorMediatorMock = new Mock<IRotatorMediator>();
            safetyMonitorMediatorMock = new Mock<ISafetyMonitorMediator>();
            focuserMediatorMock = new Mock<IFocuserMediator>();
            telescopeMediatorMock = new Mock<ITelescopeMediator>();
            guiderMediatorMock = new Mock<IGuiderMediator>();
            imagingMediatorMock = new Mock<IImagingMediator>();

            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Latitude).Returns(10);
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Longitude).Returns(20);
            profileServiceMock.SetupGet(x => x.ActiveProfile.AstrometrySettings.Elevation).Returns(30);

            broker = new SymbolBroker(profileServiceMock.Object, switchMediatorMock.Object, weatherDataMediatorMock.Object, cameraMediatorMock.Object, domeMediatorMock.Object,
                flatDeviceMediatorMock.Object, filterWheelMediatorMock.Object, rotatorMediatorMock.Object, safetyMonitorMediatorMock.Object, focuserMediatorMock.Object,
                telescopeMediatorMock.Object, guiderMediatorMock.Object, imagingMediatorMock.Object);
        }

        [TearDown]
        public void TearDown() {
            broker.Dispose();
        }

        private void ValidateSymbol(string key, bool expectedSuccess, object? expectedValue = null, bool isHidden = false) {
            // Validate TryGetSymbol
            var success = broker.TryGetSymbol(key, out var symbol);
            success.Should().Be(expectedSuccess, key);
            if (expectedSuccess) {
                symbol.Should().NotBeNull();
                if (expectedValue is not null) {
                    symbol.Value.Should().Be(expectedValue, $"Key {key} should be {expectedValue}");
                    symbol.Type.Should().Be(isHidden ? Symbol.SymbolType.SYMBOL_HIDDEN : Symbol.SymbolType.SYMBOL_NORMAL);
                }
            } else {
                symbol.Should().BeNull();
            }

            // Validate TryGetValue
            var successGetValue = broker.TryGetValue(key, out var value);
            successGetValue.Should().Be(expectedSuccess, key);
            if (expectedSuccess) {
                if (expectedValue is not null) {
                    value.Should().Be(expectedValue);
                }
            } else {
                value.Should().BeNull();
            }
        }

        [Test]
        public void SymbolBroker_Instance_IsInternalSingletonFallback() {
            typeof(SymbolBroker)
                .GetProperty(nameof(SymbolBroker.Instance), BindingFlags.Public | BindingFlags.Static)
                .Should().BeNull();

            PropertyInfo instanceProperty = typeof(SymbolBroker)
                .GetProperty(nameof(SymbolBroker.Instance), BindingFlags.NonPublic | BindingFlags.Static);

            instanceProperty.Should().NotBeNull();
            instanceProperty.GetMethod.IsAssembly.Should().BeTrue();
            instanceProperty.SetMethod.IsPrivate.Should().BeTrue();
            SymbolBroker.Instance.Should().BeSameAs(broker);
        }

        [Test]
        public void SymbolBroker_HasCoreSymbols() {
            // Assert
            ValidateSymbol(key: "NINA_ProfileId", expectedSuccess: true);
            ValidateSymbol(key: "NINA_ProfileName", expectedSuccess: true);
            ValidateSymbol(key: "NINA_Uptime", expectedSuccess: true);
            ValidateSymbol(key: "NINA_LocalSiderealTime", expectedSuccess: true);
            ValidateSymbol(key: "NINA_SunAzimuth", expectedSuccess: true);
            ValidateSymbol(key: "NINA_SunAltitude", expectedSuccess: true);
            ValidateSymbol(key: "NINA_MoonIllumination", expectedSuccess: true);
            ValidateSymbol(key: "NINA_MoonAltitude", expectedSuccess: true);
        }

        [Test]
        public void Expression_Evaluate_TimeOnlyBrokerSymbol_ShouldUseSecondsSinceMidnight() {
            // Arrange
            TimeOnly timeOnly = new TimeOnly(12, 34, 56);
            ISymbolProvider provider = broker.RegisterSymbolProvider("TemporalTest");
            provider.AddOrUpdateSymbol("TimeOnly", timeOnly);

            Mock<ISequenceEntity> context = new Mock<ISequenceEntity>();
            context.SetupGet(x => x.SymbolBroker).Returns(broker);
            context.SetupGet(x => x.Parent).Returns((ISequenceContainer)null);
            context.SetupGet(x => x.Name).Returns("SymbolBroker Test Context");

            Expression expr = new Expression("TemporalTest_TimeOnly + 30", context.Object) {
                SymbolBroker = broker,
                IsExpression = true
            };

            // Act
            expr.Evaluate(ignoreRoot: true);

            // Assert
            expr.Error.Should().BeNull();
            expr.Value.Should().Be(timeOnly.ToTimeSpan().TotalSeconds + 30);
        }

        [Test]
        public void SymbolBroker_UpdateDeviceInfo_Camera_Connected_HasSymbols() {
            // Arrange
            var info = new CameraInfo {
                Connected = true,
                TemperatureSetPoint = -15.0,
                CoolerOn = true,
                CoolerPower = 75.5,
                Temperature = -10.5,
                XSize = 4000,
                YSize = 3000,
                PixelSize = 4.54
            };

            // Act
            broker.UpdateDeviceInfo(info);

            // Assert
            ValidateSymbol(key: "Camera_Temperature", expectedSuccess: true, expectedValue: info.Temperature);
            ValidateSymbol(key: "Camera_TemperatureSetPoint", expectedSuccess: true, expectedValue: info.TemperatureSetPoint);
            ValidateSymbol(key: "Camera_CoolerOn", expectedSuccess: true, expectedValue: info.CoolerOn);
            ValidateSymbol(key: "Camera_CoolerPower", expectedSuccess: true, expectedValue: info.CoolerPower);
            ValidateSymbol(key: "Camera_PixelSize", expectedSuccess: true, expectedValue: info.PixelSize, isHidden: true);
            ValidateSymbol(key: "Camera_XSize", expectedSuccess: true, expectedValue: info.XSize, isHidden: true);
            ValidateSymbol(key: "Camera_YSize", expectedSuccess: true, expectedValue: info.YSize, isHidden: true);
        }

        [Test]
        public void SymbolBroker_UpdateDeviceInfo_Camera_Disconnected_HasNoSymbols() {
            // Arrange
            var info = new CameraInfo {
                Connected = false,
            };

            // Act
            broker.UpdateDeviceInfo(new CameraInfo { Connected = true });
            broker.UpdateDeviceInfo(info);

            // Assert
            ValidateSymbol(key: "Camera_Temperature", expectedSuccess: false);
            ValidateSymbol(key: "Camera_TemperatureSetPoint", expectedSuccess: false);
            ValidateSymbol(key: "Camera_CoolerOn", expectedSuccess: false);
            ValidateSymbol(key: "Camera_CoolerPower", expectedSuccess: false);
            ValidateSymbol(key: "Camera_PixelSize", expectedSuccess: false);
            ValidateSymbol(key: "Camera_XSize", expectedSuccess: false);
            ValidateSymbol(key: "Camera_YSize", expectedSuccess: false);
        }

        [Test]
        public void SymbolBroker_UpdateDeviceInfo_Focuser_Connected_HasSymbols() {
            // Arrange
            var info = new FocuserInfo {
                Connected = true,
                Position = 3450,
                Temperature = -10.5
            };

            // Act
            broker.UpdateDeviceInfo(info);

            // Assert
            ValidateSymbol(key: "Focuser_Position", expectedSuccess: true, expectedValue: info.Position);
            ValidateSymbol(key: "Focuser_Temperature", expectedSuccess: true, expectedValue: info.Temperature);
        }

        [Test]
        public void SymbolBroker_UpdateDeviceInfo_Focuser_Disconnected_HasNoSymbols() {
            // Arrange
            var info = new FocuserInfo {
                Connected = false
            };

            // Act
            broker.UpdateDeviceInfo(new FocuserInfo { Connected = true });
            broker.UpdateDeviceInfo(info);

            // Assert
            ValidateSymbol(key: "Focuser_Position", expectedSuccess: false);
            ValidateSymbol(key: "Focuser_Temperature", expectedSuccess: false);
        }

        [Test]
        public void SymbolBroker_UpdateDeviceInfo_Rotator_Connected_HasSymbols() {
            // Arrange
            var info = new RotatorInfo {
                Connected = true,
                Position = 60.8f,
                MechanicalPosition = 10.8f
            };

            // Act
            broker.UpdateDeviceInfo(info);

            // Assert
            ValidateSymbol(key: "Rotator_Position", expectedSuccess: true, expectedValue: info.Position);
            ValidateSymbol(key: "Rotator_MechanicalPosition", expectedSuccess: true, expectedValue: info.MechanicalPosition);
        }

        [Test]
        public void SymbolBroker_UpdateDeviceInfo_Rotator_Disconnected_HasNoSymbols() {
            // Arrange
            var info = new RotatorInfo {
                Connected = false
            };

            // Act
            broker.UpdateDeviceInfo(new RotatorInfo { Connected = true });
            broker.UpdateDeviceInfo(info);

            // Assert
            ValidateSymbol(key: "Rotator_Position", expectedSuccess: false);
            ValidateSymbol(key: "Rotator_Temperature", expectedSuccess: false);
        }

        [Test]
        public void SymbolBroker_UpdateDeviceInfo_FilterWheel_Connected_HasSymbols() {
            // Arrange
            profileServiceMock.SetupGet(x => x.ActiveProfile.FilterWheelSettings.FilterWheelFilters).Returns(new Core.Utility.ObserveAllCollection<FilterInfo> {
                new FilterInfo { Name = "Red", Position = 1 },
                new FilterInfo { Name = "Luminance", Position = 2 },
                new FilterInfo { Name = "Blue", Position = 3 }
            });

            var info = new FilterWheelInfo {
                Connected = true,
                SelectedFilter = new FilterInfo { Name = "Luminance", Position = 2 }
            };

            // Act
            broker.UpdateDeviceInfo(info);

            // Assert
            ValidateSymbol(key: "FilterWheel_CurrentFilterIndex", expectedSuccess: true, expectedValue: info.SelectedFilter.Position);
            ValidateSymbol(key: "Filter_Red", expectedSuccess: true, expectedValue: 1);
            ValidateSymbol(key: "Filter_Luminance", expectedSuccess: true, expectedValue: 2);
            ValidateSymbol(key: "Filter_Blue", expectedSuccess: true, expectedValue: 3);
        }

        [Test]
        public void SymbolBroker_UpdateDeviceInfo_FilterWheel_Disconnected_HasNoSymbols() {
            // Arrange
            profileServiceMock.SetupGet(x => x.ActiveProfile.FilterWheelSettings.FilterWheelFilters).Returns(new Core.Utility.ObserveAllCollection<FilterInfo> {
                new FilterInfo { Name = "Red", Position = 1 },
                new FilterInfo { Name = "Luminance", Position = 2 },
                new FilterInfo { Name = "Blue", Position = 3 }
            });
            var info = new FilterWheelInfo {
                Connected = false
            };

            // Act
            broker.UpdateDeviceInfo(new FilterWheelInfo { Connected = true });
            broker.UpdateDeviceInfo(info);

            // Assert
            ValidateSymbol(key: "FilterWheel_CurrentFilterIndex", expectedSuccess: false);
            ValidateSymbol(key: "Filter_Red", expectedSuccess: false);
            ValidateSymbol(key: "Filter_Luminance", expectedSuccess: false);
            ValidateSymbol(key: "Filter_Blue", expectedSuccess: false);
        }

        [Test]
        public void SymbolBroker_UpdateDeviceInfo_Switch_Connected_HasSymbols() {
            // Arrange
            var readOnlySwitch1 = new Mock<ISwitch>();
            readOnlySwitch1.SetupGet(x => x.Name).Returns("TestSwitch1");
            readOnlySwitch1.SetupGet(x => x.Value).Returns(32.4);
            var readOnlySwitch2 = new Mock<ISwitch>();
            readOnlySwitch2.SetupGet(x => x.Name).Returns("TestSwitch2");
            readOnlySwitch2.SetupGet(x => x.Value).Returns(22.2);
            var writableSwitch1 = new Mock<IWritableSwitch>();
            writableSwitch1.SetupGet(x => x.Name).Returns("TestSwitch3");
            writableSwitch1.SetupGet(x => x.Value).Returns(42.1);
            var writableSwitch2 = new Mock<IWritableSwitch>();
            writableSwitch2.SetupGet(x => x.Name).Returns("TestSwitch4");
            writableSwitch2.SetupGet(x => x.Value).Returns(52.3);

            var info = new SwitchInfo {
                Connected = true,
                ReadonlySwitches = new System.Collections.ObjectModel.ReadOnlyCollection<ISwitch>(new List<ISwitch> {
                    readOnlySwitch1.Object,
                    readOnlySwitch2.Object
                }),
                WritableSwitches = new System.Collections.ObjectModel.ReadOnlyCollection<IWritableSwitch>(new List<IWritableSwitch> {
                    writableSwitch1.Object,
                    writableSwitch2.Object
                })
            };

            // Act
            broker.UpdateDeviceInfo(info);

            // Assert
            ValidateSymbol(key: "Gauge_TestSwitch1", expectedSuccess: true, expectedValue: 32.4);
            ValidateSymbol(key: "Gauge_TestSwitch2", expectedSuccess: true, expectedValue: 22.2);
            ValidateSymbol(key: "Switch_TestSwitch3", expectedSuccess: true, expectedValue: 42.1);
            ValidateSymbol(key: "Switch_TestSwitch4", expectedSuccess: true, expectedValue: 52.3);
        }

        [Test]
        public void SymbolBroker_UpdateDeviceInfo_Switch_Disconnected_HasNoSymbols() {
            // Arrange
            var readOnlySwitch1 = new Mock<ISwitch>();
            readOnlySwitch1.SetupGet(x => x.Name).Returns("TestSwitch1");
            readOnlySwitch1.SetupGet(x => x.Value).Returns(32.4);
            var readOnlySwitch2 = new Mock<ISwitch>();
            readOnlySwitch2.SetupGet(x => x.Name).Returns("TestSwitch2");
            readOnlySwitch2.SetupGet(x => x.Value).Returns(22.2);
            var writableSwitch1 = new Mock<IWritableSwitch>();
            writableSwitch1.SetupGet(x => x.Name).Returns("TestSwitch3");
            writableSwitch1.SetupGet(x => x.Value).Returns(42.1);
            var writableSwitch2 = new Mock<IWritableSwitch>();
            writableSwitch2.SetupGet(x => x.Name).Returns("TestSwitch4");
            writableSwitch2.SetupGet(x => x.Value).Returns(52.3);
            var connectedInfo = new SwitchInfo {
                Connected = true,
                ReadonlySwitches = new System.Collections.ObjectModel.ReadOnlyCollection<ISwitch>(new List<ISwitch> {
                    readOnlySwitch1.Object,
                    readOnlySwitch2.Object
                }),
                WritableSwitches = new System.Collections.ObjectModel.ReadOnlyCollection<IWritableSwitch>(new List<IWritableSwitch> {
                    writableSwitch1.Object,
                    writableSwitch2.Object
                })
            };
            var info = new SwitchInfo {
                Connected = false,
            };

            // Act
            broker.UpdateDeviceInfo(connectedInfo);
            broker.UpdateDeviceInfo(info);

            // Assert
            ValidateSymbol(key: "Switch_Connected", expectedSuccess: true, expectedValue: false);
            ValidateSymbol(key: "Gauge_TestSwitch1", expectedSuccess: false);
            ValidateSymbol(key: "Gauge_TestSwitch2", expectedSuccess: false);
            ValidateSymbol(key: "Switch_TestSwitch3", expectedSuccess: false);
            ValidateSymbol(key: "Switch_TestSwitch4", expectedSuccess: false);
        }


        [Test]
        public void SymbolBroker_UpdateDeviceInfo_Mount_Connected_HasSymbols() {
            // Arrange
            var info = new TelescopeInfo {
                Connected = true,
                Altitude = 10.5,
                Azimuth = 20.5,
                AtPark = true,
                RightAscension = 0.5,
                Declination = 40.5,
                Coordinates = new Coordinates(Angle.ByHours(0.5), Angle.ByDegree(40.5), Epoch.J2000),
                SideOfPier = Core.Enum.PierSide.pierWest
            };

            // Act
            broker.UpdateDeviceInfo(info);

            // Assert
            ValidateSymbol(key: "Mount_Altitude", expectedSuccess: true, expectedValue: info.Altitude);
            ValidateSymbol(key: "Mount_Azimuth", expectedSuccess: true, expectedValue: info.Azimuth);
            ValidateSymbol(key: "Mount_RightAscensionJ2000", expectedSuccess: true, expectedValue: info.Coordinates.RA);
            ValidateSymbol(key: "Mount_DeclinationJ2000", expectedSuccess: true, expectedValue: info.Coordinates.Dec);
            ValidateSymbol(key: "Mount_SideOfPier", expectedSuccess: true, expectedValue: (int)info.SideOfPier);
            ValidateSymbol(key: "Mount_AtPark", expectedSuccess: true, expectedValue: info.AtPark);
        }

        [Test]
        public void SymbolBroker_UpdateDeviceInfo_Mount_Disconnected_HasNoSymbols() {
            // Arrange
            var info = new TelescopeInfo {
                Connected = false,
            };

            // Act
            broker.UpdateDeviceInfo(new TelescopeInfo { Connected = true, Coordinates = new Coordinates(Angle.ByHours(0.5), Angle.ByDegree(40.5), Epoch.J2000) });
            broker.UpdateDeviceInfo(info);

            // Assert
            ValidateSymbol(key: "Mount_Altitude", expectedSuccess: false);
            ValidateSymbol(key: "Mount_Azimuth", expectedSuccess: false);
            ValidateSymbol(key: "Mount_RightAscensionJ2000", expectedSuccess: false);
            ValidateSymbol(key: "Mount_DeclinationJ2000", expectedSuccess: false);
            ValidateSymbol(key: "Mount_SideOfPier", expectedSuccess: false);
            ValidateSymbol(key: "Mount_AtPark", expectedSuccess: false);
        }

        [Test]
        public void SymbolBroker_UpdateDeviceInfo_FlatDevice_Connected_HasSymbols() {
            // Arrange
            var info = new FlatDeviceInfo {
                Connected = true,
                LightOn = true,
                Brightness = 75,
                CoverState = CoverState.Open
            };

            // Act
            broker.UpdateDeviceInfo(info);

            // Assert
            ValidateSymbol(key: "FlatPanel_LightOn", expectedSuccess: true, expectedValue: info.LightOn);
            ValidateSymbol(key: "FlatPanel_Brightness", expectedSuccess: true, expectedValue: info.Brightness);
            ValidateSymbol(key: "FlatPanel_CoverState", expectedSuccess: true, expectedValue: (int)info.CoverState);
        }

        [Test]
        public void SymbolBroker_UpdateDeviceInfo_FlatDevice_Disconnected_HasNoSymbols() {
            // Arrange
            var info = new FlatDeviceInfo {
                Connected = false,
            };

            // Act
            broker.UpdateDeviceInfo(new FlatDeviceInfo { Connected = true });
            broker.UpdateDeviceInfo(info);

            // Assert
            ValidateSymbol(key: "FlatPanel_LightOn", expectedSuccess: false);
            ValidateSymbol(key: "FlatPanel_Brightness", expectedSuccess: false);
            ValidateSymbol(key: "FlatPanel_CoverState", expectedSuccess: false);
        }

        [Test]
        public void SymbolBroker_UpdateDeviceInfo_Weather_Connected_HasSymbols() {
            // Arrange
            var info = new WeatherDataInfo {
                Connected = true,
                Temperature = -10.5,
                AveragePeriod = 10,
                CloudCover = 20,
                DewPoint = -15,
                Humidity = 50,
                Pressure = 1013,
                RainRate = 0,
                SkyBrightness = 21.5,
                SkyQuality = 19.5,
                SkyTemperature = -12.5,
                StarFWHM = 2.5,
                WindDirection = 180,
                WindGust = 15.5,
                WindSpeed = 10.5
            };

            // Act
            broker.UpdateDeviceInfo(info);

            // Assert
            ValidateSymbol(key: "Weather_Temperature", expectedSuccess: true, expectedValue: info.Temperature);
            ValidateSymbol(key: "Weather_CloudCover", expectedSuccess: true, expectedValue: info.CloudCover);
            ValidateSymbol(key: "Weather_DewPoint", expectedSuccess: true, expectedValue: info.DewPoint);
            ValidateSymbol(key: "Weather_Humidity", expectedSuccess: true, expectedValue: info.Humidity);
            ValidateSymbol(key: "Weather_Pressure", expectedSuccess: true, expectedValue: info.Pressure);
            ValidateSymbol(key: "Weather_RainRate", expectedSuccess: true, expectedValue: info.RainRate);
            ValidateSymbol(key: "Weather_SkyBrightness", expectedSuccess: true, expectedValue: info.SkyBrightness);
            ValidateSymbol(key: "Weather_SkyQuality", expectedSuccess: true, expectedValue: info.SkyQuality);
            ValidateSymbol(key: "Weather_SkyTemperature", expectedSuccess: true, expectedValue: info.SkyTemperature);
            ValidateSymbol(key: "Weather_StarFWHM", expectedSuccess: true, expectedValue: info.StarFWHM);
            ValidateSymbol(key: "Weather_WindDirection", expectedSuccess: true, expectedValue: info.WindDirection);
            ValidateSymbol(key: "Weather_WindGust", expectedSuccess: true, expectedValue: info.WindGust);
            ValidateSymbol(key: "Weather_WindSpeed", expectedSuccess: true, expectedValue: info.WindSpeed);
        }

        [Test]
        public void SymbolBroker_UpdateDeviceInfo_Weather_Connected_PartialData_HasSymbols() {
            // Arrange
            var info = new WeatherDataInfo {
                Connected = true,
                Temperature = -10.5,
                AveragePeriod = 10,
                CloudCover = 20,
                DewPoint = -15,
                Humidity = 50,
                Pressure = 1013,
            };

            // Act
            broker.UpdateDeviceInfo(info);

            // Assert
            ValidateSymbol(key: "Weather_Temperature", expectedSuccess: true, expectedValue: info.Temperature);
            ValidateSymbol(key: "Weather_CloudCover", expectedSuccess: true, expectedValue: info.CloudCover);
            ValidateSymbol(key: "Weather_DewPoint", expectedSuccess: true, expectedValue: info.DewPoint);
            ValidateSymbol(key: "Weather_Humidity", expectedSuccess: true, expectedValue: info.Humidity);
            ValidateSymbol(key: "Weather_Pressure", expectedSuccess: true, expectedValue: info.Pressure);
            ValidateSymbol(key: "Weather_RainRate", expectedSuccess: false);
            ValidateSymbol(key: "Weather_SkyBrightness", expectedSuccess: false);
            ValidateSymbol(key: "Weather_SkyQuality", expectedSuccess: false);
            ValidateSymbol(key: "Weather_SkyTemperature", expectedSuccess: false);
            ValidateSymbol(key: "Weather_StarFWHM", expectedSuccess: false);
            ValidateSymbol(key: "Weather_WindDirection", expectedSuccess: false);
            ValidateSymbol(key: "Weather_WindGust", expectedSuccess: false);
            ValidateSymbol(key: "Weather_WindSpeed", expectedSuccess: false);
        }

        [Test]
        public void SymbolBroker_UpdateDeviceInfo_Weather_Disconnected_HasNoSymbols() {
            // Arrange
            var info = new WeatherDataInfo {
                Connected = false,
            };

            // Act
            broker.UpdateDeviceInfo(new WeatherDataInfo { Connected = true, Temperature = -10.5 });
            broker.UpdateDeviceInfo(info);

            // Assert
            ValidateSymbol(key: "Weather_Connected", expectedSuccess: true, expectedValue: false);
            ValidateSymbol(key: "Weather_Temperature", expectedSuccess: false);
            ValidateSymbol(key: "Weather_AveragePeriod", expectedSuccess: false);
            ValidateSymbol(key: "Weather_CloudCover", expectedSuccess: false);
            ValidateSymbol(key: "Weather_DewPoint", expectedSuccess: false);
            ValidateSymbol(key: "Weather_Humidity", expectedSuccess: false);
            ValidateSymbol(key: "Weather_Pressure", expectedSuccess: false);
            ValidateSymbol(key: "Weather_RainRate", expectedSuccess: false);
            ValidateSymbol(key: "Weather_SkyBrightness", expectedSuccess: false);
            ValidateSymbol(key: "Weather_SkyQuality", expectedSuccess: false);
            ValidateSymbol(key: "Weather_SkyTemperature", expectedSuccess: false);
            ValidateSymbol(key: "Weather_StarFWHM", expectedSuccess: false);
            ValidateSymbol(key: "Weather_WindDirection", expectedSuccess: false);
            ValidateSymbol(key: "Weather_WindGust", expectedSuccess: false);
            ValidateSymbol(key: "Weather_WindSpeed", expectedSuccess: false);
        }

        [Test]
        public void SymbolBroker_UpdateDeviceInfo_Dome_Connected_HasSymbols() {
            // Arrange
            var info = new DomeInfo {
                Connected = true,
                Altitude = 15.3,
                Azimuth = 333.3,
                ShutterStatus = ShutterState.ShutterClosing,
            };

            // Act
            broker.UpdateDeviceInfo(info);

            // Assert
            ValidateSymbol(key: "Dome_ShutterStatus", expectedSuccess: true, expectedValue: (int)info.ShutterStatus);
            ValidateSymbol(key: "Dome_Altitude", expectedSuccess: true, expectedValue: info.Altitude);
            ValidateSymbol(key: "Dome_Azimuth", expectedSuccess: true, expectedValue: info.Azimuth);
            ValidateSymbol(key: "Dome_DomeAltitude", expectedSuccess: true, expectedValue: info.Altitude, true);
            ValidateSymbol(key: "Dome_DomeAzimuth", expectedSuccess: true, expectedValue: info.Azimuth, true);
        }

        [Test]
        public void SymbolBroker_UpdateDeviceInfo_Dome_Disconnected_HasNoSymbols() {
            // Arrange
            var info = new DomeInfo {
                Connected = false,
            };

            // Act
            broker.UpdateDeviceInfo(new DomeInfo { Connected = true });
            broker.UpdateDeviceInfo(info);

            // Assert
            ValidateSymbol(key: "Dome_Connected", expectedSuccess: true, expectedValue: false);
            ValidateSymbol(key: "Dome_ShutterStatus", expectedSuccess: false);
            ValidateSymbol(key: "Dome_Altitude", expectedSuccess: false);
            ValidateSymbol(key: "Dome_Azimuth", expectedSuccess: false);
            ValidateSymbol(key: "Dome_DomeAltitude", expectedSuccess: false);
            ValidateSymbol(key: "Dome_DomeAzimuth", expectedSuccess: false);
        }

        [Test]
        public void SymbolBroker_UpdateDeviceInfo_Guider_Disconnected_HasSymbols() {
            // Arrange
            var info = new GuiderInfo {
                Connected = true,
                RMSError = new RMSError(rA: 1.5, dec: 2.5, peakRA: 3.7, peakDec: 4.8, total: 2.0, scale: 1) {
                    
                },
            };

            // Act
            broker.UpdateDeviceInfo(info);

            // Assert
            ValidateSymbol(key: "Guider_Connected", expectedSuccess: true, expectedValue: true);
            ValidateSymbol(key: "Guider_PeakRA", expectedSuccess: true, 3.7);
            ValidateSymbol(key: "Guider_PeakDec", expectedSuccess: true, 4.8);
            ValidateSymbol(key: "Guider_RMSTotal", expectedSuccess: true, 2.0);
            ValidateSymbol(key: "Guider_RMSRA", expectedSuccess: true, 1.5);
            ValidateSymbol(key: "Guider_RMSDec", expectedSuccess: true, 2.5);
        }

        [Test]
        public void SymbolBroker_UpdateDeviceInfo_Guider_Disconnected_HasNoSymbols() {
            // Arrange
            var info = new GuiderInfo {
                Connected = false,
            };

            // Act
            broker.UpdateDeviceInfo(new GuiderInfo { Connected = true });
            broker.UpdateDeviceInfo(info);

            // Assert
            ValidateSymbol(key: "Guider_Connected", expectedSuccess: true, expectedValue: false);
            ValidateSymbol(key: "Guider_PeakRA", expectedSuccess: false);
            ValidateSymbol(key: "Guider_PeakDec", expectedSuccess: false);
            ValidateSymbol(key: "Guider_RMSRA", expectedSuccess: false);
            ValidateSymbol(key: "Guider_RMSDec", expectedSuccess: false);
        }

        [Test]
        public void SymbolBroker_UpdateDeviceInfo_SafetyMonitor_Connected_HasSymbols() {
            // Arrange
            profileServiceMock.SetupGet(x => x.ActiveProfile.SafetyMonitorSettings.Id).Returns("TestMonitor");
            var info = new SafetyMonitorInfo {
                Connected = true,
                IsSafe = true
            };

            // Act
            broker.UpdateDeviceInfo(info);

            // Assert
            ValidateSymbol(key: "Safety_IsSafe", expectedSuccess: true, expectedValue: info.IsSafe);
        }

        [Test]
        public void SymbolBroker_UpdateDeviceInfo_SafetyMonitor_Disconnected_HasSymbols() {
            // Arrange
            profileServiceMock.SetupGet(x => x.ActiveProfile.SafetyMonitorSettings.Id).Returns("TestMonitor");
            var info = new SafetyMonitorInfo {
                Connected = false,
            };

            // Act
            broker.UpdateDeviceInfo(info);

            // Assert
            ValidateSymbol(key: "Safety_IsSafe", expectedSuccess: true, expectedValue: false);
        }

        [Test]
        public void SymbolBroker_UpdateDeviceInfo_SafetyMonitor_NoDeviceRegistered_HasNoSymbols() {
            // Arrange
            profileServiceMock.SetupGet(x => x.ActiveProfile.SafetyMonitorSettings.Id).Returns("TestMonitor");
            var info = new SafetyMonitorInfo {
                Connected = false,
            };

            // Act
            broker.UpdateDeviceInfo(new SafetyMonitorInfo { Connected = true });
            profileServiceMock.SetupGet(x => x.ActiveProfile.SafetyMonitorSettings.Id).Returns("No_Device");
            broker.UpdateDeviceInfo(info);

            // Assert
            ValidateSymbol(key: "Safety_IsSafe", expectedSuccess: false);
        }

        [Test]
        public void SymbolBroker_UpdateDeviceInfo_RemoveAllSymbolsOfOneCategory_ValidSymbolsRemain() {
            // Arrange
            var cameraInfo = new CameraInfo {
                Connected = true,
                Temperature = -10.5,
            };
            var weatherDataInfo = new WeatherDataInfo {
                Connected = true,
                Temperature = -20.5,
                AveragePeriod = 10,
                CloudCover = 20,
                DewPoint = -15,
                Humidity = 50,
                Pressure = 1013,
            };

            // Act
            broker.UpdateDeviceInfo(weatherDataInfo);
            ValidateSymbol(key: "Weather_CloudCover", expectedSuccess: true, expectedValue: weatherDataInfo.CloudCover);

            broker.UpdateDeviceInfo(cameraInfo);
            broker.UpdateDeviceInfo(new WeatherDataInfo { Connected = false });

            // Assert
            ValidateSymbol(key: "Camera_Temperature", expectedSuccess: true, expectedValue: cameraInfo.Temperature);
            ValidateSymbol(key: "Camera_TemperatureSetPoint", expectedSuccess: true, expectedValue: cameraInfo.TemperatureSetPoint);
            ValidateSymbol(key: "Camera_PixelSize", expectedSuccess: true, expectedValue: cameraInfo.PixelSize, isHidden: true);
            ValidateSymbol(key: "Camera_XSize", expectedSuccess: true, expectedValue: cameraInfo.XSize, isHidden: true);
            ValidateSymbol(key: "Camera_YSize", expectedSuccess: true, expectedValue: cameraInfo.YSize, isHidden: true);
            ValidateSymbol(key: "Weather_CloudCover", expectedSuccess: false);
        }

        [Test]
        public void SymbolBroker_SetImageSymbols_HasSymbols() {
            // Arrange            
            var renderedImageMock = new Mock<IRenderedImage>();

            renderedImageMock.SetupGet(x => x.RawImageData.MetaData).Returns(new ImageMetaData() {
                Image = new ImageParameter() {
                    Id = 10,
                    ExposureTime = 120.5,
                    ImageType = "DARK",
                    RecordedRMS = new Core.Model.RMS() {
                        Total = 123.2
                    }
                },
                Camera = new CameraParameter() {
                    Gain = 139,
                    Offset = 25
                }
            });

            var starDetectionMock = new Mock<IStarDetectionAnalysis>();
            renderedImageMock.SetupGet(x => x.RawImageData.StarDetectionAnalysis).Returns(starDetectionMock.Object);
            starDetectionMock.SetupGet(x => x.DetectedStars).Returns(2222);
            starDetectionMock.SetupGet(x => x.HFR).Returns(2.5);

            var args = new ImagePreparedEventArgs {
                RenderedImage = renderedImageMock.Object,
                Parameters = new Core.Utility.PrepareImageParameters(true, true)
            };

            // Act
            broker.SetImageSymbols(this, args);

            // Assert
            ValidateSymbol(key: "Image_ImageId", expectedSuccess: true, expectedValue: 10);
            ValidateSymbol(key: "Image_ExposureTime", expectedSuccess: true, expectedValue: 120.5);
            ValidateSymbol(key: "Image_ImageType", expectedSuccess: true, expectedValue: "DARK");
            ValidateSymbol(key: "Image_Gain", expectedSuccess: true, expectedValue: 139);
            ValidateSymbol(key: "Image_Offset", expectedSuccess: true, expectedValue: 25);
            ValidateSymbol(key: "Image_RMS", expectedSuccess: true, expectedValue: 123.2);
            ValidateSymbol(key: "Image_HFR", expectedSuccess: true, expectedValue: 2.5);
            ValidateSymbol(key: "Image_StarCount", expectedSuccess: true, expectedValue: 2222);
        }

        [Test]
        public void SymbolBroker_SetImageSymbols_AdditionalFields_HasSymbols() {
            // Arrange            
            var renderedImageMock = new Mock<IRenderedImage>();
            var arcsecPerPixel = AstroUtil.ArcsecPerPixel(3.76, 952);
            profileServiceMock.SetupGet(x => x.ActiveProfile.CameraSettings.PixelSize).Returns(3.76);
            profileServiceMock.SetupGet(x => x.ActiveProfile.TelescopeSettings.FocalLength).Returns(952);

            renderedImageMock.SetupGet(x => x.RawImageData.MetaData).Returns(new ImageMetaData() {
                Image = new ImageParameter() {
                    Id = 10,
                    ExposureTime = 120.5,
                    ImageType = "DARK",
                    RecordedRMS = new Core.Model.RMS() {
                        Total = 123.2
                    }
                },
                Camera = new CameraParameter() {
                    Gain = 139,
                    Offset = 25
                }
            });

            var starDetectionMock = new Mock<IStarDetectionAnalysis>();
            renderedImageMock.SetupGet(x => x.RawImageData.StarDetectionAnalysis).Returns(starDetectionMock.Object);
            starDetectionMock.SetupGet(x => x.DetectedStars).Returns(2222);
            starDetectionMock.SetupGet(x => x.HFR).Returns(2.5);
            starDetectionMock.SetupGet(x => x.Eccentricity).Returns(3.5);
            starDetectionMock.SetupGet(x => x.FWHM).Returns(4.5);
            starDetectionMock.SetupGet(x => x.HFRStDev).Returns(1.5);
            starDetectionMock.SetupGet(x => x.HFRUnit).Returns(StarMeasurementUnit.Pixels);
            starDetectionMock.SetupGet(x => x.FWHMUnit).Returns(StarMeasurementUnit.Pixels);
            starDetectionMock.SetupGet(x => x.HFRStDevUnit).Returns(StarMeasurementUnit.Pixels);

            var args = new ImagePreparedEventArgs {
                RenderedImage = renderedImageMock.Object,
                Parameters = new Core.Utility.PrepareImageParameters(true, true)
            };

            // Act
            broker.SetImageSymbols(this, args);

            // Assert
            ValidateSymbol(key: "Image_ImageId", expectedSuccess: true, expectedValue: 10);
            ValidateSymbol(key: "Image_ExposureTime", expectedSuccess: true, expectedValue: 120.5);
            ValidateSymbol(key: "Image_ImageType", expectedSuccess: true, expectedValue: "DARK");
            ValidateSymbol(key: "Image_Gain", expectedSuccess: true, expectedValue: 139);
            ValidateSymbol(key: "Image_Offset", expectedSuccess: true, expectedValue: 25);
            ValidateSymbol(key: "Image_RMS", expectedSuccess: true, expectedValue: 123.2);
            ValidateSymbol(key: "Image_HFR", expectedSuccess: true, expectedValue: 2.5);
            ValidateSymbol(key: "Image_StarCount", expectedSuccess: true, expectedValue: 2222);
            ValidateSymbol(key: "Image_Eccentricity", expectedSuccess: true, expectedValue: 3.5);
            ValidateSymbol(key: "Image_FWHM", expectedSuccess: true, expectedValue: 4.5);
            ValidateSymbol(key: "Image_FWHMUnit", expectedSuccess: false);
            ValidateSymbol(key: "Image_FWHMPixels", expectedSuccess: true, expectedValue: 4.5, isHidden: true);
            ValidateSymbol(key: "Image_FWHMArcseconds", expectedSuccess: true, expectedValue: Math.Round(4.5 * arcsecPerPixel, 3), isHidden: true);
            ValidateSymbol(key: "Image_HFRStDev", expectedSuccess: true, expectedValue: 1.5);
            ValidateSymbol(key: "Image_HFRStDevUnit", expectedSuccess: false);
            ValidateSymbol(key: "Image_HFRStDevPixels", expectedSuccess: true, expectedValue: 1.5, isHidden: true);
            ValidateSymbol(key: "Image_HFRStDevArcseconds", expectedSuccess: true, expectedValue: Math.Round(1.5 * arcsecPerPixel, 3), isHidden: true);
            ValidateSymbol(key: "Image_HFRUnit", expectedSuccess: false);
            ValidateSymbol(key: "Image_HFRPixels", expectedSuccess: true, expectedValue: 2.5, isHidden: true);
            ValidateSymbol(key: "Image_HFRArcseconds", expectedSuccess: true, expectedValue: Math.Round(2.5 * arcsecPerPixel, 3), isHidden: true);
        }

        [Test]
        public void SymbolBroker_SetImageSymbols_LegacyAnalysisConcreteOptionalFields_HasSymbols() {
            // Arrange
            var renderedImageMock = new Mock<IRenderedImage>();
            var arcsecPerPixel = AstroUtil.ArcsecPerPixel(3.76, 952);
            profileServiceMock.SetupGet(x => x.ActiveProfile.CameraSettings.PixelSize).Returns(3.76);
            profileServiceMock.SetupGet(x => x.ActiveProfile.TelescopeSettings.FocalLength).Returns(952);

            renderedImageMock.SetupGet(x => x.RawImageData.MetaData).Returns(new ImageMetaData() {
                Image = new ImageParameter() {
                    Id = 10,
                    ExposureTime = 120.5,
                    ImageType = "LIGHT"
                },
                Camera = new CameraParameter() {
                    Gain = 139,
                    Offset = 25
                }
            });

            renderedImageMock.SetupGet(x => x.RawImageData.StarDetectionAnalysis).Returns(new LegacyStarDetectionAnalysis {
                DetectedStars = 2222,
                HFR = 2.5,
                FWHM = 4.5f,
                Eccentricity = 0.42m,
                HFRStDev = 1.5
            });

            var args = new ImagePreparedEventArgs {
                RenderedImage = renderedImageMock.Object,
                Parameters = new Core.Utility.PrepareImageParameters(true, true)
            };

            // Act
            broker.SetImageSymbols(this, args);

            // Assert
            ValidateSymbol(key: "Image_HFR", expectedSuccess: true, expectedValue: 2.5);
            ValidateSymbol(key: "Image_FWHM", expectedSuccess: true, expectedValue: 4.5);
            ValidateSymbol(key: "Image_FWHMUnit", expectedSuccess: false);
            ValidateSymbol(key: "Image_FWHMPixels", expectedSuccess: true, expectedValue: Math.Round(4.5 / arcsecPerPixel, 3), isHidden: true);
            ValidateSymbol(key: "Image_FWHMArcseconds", expectedSuccess: true, expectedValue: 4.5, isHidden: true);
            ValidateSymbol(key: "Image_Eccentricity", expectedSuccess: true, expectedValue: 0.42);
            ValidateSymbol(key: "Image_HFRStDev", expectedSuccess: true, expectedValue: 1.5);
            ValidateSymbol(key: "Image_HFRStDevUnit", expectedSuccess: false);
            ValidateSymbol(key: "Image_HFRStDevPixels", expectedSuccess: true, expectedValue: 1.5, isHidden: true);
            ValidateSymbol(key: "Image_HFRStDevArcseconds", expectedSuccess: true, expectedValue: Math.Round(1.5 * arcsecPerPixel, 3), isHidden: true);
            ValidateSymbol(key: "Image_HFRUnit", expectedSuccess: false);
            ValidateSymbol(key: "Image_HFRPixels", expectedSuccess: true, expectedValue: 2.5, isHidden: true);
            ValidateSymbol(key: "Image_HFRArcseconds", expectedSuccess: true, expectedValue: Math.Round(2.5 * arcsecPerPixel, 3), isHidden: true);
            ValidateSymbol(key: "Image_StarCount", expectedSuccess: true, expectedValue: 2222);
        }

        [Test]
        public async Task SymbolBroker_SetImageStatisticsSymbols_HasSymbols() {
            // Arrange
            var imageDataMock = new Mock<IImageData>();
            var imageStatisticsMock = new Mock<IImageStatistics>();
            imageStatisticsMock.SetupGet(x => x.Mean).Returns(123.45);
            imageStatisticsMock.SetupGet(x => x.Median).Returns(122.5);
            imageStatisticsMock.SetupGet(x => x.StDev).Returns(6.75);
            imageStatisticsMock.SetupGet(x => x.MedianAbsoluteDeviation).Returns(4.25);
            imageStatisticsMock.SetupGet(x => x.Min).Returns(10);
            imageStatisticsMock.SetupGet(x => x.Max).Returns(65000);
            imageStatisticsMock.SetupGet(x => x.MinOccurrences).Returns(11);
            imageStatisticsMock.SetupGet(x => x.MaxOccurrences).Returns(22);
            imageDataMock
                .SetupGet(x => x.Statistics)
                .Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(() => Task.FromResult(imageStatisticsMock.Object)));

            // Act
            await broker.SetImageStatisticsSymbolsAsync(imageDataMock.Object, 0);

            // Assert
            ValidateSymbol(key: "Image_Mean", expectedSuccess: true, expectedValue: 123.45);
            ValidateSymbol(key: "Image_Median", expectedSuccess: true, expectedValue: 122.5);
            ValidateSymbol(key: "Image_StDev", expectedSuccess: true, expectedValue: 6.75);
            ValidateSymbol(key: "Image_MAD", expectedSuccess: true, expectedValue: 4.25);
            ValidateSymbol(key: "Image_Min", expectedSuccess: true, expectedValue: 10);
            ValidateSymbol(key: "Image_Max", expectedSuccess: true, expectedValue: 65000);
            ValidateSymbol(key: "Image_MinOccurrences", expectedSuccess: true, expectedValue: 11);
            ValidateSymbol(key: "Image_MaxOccurrences", expectedSuccess: true, expectedValue: 22);
        }

        [Test]
        public async Task SymbolBroker_SetImageStatisticsSymbols_StaleImageVersion_DoesNotUpdateSymbols() {
            // Arrange
            var imageDataMock = new Mock<IImageData>();
            var imageStatisticsMock = new Mock<IImageStatistics>();
            imageStatisticsMock.SetupGet(x => x.Mean).Returns(123.45);
            imageDataMock
                .SetupGet(x => x.Statistics)
                .Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(() => Task.FromResult(imageStatisticsMock.Object)));

            // Act
            await broker.SetImageStatisticsSymbolsAsync(imageDataMock.Object, -1);

            // Assert
            ValidateSymbol(key: "Image_Mean", expectedSuccess: false);
        }

        [Test]
        public void SymbolBroker_SetImageSymbols_DoesNotWaitForImageStatistics() {
            // Arrange
            var imageDataMock = new Mock<IImageData>();
            imageDataMock.SetupGet(x => x.MetaData).Returns(new ImageMetaData() {
                Image = new ImageParameter() {
                    Id = 10,
                    ExposureTime = 120.5,
                    ImageType = "DARK"
                },
                Camera = new CameraParameter() {
                    Gain = 139,
                    Offset = 25
                }
            });

            var starDetectionMock = new Mock<IStarDetectionAnalysis>();
            imageDataMock.SetupGet(x => x.StarDetectionAnalysis).Returns(starDetectionMock.Object);
            starDetectionMock.SetupGet(x => x.DetectedStars).Returns(2222);
            starDetectionMock.SetupGet(x => x.HFR).Returns(2.5);

            var imageStatisticsMock = new Mock<IImageStatistics>();
            imageStatisticsMock.SetupGet(x => x.Mean).Returns(123.45);
            var statisticsCompletion = new TaskCompletionSource<IImageStatistics>(TaskCreationOptions.RunContinuationsAsynchronously);
            imageDataMock
                .SetupGet(x => x.Statistics)
                .Returns(new Nito.AsyncEx.AsyncLazy<IImageStatistics>(async () => await statisticsCompletion.Task));

            var renderedImageMock = new Mock<IRenderedImage>();
            renderedImageMock.SetupGet(x => x.RawImageData).Returns(imageDataMock.Object);

            var args = new ImagePreparedEventArgs {
                RenderedImage = renderedImageMock.Object,
                Parameters = new Core.Utility.PrepareImageParameters(true, true)
            };

            // Act
            var setImageSymbolsTask = Task.Run(() => broker.SetImageSymbols(this, args));

            try {
                // Assert
                setImageSymbolsTask.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue("image statistics must not hold up ImagePrepared handling");
                ValidateSymbol(key: "Image_ImageId", expectedSuccess: true, expectedValue: 10);
                ValidateSymbol(key: "Image_Mean", expectedSuccess: false);
            } finally {
                statisticsCompletion.TrySetResult(imageStatisticsMock.Object);
            }

            SpinWait.SpinUntil(() => broker.TryGetValue("Image_Mean", out _), TimeSpan.FromSeconds(5)).Should().BeTrue();
        }

        private class LegacyStarDetectionAnalysis : IStarDetectionAnalysis {
            public double HFR { get; set; }
            public float FWHM { get; set; }
            public decimal Eccentricity { get; set; }
            public double HFRStDev { get; set; }
            public int DetectedStars { get; set; }
            public List<NINA.Image.ImageAnalysis.DetectedStar> StarList { get; set; } = new List<NINA.Image.ImageAnalysis.DetectedStar>();

            double IStarDetectionAnalysis.FWHM {
                get => throw new InvalidOperationException("Legacy FWHM is only available as a concrete property.");
                set => throw new InvalidOperationException("Legacy FWHM is only available as a concrete property.");
            }

            double IStarDetectionAnalysis.Eccentricity {
                get => throw new InvalidOperationException("Legacy eccentricity is only available as a concrete property.");
                set => throw new InvalidOperationException("Legacy eccentricity is only available as a concrete property.");
            }

            public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged {
                add { }
                remove { }
            }
        }

        [Test]
        public void SymbolBroker_AmbiguityTest() {
            var info = new CameraInfo {
                Connected = true,
                TemperatureSetPoint = -15.0,
                Temperature = -10.5,
                XSize = 4000,
                YSize = 3000,
                PixelSize = 4.54
            };

            broker.UpdateDeviceInfo(info);
            ValidateSymbol("Camera_Connected", true, true);
            ValidateSymbol("FilterWheel_Connected", false, false);
            ValidateSymbol("Connected", true, true);

            broker.UpdateDeviceInfo(new CameraInfo { Connected = false });
            ValidateSymbol("Camera_Connected", true, false);
            ValidateSymbol("FilterWheel_Connected", false, false);
            ValidateSymbol("Connected", true, false);
        }

        [Test]
        public void SymbolBroker_GetSymbols_ReturnAllSymbols() {
            // Arrange
            var info = new CameraInfo {
                Connected = true,
                TemperatureSetPoint = -15.0,
                Temperature = -10.5,
                XSize = 4000,
                YSize = 3000,
                PixelSize = 4.54
            };

            // Act
            broker.UpdateDeviceInfo(info);
            var symbols = broker.GetSymbols();

            // Assert
            symbols.Should().NotBeNull();
            symbols.Should().Contain(x => x.Category == "NINA");
            symbols.Should().Contain(x => x.Category == "Camera");
            symbols.Should().NotContain(x => x.Category == "Camera" && x.Key == "XSize");
            ValidateSymbol("Camera_Connected", true, true);

            // Act 2
            broker.UpdateDeviceInfo(new CameraInfo { Connected = false });
            var symbols2 = broker.GetSymbols();

            // Assert 2
            symbols2.Should().NotBeNull();
            symbols2.Should().Contain(x => x.Category == "NINA");
            ValidateSymbol("Camera_Connected", true, false);
            symbols2.Should().NotContain(x => x.Category == "Camera" && x.Key == "XSize");
        }

        [Test]
        public void SymbolBroker_RegisterSymbolProvider_Successful() {
            // Act
            var provider = broker.RegisterSymbolProvider("TestProvider");

            // Assert
            provider.Should().NotBeNull();
            provider.GetProviderName().Should().Be("TestProvider");
        }

        [Test]
        public void SymbolBroker_RegisterSymbolProviderTwice_Fails() {
            // Arrange
            broker.RegisterSymbolProvider("TestProvider");

            // Act
            Action act = () => broker.RegisterSymbolProvider("TestProvider");

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void SymbolBroker_IsProviderRegistered_ReturnsRegistrationState() {
            broker.IsProviderRegistered("Camera").Should().BeTrue();
            broker.IsProviderRegistered("TestProvider").Should().BeFalse();

            broker.RegisterSymbolProvider("TestProvider");

            broker.IsProviderRegistered("TestProvider").Should().BeTrue();
            broker.IsProviderRegistered("testprovider").Should().BeTrue();
            broker.IsProviderRegistered(null).Should().BeFalse();
        }

        [Test]
        public void SymbolBroker_AddOrUpdateSymbol_NoProvider_Fails() {
            // Act
            Action act = () => broker.AddOrUpdateSymbol(null, "MySymbol", 123.45);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void SymbolBroker_AddOrUpdateSymbol_WithSymbolArray_NoProvider_Fails() {
            // Act
            Action act = () => broker.AddOrUpdateSymbol(null, "MySymbol", 123.45, []);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void SymbolBroker_AddOrUpdateSymbol_SuccessfullyAdded() {
            // Arrange
            var provider = broker.RegisterSymbolProvider("Test");

            // Act
            broker.AddOrUpdateSymbol(provider, "MySymbol", 123.45);

            // Assert
            ValidateSymbol("Test_MySymbol", true, 123.45, false);
        }

        [Test]
        public void SymbolBroker_SymbolEvents_ReportAddUpdateAndRemove() {
            List<SymbolChangedEventArgs> added = new List<SymbolChangedEventArgs>();
            List<SymbolChangedEventArgs> updated = new List<SymbolChangedEventArgs>();
            List<SymbolChangedEventArgs> removed = new List<SymbolChangedEventArgs>();
            broker.SymbolAdded += (sender, args) => added.Add(args);
            broker.SymbolUpdated += (sender, args) => updated.Add(args);
            broker.SymbolRemoved += (sender, args) => removed.Add(args);
            var provider = broker.RegisterSymbolProvider("Events");

            provider.AddOrUpdateSymbol("Value", 1);
            provider.AddOrUpdateSymbol("Value", 1);
            provider.AddOrUpdateSymbol("Value", 2);
            provider.RemoveSymbol("Value").Should().BeTrue();

            added.Should().ContainSingle();
            added[0].ChangeKind.Should().Be(SymbolChangeKind.Added);
            added[0].ProviderName.Should().Be("Events");
            added[0].Key.Should().Be("Value");
            added[0].QualifiedKey.Should().Be("Events_Value");
            added[0].OldValue.Should().BeNull();
            added[0].NewValue.Should().Be(1);

            updated.Should().ContainSingle();
            updated[0].ChangeKind.Should().Be(SymbolChangeKind.Updated);
            updated[0].OldValue.Should().Be(1);
            updated[0].NewValue.Should().Be(2);

            removed.Should().ContainSingle();
            removed[0].ChangeKind.Should().Be(SymbolChangeKind.Removed);
            removed[0].OldValue.Should().Be(2);
            removed[0].NewValue.Should().BeNull();
        }

        [Test]
        public void SymbolProvider_SymbolEvents_AreFilteredToProvider() {
            var provider1 = broker.RegisterSymbolProvider("ProviderOne");
            var provider2 = broker.RegisterSymbolProvider("ProviderTwo");
            List<SymbolChangedEventArgs> added = new List<SymbolChangedEventArgs>();
            object eventSender = new object();
            provider1.SymbolAdded += (sender, args) => {
                eventSender = sender ?? new object();
                added.Add(args);
            };

            provider2.AddOrUpdateSymbol("Value", 1);
            provider1.AddOrUpdateSymbol("Value", 2);

            added.Should().ContainSingle();
            added[0].ProviderName.Should().Be("ProviderOne");
            added[0].NewValue.Should().Be(2);
            eventSender.Should().BeSameAs(provider1);
        }

        [Test]
        public void SymbolBroker_SymbolEvents_ContinueAfterSubscriberFailure() {
            var provider = broker.RegisterSymbolProvider("Events");
            int delivered = 0;
            broker.SymbolAdded += (sender, args) => throw new InvalidOperationException("subscriber failed");
            broker.SymbolAdded += (sender, args) => delivered++;

            Action act = () => provider.AddOrUpdateSymbol("Value", 1);

            act.Should().NotThrow();
            delivered.Should().Be(1);
        }

        [Test]
        public void SymbolProvider_SymbolEvents_ContinueAfterSubscriberFailure() {
            var provider = broker.RegisterSymbolProvider("Events");
            int delivered = 0;
            provider.SymbolAdded += (sender, args) => throw new InvalidOperationException("subscriber failed");
            provider.SymbolAdded += (sender, args) => delivered++;

            Action act = () => provider.AddOrUpdateSymbol("Value", 1);

            act.Should().NotThrow();
            delivered.Should().Be(1);
        }

        [Test]
        public void SymbolBroker_RemoveSymbol_NoProvider_Fails() {
            // Act
            Action act = () => broker.RemoveSymbol(null, "MySymbol");

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void SymbolBroker_RemoveSymbol_SuccessfullyAddedAndRemoved() {
            // Arrange
            var provider = broker.RegisterSymbolProvider("Test");
            broker.AddOrUpdateSymbol(provider, "MySymbol", 123.45);
            ValidateSymbol("Test_MySymbol", true, 123.45, false);

            // Act
            var success = broker.RemoveSymbol(provider, "MySymbol");

            // Assert
            success.Should().BeTrue();
            ValidateSymbol("Test_MySymbol", false);
        }

        [Test]
        public void SymbolBroker_RemoveSymbol_Ambiguous_SuccessfullyAddedAndRemoved() {
            // Arrange
            var provider = broker.RegisterSymbolProvider("Test");
            broker.AddOrUpdateSymbol(provider, "MySymbol", 123.45);
            ValidateSymbol("Test_MySymbol", true, 123.45, false);

            var provider2 = broker.RegisterSymbolProvider("Test2");
            broker.AddOrUpdateSymbol(provider2, "MySymbol", 543.21);
            ValidateSymbol("Test_MySymbol", true, 123.45, false);

            // Act
            var success = broker.RemoveSymbol(provider, "MySymbol");

            // Assert
            success.Should().BeTrue();
            ValidateSymbol("Test2_MySymbol", true, 543.21, false);
        }

        [Test]
        public void SymbolBroker_GetHiddenSymbols_ReturnAllHiddenSymbols() {
            // Arrange
            var info = new CameraInfo {
                Connected = true,
                TemperatureSetPoint = -15.0,
                Temperature = -10.5,
                XSize = 4000,
                YSize = 3000,
                PixelSize = 4.54
            };

            // Act
            broker.UpdateDeviceInfo(info);
            var symbols = broker.GetHiddenSymbols("Camera");

            // Assert
            symbols.Should().NotBeNull();
            symbols.Should().Contain(x => x.Category == "Camera" && x.Key == "XSize" && x.Type == Symbol.SymbolType.SYMBOL_HIDDEN);
        }

        [Test]
        public void SymbolBroker_GetHiddenSymbols_ReturnsSnapshot() {
            // Arrange
            var info = new CameraInfo {
                Connected = true,
                TemperatureSetPoint = -15.0,
                Temperature = -10.5,
                XSize = 4000,
                YSize = 3000,
                PixelSize = 4.54
            };

            // Act
            broker.UpdateDeviceInfo(info);
            var symbols = broker.GetHiddenSymbols("Camera");
            symbols.Clear();

            // Assert
            broker.GetHiddenSymbols("Camera")
                .Should()
                .Contain(x => x.Category == "Camera" && x.Key == "XSize" && x.Type == Symbol.SymbolType.SYMBOL_HIDDEN);
        }

        [Test]
        public async Task SymbolBroker_ConcurrentSymbolSnapshotsAndUpdates_DoNotThrow() {
            // Arrange
            ISymbolProvider provider = broker.RegisterSymbolProvider("ConcurrentProvider");
            provider.AddOrUpdateSymbol("Value", 0);
            provider.AddOrUpdateHiddenSymbol("HiddenValue", 0, Array.Empty<Symbol>());
            ConcurrentQueue<Exception> exceptions = new ConcurrentQueue<Exception>();

            Task updater = Task.Run(() => {
                try {
                    for (int i = 0; i < 10000; i++) {
                        provider.AddOrUpdateSymbol("Value", i);
                        provider.AddOrUpdateHiddenSymbol("HiddenValue", i, Array.Empty<Symbol>());

                        if (i % 5 == 0) {
                            provider.RemoveSymbol("Value");
                        }
                    }
                } catch (Exception ex) {
                    exceptions.Enqueue(ex);
                }
            });

            Task[] readers = Enumerable.Range(0, 16)
                .Select(index => Task.Run(() => {
                    try {
                        for (int i = 0; i < 10000; i++) {
                            broker.TryGetValue("ConcurrentProvider_Value", out _);
                            broker.TryGetSymbol("ConcurrentProvider_HiddenValue", out _);
                            broker.GetSymbols();
                            broker.GetHiddenSymbols("ConcurrentProvider");
                        }
                    } catch (Exception ex) {
                        exceptions.Enqueue(ex);
                    }
                }))
                .ToArray();

            // Act
            await Task.WhenAll(readers.Concat(new[] { updater }));

            // Assert
            exceptions.Should().BeEmpty();
        }

        [Test]
        public void SymbolBroker_GetMyProviders_ReturnsOnlyProvidersRegisteredByCallingAssembly() {
            // Arrange
            var provider1 = broker.RegisterSymbolProvider("TestProvider1");
            var provider2 = broker.RegisterSymbolProvider("TestProvider2");

            // Act
            var myProviders = broker.GetMyProviders();

            // Assert
            myProviders.Should().NotBeNull();
            myProviders.Count.Should().BeGreaterOrEqualTo(2);

            var providerNames = myProviders.Select(p => p.GetProviderName()).ToList();
            providerNames.Should().Contain("TestProvider1");
            providerNames.Should().Contain("TestProvider2");
        }

        [Test]
        public void SymbolBroker_GetMyProviders_DoesNotReturnInternalProviders() {
            // Arrange
            var provider = broker.RegisterSymbolProvider("MyCustomProvider");

            // Act
            var myProviders = broker.GetMyProviders();

            // Assert
            var providerNames = myProviders.Select(p => p.GetProviderName()).ToList();

            // Should NOT contain NINA's internal providers (registered by SymbolBroker constructor)
            providerNames.Should().NotContain("NINA");
            providerNames.Should().NotContain("Image");
            providerNames.Should().NotContain("Camera");
            providerNames.Should().NotContain("Mount");

            // Should contain our custom provider
            providerNames.Should().Contain("MyCustomProvider");
        }

        [Test]
        public void SymbolBroker_GetMyProviders_ReturnsReadOnlyCollection() {
            // Arrange
            var provider = broker.RegisterSymbolProvider("TestProvider");

            // Act
            var myProviders = broker.GetMyProviders();

            // Assert
            myProviders.Should().NotBeNull();
            myProviders.Should().BeAssignableTo<System.Collections.Generic.IReadOnlyCollection<ISymbolProvider>>();
        }

        [Test]
        [TestCase("MySwitch", "MySwitch")]
        [TestCase("My Switch", "My_Switch")]
        [TestCase("Switch #1", "Switch__1")]
        [TestCase("123Test", "_123Test")]
        [TestCase("_private", "_private")]
        [TestCase("test_var", "test_var")]
        [TestCase("A1B2C3", "A1B2C3")]
        [TestCase("switch-1", "switch_1")]
        [TestCase("switch+1", "switch_1")]
        [TestCase("my@switch", "my_switch")]
        [TestCase("my.switch", "my_switch")]
        [TestCase("my$switch", "my_switch")]
        public void SymbolBroker_SanitizeIdentifier_ValidIdentifiers(string input, string expected) {
            // Act
            var result = SymbolBroker.SanitizeIdentifier(input);

            // Assert
            result.Should().Be(expected);

            // Verify it matches VALID_SYMBOL regex
            SymbolProvider.ValidSymbolRegex.IsMatch(result)
                .Should().BeTrue($"{result} should be a valid NCalc identifier");
        }

        [Test]
        public void SymbolBroker_SanitizeIdentifier_NullString_ReturnsDefault() {
            // Act
            var result = SymbolBroker.SanitizeIdentifier(null);

            // Assert
            result.Should().Be("__Null__");
            SymbolProvider.ValidSymbolRegex.IsMatch(result)
                .Should().BeTrue();
        }

        [Test]
        public void SymbolBroker_SanitizeIdentifier_EmptyString_ReturnsDefault() {
            // Act
            var result = SymbolBroker.SanitizeIdentifier("");

            // Assert
            result.Should().Be("__Empty__");
            SymbolProvider.ValidSymbolRegex.IsMatch(result)
                .Should().BeTrue();
        }

        [Test]
        [TestCase("!!!")]
        [TestCase("@@@")]
        [TestCase("   ")]
        public void SymbolBroker_SanitizeIdentifier_AllSpecialChars_ReturnsUnderscores(string input) {
            // Act
            var result = SymbolBroker.SanitizeIdentifier(input);

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().MatchRegex("^_+$", "should be only underscores");
            SymbolProvider.ValidSymbolRegex.IsMatch(result)
                .Should().BeTrue($"{result} should be a valid NCalc identifier");
        }

        [Test]
        public void SymbolBroker_SanitizeIdentifier_StartsWithDigit_PrependsUnderscore() {
            // Act
            var result = SymbolBroker.SanitizeIdentifier("1stSwitch");

            // Assert
            result.Should().Be("_1stSwitch");
            result[0].Should().Be('_', "first character should be underscore when original starts with digit");
            SymbolProvider.ValidSymbolRegex.IsMatch(result)
                .Should().BeTrue();
        }

        [Test]
        public void SymbolBroker_SanitizeIdentifier_PreservesCase() {
            // Act
            var result = SymbolBroker.SanitizeIdentifier("MyTestSwitch");

            // Assert
            result.Should().Be("MyTestSwitch");
        }

        [Test]
        public void SymbolBroker_SanitizeIdentifier_RealWorldSwitchNames() {
            // Test real-world switch names that might come from devices
            var testCases = new Dictionary<string, string> {
                { "Power Switch #1", "Power_Switch__1" },
                { "12V Output", "_12V_Output" },
                { "Flat-Panel PWM", "Flat_Panel_PWM" },
                { "Dew Heater (A)", "Dew_Heater__A_" },
                { "USB Hub 2.0", "USB_Hub_2_0" }
            };

            foreach (var kvp in testCases) {
                var result = SymbolBroker.SanitizeIdentifier(kvp.Key);
                result.Should().Be(kvp.Value, $"for input '{kvp.Key}'");
                SymbolProvider.ValidSymbolRegex.IsMatch(result)
                    .Should().BeTrue($"{result} should be a valid NCalc identifier");
            }
        }
    }
}
