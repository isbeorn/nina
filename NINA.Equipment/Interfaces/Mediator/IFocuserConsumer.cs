#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MyFocuser;
using System;

namespace NINA.Equipment.Interfaces.Mediator {

    public class AutoFocusInfo {

        public AutoFocusInfo(double temperature, double position, string filter, DateTime timestamp) {
            Temperature = temperature;
            Position = position;
            Filter = filter;
            Timestamp = timestamp;
        }

        public string Filter { get; set; }

        public DateTime Timestamp { get; set; }

        public double Temperature { get; set; }

        public double Position { get; set; }
    }

    public interface IFocuserConsumer : IDeviceConsumer<FocuserInfo> {

        void UpdateEndAutoFocusRun(AutoFocusInfo info);

        void UpdateUserFocused(FocuserInfo info);
    }
}