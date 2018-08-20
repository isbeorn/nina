using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model.MyRotator {

    internal class RotatorInfo : DeviceInfo {
        private float position;

        public float Position {
            get { return position; }
            set { position = value; RaisePropertyChanged(); }
        }

        private bool isMoving;

        public bool IsMoving {
            get { return isMoving; }
            set { isMoving = value; RaisePropertyChanged(); }
        }
    }
}