using Moq;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.ViewModel.ImageHistory;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Sequencer.Trigger.Autofocus;
using FluentAssertions;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Sequencer.Interfaces;
using NINA.WPF.Base.Model;
using NINA.WPF.Base.Utility.AutoFocus;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Image.ImageData;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem.Autofocus;
using NINA.Core.Utility.WindowService;
using System.Windows;

namespace NINA.Test.Sequencer.Trigger.Autofocus {
    [TestFixture]
    public class AuftofocusAfterExposuresTriggerTest {
        private Mock<IProfileService> profileServiceMock;
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<IFilterWheelMediator> filterWheelMediatorMock;
        private Mock<IFocuserMediator> focuserMediatorMock;
        private Mock<IAutoFocusVMFactory> autoFocusVMFactoryMock;
        private Mock<IImageSaveMediator> imageSaveMediatorMock;
        private ImageHistoryVM imagehistory;

        [SetUp]
        public void Setup() {
            profileServiceMock = new Mock<IProfileService>();
            cameraMediatorMock = new Mock<ICameraMediator>();
            filterWheelMediatorMock = new Mock<IFilterWheelMediator>();
            focuserMediatorMock = new Mock<IFocuserMediator>();
            autoFocusVMFactoryMock = new Mock<IAutoFocusVMFactory>();
            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo { Connected = true });
            focuserMediatorMock.Setup(x => x.GetInfo()).Returns(new FocuserInfo { Connected = true });
            imageSaveMediatorMock = new Mock<IImageSaveMediator>();

            var autoFocusVM = new Mock<IAutoFocusVM>();
            autoFocusVM.Setup(x => x.StartAutoFocus(It.IsAny<FilterInfo>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<ApplicationStatus>>())).ReturnsAsync(new AutoFocusReport() { Timestamp = DateTime.Now });
            autoFocusVMFactoryMock.Setup(x => x.Create()).Returns(autoFocusVM.Object);

            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusExposureTime).Returns(2);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps).Returns(4);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.AutoFocusNumberOfFramesPerPoint).Returns(2);
            profileServiceMock.SetupGet(x => x.ActiveProfile.FocuserSettings.FocuserSettleTime).Returns(1);
            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageHistorySettings.ImageHistoryLeftSelected).Returns(Core.Enum.ImageHistoryEnum.HFR);
            profileServiceMock.SetupGet(x => x.ActiveProfile.ImageHistorySettings.ImageHistoryRightSelected).Returns(Core.Enum.ImageHistoryEnum.Stars);

            imagehistory = new ImageHistoryVM(profileServiceMock.Object, imageSaveMediatorMock.Object);
        }        

        [Test]
        public void CloneTest() {
            var initial = new AutofocusAfterExposures(profileServiceMock.Object, imagehistory, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object);
            initial.Icon = new System.Windows.Media.GeometryGroup();

            var sut = (AutofocusAfterExposures)initial.Clone();

            sut.Should().NotBeSameAs(initial);
            sut.Icon.Should().BeSameAs(initial.Icon);
        }

        [Test]
        [TestCase(1, 1, true)]
        [TestCase(10, 10, true)]
        [TestCase(10, 20, true)]
        [TestCase(10, 5, false)]
        [TestCase(10, 30, true)]
        [TestCase(10, 15, false)]
        [TestCase(10, 11, false)]
        [TestCase(10, 9, false)]
        [TestCase(13, 13, true)]
        [TestCase(100, 205, false)]
        public async Task ShouldTrigger_OneAFBeforeFirstExposure_TriggersAsExpected(int afterExposures, double exposuresToAdd, bool shouldTrigger) {
            var afHistory = new AsyncObservableCollection<ImageHistoryPoint>();
            var report = new AutoFocusReport() { Timestamp = DateTime.Now - TimeSpan.FromMinutes(10) };
            imagehistory.AppendAutoFocusPoint(report);

            var afTrigger = new AutofocusAfterExposures(profileServiceMock.Object, imagehistory, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object);
            afTrigger.AfterExposures = afterExposures;

            afTrigger.SequenceBlockInitialize();

            for (int i = 0; i < exposuresToAdd; i++) {
                var id = imagehistory.GetNextImageId();
                imagehistory.Add(id, "LIGHT");
                imagehistory.PopulateStatistics(i, ImageStatistics.Create(new ImageProperties(2, 2, 16, false, 100, 200), new ushort[] { 300, 400, 600, 800 }));
            }

            var itemMock = new Mock<IExposureItem>();
            itemMock.SetupGet(x => x.ImageType).Returns("LIGHT");
            var should = afTrigger.ShouldTrigger(null, itemMock.Object);

            should.Should().Be(shouldTrigger);
        }

        [Test]
        [TestCase(1, 1, true)]
        [TestCase(10, 10, true)]
        [TestCase(10, 20, true)]
        [TestCase(10, 30, true)]
        [TestCase(10, 5, false)]
        [TestCase(10, 15, false)]
        [TestCase(10, 11, false)]
        [TestCase(10, 9, false)]
        [TestCase(13, 13, true)]
        [TestCase(100, 205, false)]
        public async Task ShouldTrigger_OneAFAfterThreeExposures_TriggersAsExpected(int afterExposures, double exposuresToAdd, bool shouldTrigger) {
            for (int i = 0; i < 3; i++) {
                var id = imagehistory.GetNextImageId();
                imagehistory.Add(id, "LIGHT");
                imagehistory.PopulateStatistics(i, ImageStatistics.Create(new ImageProperties(2, 2, 16, false, 100, 200), new ushort[] { 300, 400, 600, 800 }));
            }
            var afHistory = new AsyncObservableCollection<ImageHistoryPoint>();
            var report = new AutoFocusReport() { Timestamp = DateTime.Now - TimeSpan.FromMinutes(10) };
            imagehistory.AppendAutoFocusPoint(report);

            var afTrigger = new AutofocusAfterExposures(profileServiceMock.Object, imagehistory, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object);
            afTrigger.AfterExposures = afterExposures;

            afTrigger.SequenceBlockInitialize();

            for (int i = 0; i < exposuresToAdd; i++) {
                var id = imagehistory.GetNextImageId();
                imagehistory.Add(id, "LIGHT");
                imagehistory.PopulateStatistics(i, ImageStatistics.Create(new ImageProperties(2, 2, 16, false, 100, 200), new ushort[] { 300, 400, 600, 800 }));
            }

            var itemMock = new Mock<IExposureItem>();
            itemMock.SetupGet(x => x.ImageType).Returns("LIGHT");
            var should = afTrigger.ShouldTrigger(null, itemMock.Object);

            should.Should().Be(shouldTrigger);
        }

        [Test]
        [TestCase(1, 1, true)]
        [TestCase(10, 10, true)]
        [TestCase(10, 20, true)]
        [TestCase(10, 30, true)]
        [TestCase(10, 5, false)]
        [TestCase(10, 15, false)]
        [TestCase(10, 11, false)]
        [TestCase(10, 9, false)]
        [TestCase(13, 13, true)]
        [TestCase(100, 205, false)]
        public async Task ShouldTrigger_OneAFAfterThreeExposures_TriggersAsExpected_ButNotTwice(int afterExposures, double exposuresToAdd, bool shouldTrigger) {
            for (int i = 0; i < 3; i++) {
                var id = imagehistory.GetNextImageId();
                imagehistory.Add(id, "LIGHT");
                imagehistory.PopulateStatistics(i, ImageStatistics.Create(new ImageProperties(2, 2, 16, false, 100, 200), new ushort[] { 300, 400, 600, 800 }));
            }
            var afHistory = new AsyncObservableCollection<ImageHistoryPoint>();
            var report = new AutoFocusReport() { Timestamp = DateTime.Now - TimeSpan.FromMinutes(10) };
            imagehistory.AppendAutoFocusPoint(report);

            var afTrigger = new AutofocusAfterExposures(profileServiceMock.Object, imagehistory, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object);
            var windowServiceFactory = new Mock<IWindowServiceFactory>();
            var windowService = new Mock<IWindowService>();
            windowService.Setup(x => x.Show(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<ResizeMode>(), It.IsAny<WindowStyle>()));
            windowServiceFactory.Setup(x => x.Create()).Returns(windowService.Object);
            (afTrigger.TriggerRunner.Items.First() as RunAutofocus).WindowServiceFactory = windowServiceFactory.Object;
            afTrigger.AfterExposures = afterExposures;

            afTrigger.SequenceBlockInitialize();

            for (int i = 0; i < exposuresToAdd; i++) {
                var id = imagehistory.GetNextImageId();
                imagehistory.Add(id, "LIGHT");
                imagehistory.PopulateStatistics(i, ImageStatistics.Create(new ImageProperties(2, 2, 16, false, 100, 200), new ushort[] { 300, 400, 600, 800 }));
            }

            var itemMock = new Mock<IExposureItem>();
            itemMock.SetupGet(x => x.ImageType).Returns("LIGHT");
            var should = afTrigger.ShouldTrigger(null, itemMock.Object);

            should.Should().Be(shouldTrigger);
            if(should) {
                await afTrigger.Execute(default, default, default);
            }

            afTrigger.ShouldTrigger(null, itemMock.Object).Should().BeFalse();
        }

        [Test]
        [TestCase(1, 1, true)]
        [TestCase(10, 10, true)]
        [TestCase(10, 20, true)]
        [TestCase(10, 30, true)]
        [TestCase(10, 5, false)]
        [TestCase(10, 15, false)]
        [TestCase(10, 11, false)]
        [TestCase(10, 9, false)]
        [TestCase(13, 13, true)]
        [TestCase(100, 205, false)]
        public async Task ShouldTrigger_NoLastAFRun(int afterExposures, double exposuresToAdd, bool shouldTrigger) {
            var afTrigger = new AutofocusAfterExposures(profileServiceMock.Object, imagehistory, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object);
            afTrigger.AfterExposures = afterExposures;

            afTrigger.SequenceBlockInitialize();


            for (int i = 0; i < exposuresToAdd; i++) {
                var id = imagehistory.GetNextImageId();
                imagehistory.Add(id, "LIGHT");
                imagehistory.PopulateStatistics(i, ImageStatistics.Create(new ImageProperties(2, 2, 16, false, 100, 200), new ushort[] { 300, 400, 600, 800 }));
            }

            var itemMock = new Mock<IExposureItem>();
            itemMock.SetupGet(x => x.ImageType).Returns("LIGHT");
            var should = afTrigger.ShouldTrigger(null, itemMock.Object);

            should.Should().Be(shouldTrigger);
        }

        [Test]
        [TestCase(1, 1, false)]
        [TestCase(10, 10, false)]
        [TestCase(10, 20, false)]
        [TestCase(10, 30, false)]
        [TestCase(10, 5, false)]
        [TestCase(10, 15, false)]
        [TestCase(10, 11, false)]
        [TestCase(10, 9, false)]
        [TestCase(13, 13, false)]
        [TestCase(100, 205, false)]
        public async Task NextItemNoLightExposure_ShouldNotTrigger_NoLastAFRun(int afterExposures, double exposuresToAdd, bool shouldTrigger) {
            var afTrigger = new AutofocusAfterExposures(profileServiceMock.Object, imagehistory, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object);
            afTrigger.AfterExposures = afterExposures;

            afTrigger.SequenceBlockInitialize();


            for (int i = 0; i < exposuresToAdd; i++) {
                var id = imagehistory.GetNextImageId();
                imagehistory.Add(id, "LIGHT");
                imagehistory.PopulateStatistics(i, ImageStatistics.Create(new ImageProperties(2, 2, 16, false, 100, 200), new ushort[] { 300, 400, 600, 800 }));
            }

            var itemMock = new Mock<IExposureItem>();
            itemMock.SetupGet(x => x.ImageType).Returns("DARK");
            var should = afTrigger.ShouldTrigger(null, itemMock.Object);

            should.Should().Be(shouldTrigger);
        }

        [Test]
        public async Task Execute_Successfully_WithAllParametersPassedCorrectly() {
            var report = new AutoFocusReport();

            var filter = new FilterInfo() { Position = 0 };
            filterWheelMediatorMock.Setup(x => x.GetInfo()).Returns(new FilterWheelInfo() { SelectedFilter = filter });

            var sut = new AutofocusAfterExposures(profileServiceMock.Object, imagehistory, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object);

            await sut.Execute(default, default, default);

            // Todo proper assertion
            // historyMock.Verify(h => h.AppendAutoFocusPoint(It.Is<AutoFocusReport>(r => r == report)), Times.Once);
        }

        [Test]
        public void ToString_FilledProperly() {
            var sut = new AutofocusAfterExposures(profileServiceMock.Object, imagehistory, cameraMediatorMock.Object, filterWheelMediatorMock.Object, focuserMediatorMock.Object, autoFocusVMFactoryMock.Object);
            var tostring = sut.ToString();
            tostring.Should().Be("Trigger: AutofocusAfterExposures, AfterExposures: 5");
        }
    }
}
