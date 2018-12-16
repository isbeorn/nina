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
using System.Collections;
using System.Xml.Serialization;

namespace NINA.Model.MyFilterWheel {

    internal interface IFilterWheel : IDevice {
        short InterfaceVersion { get; }
        int[] FocusOffsets { get; }
        string[] Names { get; }
        short Position { get; set; }
        ArrayList SupportedActions { get; }
        AsyncObservableCollection<FilterInfo> Filters { get; }
    }

    [Serializable()]
    [XmlRoot(ElementName = nameof(FilterInfo))]
    public class FilterInfo : BaseINPC {

        private FilterInfo() {
        }

        private string _name;
        private int _focusOffset;
        private short _position;
        private double _autoFocusExposureTime;

        [XmlElement(nameof(Name))]
        public string Name {
            get {
                return _name;
            }

            set {
                _name = value;
                RaisePropertyChanged();
            }
        }

        [XmlElement(nameof(FocusOffset))]
        public int FocusOffset {
            get {
                return _focusOffset;
            }

            set {
                _focusOffset = value;
                RaisePropertyChanged();
            }
        }

        [XmlElement(nameof(Position))]
        public short Position {
            get {
                return _position;
            }

            set {
                _position = value;
                RaisePropertyChanged();
            }
        }

        [XmlElement(nameof(AutoFocusExposureTime))]
        public double AutoFocusExposureTime {
            get {
                return _autoFocusExposureTime;
            }

            set {
                _autoFocusExposureTime = value;
                RaisePropertyChanged();
            }
        }

        public FilterInfo(string n, int offset, short position) {
            Name = n;
            FocusOffset = offset;
            Position = position;
        }

        public FilterInfo(string n, int offset, short position, double autoFocusExposureTime) : this(n, offset, position) {
            AutoFocusExposureTime = autoFocusExposureTime;
        }

        public override string ToString() {
            return Name;
        }
    }
}