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
using System.Threading.Tasks;

namespace NINA.Model.MySwitch {

    internal class AscomSwitch : BaseINPC, ISwitch {

        public AscomSwitch(Switch s, short id) {
            Id = id;
            ascomSwitchHub = s;

            this.Name = ascomSwitchHub.GetSwitchName(Id);
            this.Description = ascomSwitchHub.GetSwitchDescription(Id);
            this.Value = ascomSwitchHub.GetSwitchValue(Id);
        }

        protected Switch ascomSwitchHub;

        public short Id { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public double Value { get; private set; }

        public async Task<bool> Poll() {
            var success = await Task.Run(() => {
                try {
                    Logger.Trace($"Try getting values for switch id {Id}");
                    this.Name = ascomSwitchHub.GetSwitchName(Id);
                    this.Value = ascomSwitchHub.GetSwitchValue(Id);
                } catch (Exception) {
                    return false;
                }
                return true;
            });
            if (success) {
                RaisePropertyChanged(nameof(Name));
                RaisePropertyChanged(nameof(Value));
            }
            return success;
        }
    }
}