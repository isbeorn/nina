using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model.MyFocuser {

    internal class FocuserInfo : DeviceInfo {
        private int position;

        public int Position {
            get { return position; }
            set { position = value; RaisePropertyChanged(); }
        }

        private double temperature;

        public double Temperature {
            get { return temperature; }
            set { temperature = value; RaisePropertyChanged(); }
        }

        private bool isMoving;

        public bool IsMoving {
            get { return isMoving; }
            set { isMoving = value; RaisePropertyChanged(); }
        }

        private bool tempComp;

        public bool TempComp {
            get { return tempComp; }
            set { tempComp = value; RaisePropertyChanged(); }
        }
    }
}