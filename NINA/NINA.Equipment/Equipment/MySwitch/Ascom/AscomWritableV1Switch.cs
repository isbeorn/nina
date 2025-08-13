#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

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
using ASCOM.Common.DeviceInterfaces;
using ASCOM.Com.DriverAccess;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;

namespace NINA.Equipment.Equipment.MySwitch.Ascom {

    internal class AscomWritableV1Switch : AscomV1Switch, IWritableSwitch {

        public AscomWritableV1Switch(ISwitchV2 s, short id) : base(s, id) {
            Minimum = 0;
            Maximum = 1;
            StepSize = 1;
            this.TargetValue = this.Value;
        }

        public void SetValue() {
            Logger.Trace($"Try setting value {TargetValue} for switch id {Id}");
            var val = TargetValue == 1 ? true : false;
            ascomSwitchHub.SetSwitch(Id, val);
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