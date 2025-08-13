#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Locale;
using NINA.Equipment.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MySwitch {

    public class DummySwitch : IWritableSwitch {

        public DummySwitch(short index) {
            Id = index;
            Name = $"{Loc.Instance["LblSwitch"]} {index}";
        }

        public double Maximum => double.MaxValue;

        public double Minimum => double.MinValue;

        public double StepSize => 0.01;

        public double TargetValue { get; set; }

        public short Id { get; private set; }

        public string Name { get; private set; }

        public string Description { get; }

        public double Value { get; }

        public bool Poll() {
            return true;
        }

        public void SetValue() {
            return;
        }
    }
}