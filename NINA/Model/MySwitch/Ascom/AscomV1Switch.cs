#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using ASCOM.DriverAccess;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Model.MySwitch {

    internal class AscomV1Switch : BaseINPC, ISwitch {

        public AscomV1Switch(Switch s, short id) {
            Id = id;
            ascomSwitchHub = s;

            this.Name = ascomSwitchHub.GetSwitchName(Id);
            this.Description = string.Empty;
            this.Value = ascomSwitchHub.GetSwitch(Id) ? 1d : 0d;
        }

        public async Task<bool> Poll() {
            var success = await Task.Run(() => {
                try {
                    Logger.Trace($"Try getting values for switch id {Id}");
                    this.Value = ascomSwitchHub.GetSwitch(Id) ? 1d : 0d;
                } catch (Exception) {
                    return false;
                }
                return true;
            });
            if (success) {
                RaisePropertyChanged(nameof(Value));
            }
            return success;
        }

        protected Switch ascomSwitchHub;

        public short Id { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public double Value { get; private set; }
    }
}