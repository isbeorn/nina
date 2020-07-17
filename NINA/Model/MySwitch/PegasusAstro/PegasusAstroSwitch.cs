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