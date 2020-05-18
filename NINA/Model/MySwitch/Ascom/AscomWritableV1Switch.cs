#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASCOM.DriverAccess;
using NINA.Utility;

namespace NINA.Model.MySwitch {

    internal class AscomWritableV1Switch : AscomV1Switch, IWritableSwitch {

        public AscomWritableV1Switch(Switch s, short id) : base(s, id) {
            Minimum = 0;
            Maximum = 1;
            StepSize = 1;
            this.TargetValue = this.Value;
        }

        public Task SetValue() {
            Logger.Trace($"Try setting value {TargetValue} for switch id {Id}");
            var val = TargetValue == 1 ? true : false;
            ascomSwitchHub.SetSwitch(Id, val);
            return Utility.Utility.Wait(TimeSpan.FromMilliseconds(50));
        }

        public double Maximum { get; }

        public double Minimum { get; }

        public double StepSize { get; }

        private double targetValue;

        public double TargetValue {
            get => targetValue;
            set {
                targetValue = value;
                RaisePropertyChanged();
            }
        }
    }
}
