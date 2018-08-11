using NINA.Model.MyCamera;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {

    internal interface ICameraConsumer {

        void UpdateCameraInfo(CameraInfo cameraInfo);
    }
}