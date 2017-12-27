using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NINA.Model.MyCamera {
    [Serializable()]
    [XmlRoot(ElementName = nameof(BinningMode))]
    public class BinningMode : BaseINPC {
        private BinningMode() { }
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
        [XmlElement(nameof(X))]
        public short X {
            get {
                return _x;
            }

            set {
                _x = value;
                RaisePropertyChanged();
            }
        }
        [XmlElement(nameof(Y))]
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
