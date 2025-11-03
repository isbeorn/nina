using Moq;
using NINA.Profile.Interfaces;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Astrometry.Interfaces;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyGuider;
using FluentAssertions;
using NINA.Sequencer.Trigger.Guider;

namespace NINA.Test.SimpleSequencer {
    [TestFixture]
    public class SimpleDSOContainerTest {
        private Mock<ISequencerFactory> factoryMock;
        private Mock<IProfileService> profileServiceMock;
        private Mock<ICameraMediator> cameraMediatorMock;
        private Mock<INighttimeCalculator> nighttimeCalculatorMock;
        private Mock<IFramingAssistantVM> framingAssistantVMMock;
        private Mock<IApplicationMediator> applicationMediatorMock;
        private Mock<IPlanetariumFactory> planetariumFactoryMock;
        private Mock<IFilterWheelMediator> filterWheelMediatorMock;
        private Mock<IImageSaveMediator> imageSaveMediatorMock;
        private Mock<IImagingMediator> imagingMediatorMock;
        private Mock<IImageHistoryVM> imageHistoryVMMock;
        private Mock<IGuiderMediator> guiderMediatorMock;
        private Mock<ISafetyMonitorMediator> safetyMonitorMediatorMock;

        [SetUp]
        public void Setup() {
            factoryMock = new Mock<ISequencerFactory>();
            profileServiceMock = new Mock<IProfileService>();
            cameraMediatorMock = new Mock<ICameraMediator>();
            nighttimeCalculatorMock = new Mock<INighttimeCalculator>();
            framingAssistantVMMock = new Mock<IFramingAssistantVM>();
            applicationMediatorMock = new Mock<IApplicationMediator>();
            planetariumFactoryMock = new Mock<IPlanetariumFactory>();
            filterWheelMediatorMock = new Mock<IFilterWheelMediator>();
            imageSaveMediatorMock = new Mock<IImageSaveMediator>();
            imagingMediatorMock = new Mock<IImagingMediator>();
            imageHistoryVMMock = new Mock<IImageHistoryVM>();
            guiderMediatorMock = new Mock<IGuiderMediator>();
            safetyMonitorMediatorMock = new Mock<ISafetyMonitorMediator>();

            profileServiceMock.Setup(m => m.ActiveProfile.AstrometrySettings.Latitude).Returns(33.005699);
            profileServiceMock.Setup(m => m.ActiveProfile.AstrometrySettings.Longitude).Returns(-117.103254);
            profileServiceMock.Setup(m => m.ActiveProfile.ImageFileSettings.FilePath).Returns("");
            profileServiceMock.Setup(m => m.ActiveProfile.SequenceSettings.EstimatedDownloadTime).Returns(TimeSpan.Zero);

            cameraMediatorMock.Setup(x => x.GetInfo()).Returns(new CameraInfo() { Connected = true });
            guiderMediatorMock.Setup(x => x.GetInfo()).Returns(new GuiderInfo() { Connected = true });

            factoryMock.Setup(x => x.GetCondition<LoopCondition>()).Returns(() => new LoopCondition());
            factoryMock.Setup(x => x.GetItem<SwitchFilter>()).Returns(() => new SwitchFilter(profileServiceMock.Object, filterWheelMediatorMock.Object));
            factoryMock.Setup(x => x.GetItem<TakeExposure>()).Returns(() => new TakeExposure(profileServiceMock.Object, cameraMediatorMock.Object, imagingMediatorMock.Object, imageSaveMediatorMock.Object, imageHistoryVMMock.Object));
            factoryMock.Setup(x => x.GetTrigger<DitherAfterExposures>()).Returns(() =>   new DitherAfterExposures(guiderMediatorMock.Object, imageHistoryVMMock.Object, profileServiceMock.Object, safetyMonitorMediatorMock.Object));
        }

        [Test]
        [TestCase(1,0,60, 1,0,30, 1,0,15, 105, Description = "One Iteration, no item completed")]
        [TestCase(10, 0, 60, 10, 0, 30, 10, 0, 15, 1050, Description = "Ten Iterations, no item completed")]
        [TestCase(10, 10, 60, 10, 0, 30, 10, 0, 15, 450, Description = "Ten Iterations, fist item completed")]
        [TestCase(10, 10, 60, 10, 5, 30, 10, 0, 15, 300, Description = "Ten Iterations, first item completed, second item halfway complete")]
        [TestCase(10, 10, 60, 10, 10, 30, 10, 10, 15, 0, Description = "Ten Iterations, all completed")]
        public void SimpleDSOContainer_StandardMode_CalculateEstimatedTime(int item1Iterations, int item1CompletedIterations, int item1ExposureTime, int item2Iterations, int item2CompletedIterations, int item2ExposureTime, int item3Iterations, int item3CompletedIterations, int item3ExposureTime, int estimatedSeconds) {

            var sut = new SimpleDSOContainer(factoryMock.Object, profileServiceMock.Object, cameraMediatorMock.Object, nighttimeCalculatorMock.Object, framingAssistantVMMock.Object, applicationMediatorMock.Object, planetariumFactoryMock.Object);

            sut.Mode = Core.Enum.SequenceMode.STANDARD;
            sut.Iterations = 1;

            var se1 = sut.AddSimpleExposure();
            var loop1 = se1.GetLoopCondition() as LoopCondition;
            var exposure1 = se1.GetTakeExposure() as TakeExposure;
            loop1.Iterations = item1Iterations;
            loop1.CompletedIterations = item1CompletedIterations;
            exposure1.ExposureTime = item1ExposureTime;

            var se2 = sut.AddSimpleExposure();
            var loop2 = se2.GetLoopCondition() as LoopCondition;
            var exposure2 = se2.GetTakeExposure() as TakeExposure;
            loop2.Iterations = item2Iterations;
            loop2.CompletedIterations = item2CompletedIterations;
            exposure2.ExposureTime = item2ExposureTime;

            var se3 = sut.AddSimpleExposure();
            var loop3 = se3.GetLoopCondition() as LoopCondition;
            var exposure3 = se3.GetTakeExposure() as TakeExposure;
            loop3.Iterations = item3Iterations;
            loop3.CompletedIterations = item3CompletedIterations;
            exposure3.ExposureTime = item3ExposureTime;


            var time = sut.CalculateEstimatedRuntime();

            time.Should().Be(TimeSpan.FromSeconds(estimatedSeconds));
        }

        [Test]
        [TestCase(1, 0, 60,false, 30,false, 15,false, 105, Description = "One Iteration, no item completed")]
        [TestCase(2, 0, 60, false, 30, false, 15, false, 210, Description = "Two Iterations, no item completed")]
        [TestCase(2, 1, 60, false, 30, false, 15, false, 105, Description = "Two Iterations, one iteration complete, no item completed")]
        [TestCase(2, 0, 60, true, 30, false, 15, false, 150, Description = "Two Iterations, first item completed")]
        [TestCase(2, 1, 60, true, 30, false, 15, false, 45, Description = "Two Iterations, one iteration complete, first item completed")]
        [TestCase(2, 1, 60, true, 30, true, 15, true, 0, Description = "Two Iterations, one iteration complete, all items completed")]
        [TestCase(2, 2, 60, true, 30, true, 15, true, 0, Description = "Two Iterations, two iterations complete, all items completed")]
        [TestCase(2, 2, 60, false, 30, false, 15, false, 0, Description = "Two Iterations, two iterations complete, no items completed")]
        [TestCase(2, 0, 60, true, 30, true, 15, true, 105, Description = "Two Iterations, no item completed")]
        public void SimpleDSOContainer_RotateMode_CalculateEstimatedTime(int iterations, int completedIterations, int item1ExposureTime, bool item1Complete, int item2ExposureTime, bool item2Complete, int item3ExposureTime, bool item3Complete, int estimatedSeconds) {
            var sequencerCondition = new LoopCondition();
            factoryMock.SetupSequence(x => x.GetCondition<LoopCondition>()).Returns(sequencerCondition);
                
            var sut = new SimpleDSOContainer(factoryMock.Object, profileServiceMock.Object, cameraMediatorMock.Object, nighttimeCalculatorMock.Object, framingAssistantVMMock.Object, applicationMediatorMock.Object, planetariumFactoryMock.Object);

            sut.Mode = Core.Enum.SequenceMode.ROTATE;            
            sut.RotateIterations = iterations;
            sequencerCondition.CompletedIterations = completedIterations;

            factoryMock.Setup(x => x.GetCondition<LoopCondition>()).Returns(() => new LoopCondition());
            var se1 = sut.AddSimpleExposure();
            var loop1 = se1.GetLoopCondition() as LoopCondition;
            var exposure1 = se1.GetTakeExposure() as TakeExposure;
            loop1.Iterations = 1;
            loop1.CompletedIterations = item1Complete ? 1 : 0;
            exposure1.ExposureTime = item1ExposureTime;

            var se2 = sut.AddSimpleExposure();
            var loop2 = se2.GetLoopCondition() as LoopCondition;
            var exposure2 = se2.GetTakeExposure() as TakeExposure;
            loop2.Iterations = 1;
            loop2.CompletedIterations = item2Complete ? 1 : 0;
            exposure2.ExposureTime = item2ExposureTime;

            var se3 = sut.AddSimpleExposure();
            var loop3 = se3.GetLoopCondition() as LoopCondition;
            var exposure3 = se3.GetTakeExposure() as TakeExposure;
            loop3.Iterations = 1;
            loop3.CompletedIterations = item3Complete ? 1 : 0;
            exposure3.ExposureTime = item3ExposureTime;


            var time = sut.CalculateEstimatedRuntime();

            time.Should().Be(TimeSpan.FromSeconds(estimatedSeconds));
        }
    }
}
