using NINA.Model.MyCamera;
using NINA.Profile;
using System;

namespace ZWOptical.ASISDK {

    public static class ASICameras {

        public static int Count {
            get { return ASICameraDll.GetNumOfConnectedCameras(); }
        }

        public static ASICamera GetCamera(int cameraId, IProfileService profileService) {
            if (cameraId >= Count || cameraId < 0)
                throw new IndexOutOfRangeException();

            return new ASICamera(cameraId, profileService);
        }
    }
}