#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility;
using System;
using System.Xml.Serialization;

namespace NINA.Model.MyCamera {

    [Serializable()]
    [XmlRoot(ElementName = nameof(BinningMode))]
    public class BinningMode : BaseINPC {

        private BinningMode() {
        }

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

        public override bool Equals(object obj) {
            if (obj == null || this.GetType() != obj.GetType()) {
                return false;
            }

            var other = (BinningMode)obj;
            return _x == other._x && _y == other._y;
        }

        public override int GetHashCode() {
            //see https://en.wikipedia.org/wiki/Hash_function, used when BinningMode is used as a dictionary key for instance
            const int primeNumber = 397;
            unchecked {
                return (_x.GetHashCode() * primeNumber) ^ _y.GetHashCode();
            }
        }
    }
}
