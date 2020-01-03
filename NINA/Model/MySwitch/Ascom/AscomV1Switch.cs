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