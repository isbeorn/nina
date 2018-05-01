using NINA.Model.MyCamera;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.RawConverter {
    class RawConverter {
        public static IRawConverter CreateInstance() {
            switch(Profile.ProfileManager.Instance.ActiveProfile.CameraSettings.RawConverter) {
                case Enum.RawConverterEnum.DCRAW:
                    return new DCRaw();
                case Enum.RawConverterEnum.FREEIMAGE:
                    return new FreeImageConverter();
                default:
                    return new DCRaw();
            }            
        }
    }

    interface IRawConverter {
        Task<ImageArray> ConvertToImageArray(MemoryStream s, CancellationToken token);
    }
}
