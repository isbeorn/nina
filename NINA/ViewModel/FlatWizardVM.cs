using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Profile;

namespace NINA.ViewModel {

    internal class FlatWizardVM : DockableVM, ICameraConsumer, IFilterWheelConsumer {
        private readonly ICameraMediator cameraMediator;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly IImagingMediator imagingMediator;
        private readonly IApplicationStatusMediator applicationStatusMediator;

        public FlatWizardVM(IProfileService profileService,
                            ICameraMediator cameraMediator,
                            IFilterWheelMediator filterWheelMediator,
                            IImagingMediator imagingMediator,
                            IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            Title = "LblFlatsWizard";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ContrastSVG"];

            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);

            this.filterWheelMediator = filterWheelMediator;
            this.filterWheelMediator.RegisterConsumer(this);

            this.imagingMediator = imagingMediator;
            this.applicationStatusMediator = applicationStatusMediator;
        }

        public void UpdateDeviceInfo(CameraInfo deviceInfo) {
            throw new NotImplementedException();
        }

        public void UpdateDeviceInfo(FilterWheelInfo deviceInfo) {
        }
    }
}