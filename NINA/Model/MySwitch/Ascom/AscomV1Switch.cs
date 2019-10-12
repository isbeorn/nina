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