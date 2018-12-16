#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
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
    }
}