#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using Moq;
using NINA.Model;
using NINA.Model.MyFlatDevice;
using NINA.Model.MyGuider;
using NINA.Model.MyTelescope;
using NINA.Profile;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel;
using NINA.ViewModel.Equipment.FlatDevice;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace NINATest {

    [TestFixture]
    public class SequenceVMTest {
        public TestContext TestContext { get; set; }

        private Mock<IProfileService> profileServiceMock;
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<ITelescopeMediator> telescopeMediatorMock;
        private Mock<IFocuserMediator> focuserMediatorMock;
        private Mock<IFilterWheelMediator> filterWheelMediatorMock;
        private Mock<IGuiderMediator> guiderMediatorMock;
        private Mock<IRotatorMediator> rotatorMediatorMock;
        private Mock<IWeatherDataMediator> weatherDataMediatorMock;
        private Mock<IImagingMediator> imagingMediatorMock;
        private Mock<IApplicationStatusMediator> applicationStatusMediatorMock;
        private Mock<IFlatDeviceMediator> _flatDeviceMediatorMock;
        private FlatDeviceInfo _flatDevice;
        private CaptureSequenceList _dummyList;
        private SequenceVM _sut;

        [SetUp]
        public void SequenceVM_TestInit() {
            profileServiceMock = new Mock<IProfileService>();
            profileServiceMock.SetupProperty(m => m.ActiveProfile.ImageFileSettings.FilePath, TestContext.CurrentContext.TestDirectory);
            profileServiceMock.SetupProperty(m => m.ActiveProfile.SequenceSettings.TemplatePath, TestContext.CurrentContext.TestDirectory);
            profileServiceMock.SetupProperty(m => m.ActiveProfile.AstrometrySettings.Longitude, 0);
            profileServiceMock.SetupProperty(m => m.ActiveProfile.AstrometrySettings.Latitude, 0);
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FocuserSettings.AutoFocusDisableGuiding, true);

            cameraMediatorMock = new Mock<ICameraMediator>();
            telescopeMediatorMock = new Mock<ITelescopeMediator>();
            focuserMediatorMock = new Mock<IFocuserMediator>();
            filterWheelMediatorMock = new Mock<IFilterWheelMediator>();
            guiderMediatorMock = new Mock<IGuiderMediator>();
            rotatorMediatorMock = new Mock<IRotatorMediator>();
            _flatDeviceMediatorMock = new Mock<IFlatDeviceMediator>();
            weatherDataMediatorMock = new Mock<IWeatherDataMediator>();
            imagingMediatorMock = new Mock<IImagingMediator>();
            applicationStatusMediatorMock = new Mock<IApplicationStatusMediator>();

            _dummyList = new CaptureSequenceList();
            _dummyList.Add(new CaptureSequence() { TotalExposureCount = 10 });
            _dummyList.Add(new CaptureSequence() { TotalExposureCount = 20 });
            _dummyList.Add(new CaptureSequence() { TotalExposureCount = 5 });

            _flatDevice = new FlatDeviceInfo() {
                Brightness = 1.0,
                Connected = true,
                CoverState = CoverState.Open,
                Description = "Some description",
                DriverInfo = "Some driverInfo",
                LightOn = false,
                DriverVersion = "200",
                MaxBrightness = 255,
                MinBrightness = 0,
                Name = "Some name",
                SupportsOpenClose = true
            };

            _sut = new SequenceVM(profileServiceMock.Object, cameraMediatorMock.Object, telescopeMediatorMock.Object, focuserMediatorMock.Object,
                filterWheelMediatorMock.Object, guiderMediatorMock.Object, rotatorMediatorMock.Object, _flatDeviceMediatorMock.Object,
                weatherDataMediatorMock.Object, imagingMediatorMock.Object, applicationStatusMediatorMock.Object);
        }

        [TearDown]
        public void Cleanup() {
        }

        [Test]
        public void Sequence_ConsumerRegistered() {
            telescopeMediatorMock.Verify(m => m.RegisterConsumer(_sut));
            focuserMediatorMock.Verify(m => m.RegisterConsumer(_sut));
            filterWheelMediatorMock.Verify(m => m.RegisterConsumer(_sut));
            rotatorMediatorMock.Verify(m => m.RegisterConsumer(_sut));
            weatherDataMediatorMock.Verify(m => m.RegisterConsumer(_sut));
            guiderMediatorMock.Verify(m => m.RegisterConsumer(_sut));
            _flatDeviceMediatorMock.Verify(m => m.RegisterConsumer(_sut));
        }

        [Test]
        public async Task ProcessSequence_Default() {
            _sut.UpdateDeviceInfo(_flatDevice);
            profileServiceMock.SetupProperty(m => m.ActiveProfile.FlatDeviceSettings.CloseAtSequenceEnd, true);
            //Act
            await _sut.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            Assert.That(_sut.SelectedSequenceRowIdx, Is.EqualTo(0));
            Assert.That(_sut.IsPaused, Is.False);
            Assert.That(_sut.Sequence.IsRunning, Is.False);
            _flatDeviceMediatorMock.Verify(m => m.CloseCover(), Times.Once);
        }

        [Test]
        public async Task ProcessSequence_StartOptions_DontSlewToTargetTest() {
            _dummyList.SlewToTarget = false;
            _sut.Sequence = _dummyList;

            //Act
            await _sut.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            telescopeMediatorMock.Verify(m => m.SlewToCoordinatesAsync(It.IsAny<Coordinates>()), Times.Never);
        }

        [Test]
        public async Task ProcessSequence_StartOptions_CoordinatesSlewTest() {
            _sut.UpdateDeviceInfo(new TelescopeInfo() { Connected = true });
            _dummyList.CenterTarget = true;
            var coordinates = new Coordinates(10, 10, Epoch.J2000, Coordinates.RAType.Degrees);
            _dummyList.Coordinates = coordinates;

            _sut.Sequence = _dummyList;

            //Act
            await _sut.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            telescopeMediatorMock.Verify(m => m.SlewToCoordinatesAsync(coordinates));
        }

        [Test]
        public async Task ProcessSequence_StartOptions_StartGuidingTest() {
            _sut.UpdateDeviceInfo(new GuiderInfo() { Connected = true });
            _dummyList.StartGuiding = true;
            _sut.Sequence = _dummyList;

            //Act
            await _sut.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            guiderMediatorMock.Verify(m => m.StartGuiding(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task ProcessSequence_StartOptions_DontStartGuidingTest() {
            _dummyList.StartGuiding = false;
            _sut.Sequence = _dummyList;

            //Act
            await _sut.StartSequenceCommand.ExecuteAsync(null);

            //Assert
            guiderMediatorMock.Verify(m => m.StartGuiding(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void ProcessSequence_AddSequenceCommand() {
            //Act
            _sut.AddSequenceRowCommand.Execute(null);

            //Assert
            Assert.That(_sut.SelectedSequenceRowIdx, Is.EqualTo(1));
            Assert.That(_sut.Sequence.Count, Is.EqualTo(2));
        }

        [Test]
        public void ProcessSequence_OnEmptySequence_AddSequenceCommand() {
            //Arrange
            _sut.Sequence = new CaptureSequenceList();

            //Act
            _sut.AddSequenceRowCommand.Execute(null);

            //Assert
            Assert.That(_sut.SelectedSequenceRowIdx, Is.EqualTo(0));
            Assert.That(_sut.Sequence.Count, Is.EqualTo(1));
        }

        [Test]
        public void ProcessSequence_RemoveSequenceCommand() {
            //Act
            _sut.RemoveSequenceRowCommand.Execute(null);

            //Assert
            Assert.That(_sut.SelectedSequenceRowIdx, Is.EqualTo(-1));
            Assert.That(_sut.Sequence.Count, Is.EqualTo(0));
        }

        [Test]
        public void ProcessSequence_OnEmptySequence_RemoveSequenceCommand() {
            //Act
            _sut.RemoveSequenceRowCommand.Execute(null);
            _sut.RemoveSequenceRowCommand.Execute(null);

            //Assert
            Assert.That(_sut.SelectedSequenceRowIdx, Is.EqualTo(-1));
            Assert.That(_sut.Sequence.Count, Is.EqualTo(0));
        }
    }
}