#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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
using NINA.Utility.SwitchSDKs.PegasusAstro;
using System;
using System.Threading.Tasks;

namespace NINA.Model.MySwitch.PegasusAstro {

    public abstract class PegasusAstroSwitch : BaseINPC, IWritableSwitch {
        protected const double Tolerance = 0.00001;
        public short Id { get; set; }
        private IPegasusDevice _sdk;

        public IPegasusDevice Sdk {
            get => _sdk ?? (_sdk = PegasusDevice.Instance);
            set => _sdk = value;
        }

        private string _name;

        public string Name {
            get => _name;
            set {
                if (_name == value) return;
                _name = value;
                RaisePropertyChanged();
            }
        }

        private string _description;

        public string Description {
            get => _description;
            set {
                if (_description == value) return;
                _description = value;
                RaisePropertyChanged();
            }
        }

        private double _value;

        public double Value {
            get => _value;
            protected set {
                if (Math.Abs(_value - value) < Tolerance) return;
                _value = value;
                RaisePropertyChanged();
            }
        }

        public abstract Task<bool> Poll();

        public abstract double Maximum { get; protected set; }
        public abstract double Minimum { get; protected set; }
        public abstract double StepSize { get; protected set; }
        public abstract double TargetValue { get; set; }

        public abstract Task SetValue();
    }
}