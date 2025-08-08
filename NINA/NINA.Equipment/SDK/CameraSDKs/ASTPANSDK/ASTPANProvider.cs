using NINA.Core.Interfaces;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Equipment.SDK.CameraSDKs.ASTPANSDK {
    [Export(typeof(IEquipmentProvider))]
    public class ASTPANProvider : IEquipmentProvider<ICamera> {
        public string Name => "ASTPAN";
        public string ContentId => this.GetType().FullName;
        private IProfileService profileService;
        private IASTPANPInvokeProxy astpanPInvoke;
        private IExposureDataFactory exposureDataFactory;

        [ExcludeFromCodeCoverage]
        [ImportingConstructor]
        public ASTPANProvider(IProfileService profileService, IExposureDataFactory exposureDataFactory) : this(profileService, exposureDataFactory, new ASTPANPInvokeProxy()) {
        }

        public ASTPANProvider(IProfileService profileService, IExposureDataFactory exposureDataFactory, IASTPANPInvokeProxy sVBonyPInvoke) {
            this.profileService = profileService;
            this.exposureDataFactory = exposureDataFactory;
            this.astpanPInvoke = sVBonyPInvoke;
        }

        public IList<ICamera> GetEquipment() {
            Logger.Debug("Getting ASTPAN Cameras");
            var devices = new List<ICamera>();
            astpanPInvoke.ASTPANGetNumOfCameras(out var cameras);
            if (cameras > 0) {
                for (var i = 0; i < cameras; i++) {
                    astpanPInvoke.ASTPANGetCameraInfo(out var info, i);


                    devices.Add(new GenericCamera(info.CameraID.ToString(), info.Name, "AstpanCam", "1.0", false, new ASTPANSDK((int)info.CameraID), profileService, exposureDataFactory));
                }
            }
            return devices;
        }
    }
}
