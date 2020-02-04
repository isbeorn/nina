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