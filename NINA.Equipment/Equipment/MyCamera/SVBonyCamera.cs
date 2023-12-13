using NINA.Equipment.Interfaces;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyCamera {
    public class SVBonyCamera : GenericCamera {
        private ISVBonySDK svBonySdk;

        public SVBonyCamera(int id, string name, string driverVersion, ISVBonySDK sdk, IProfileService profileService, IExposureDataFactory exposureDataFactory) : base(id,name,category: "SVBony", driverVersion,supportBitScaling: true, sdk, profileService, exposureDataFactory) {
            this.svBonySdk = sdk;
        }

        protected override void Initialize() {
            base.Initialize();
            this.BadPixelCorrection = profileService.ActiveProfile.CameraSettings.BadPixelCorrection;
            this.BadPixelCorrectionThreshold = profileService.ActiveProfile.CameraSettings.BadPixelCorrectionThreshold;
        }

        public bool BadPixelCorrection {
            get => svBonySdk.GetBadPixelCorrection();
            set {
                if (svBonySdk.SetBadPixelCorrection(value)) {
                    profileService.ActiveProfile.CameraSettings.BadPixelCorrection = value;
                    RaisePropertyChanged();
                }
            }
        }

        public int MinBadPixelCorrectionThreshold => svBonySdk.GetMinBadPixelCorrectionThreshold();

        public int MaxBadPixelCorrectionThreshold => svBonySdk.GetMaxBadPixelCorrectionThreshold();

        public int BadPixelCorrectionThreshold {
            get => svBonySdk.GetBadPixelCorrectionThreshold();
            set {
                if (svBonySdk.SetBadPixelCorrectionThreshold(value)) {
                    profileService.ActiveProfile.CameraSettings.BadPixelCorrectionThreshold = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}
