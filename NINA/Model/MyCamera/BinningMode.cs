using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model.MyCamera {
    public class BinningMode : BaseINPC {
        public BinningMode(short x, short y) {
            X = x;
            Y = y;
        }
        private short _x;
        private short _y;
        public string Name {
            get {
                return string.Join("x", X, Y);
            }
        }
        public short X {
            get {
                return _x;
            }

            set {
                _x = value;
                RaisePropertyChanged();
            }
        }

        public short Y {
            get {
                return _y;
            }

            set {
                _y = value;
                RaisePropertyChanged();
            }
        }

        public override string ToString() {
            return Name;
        }
    }
}
