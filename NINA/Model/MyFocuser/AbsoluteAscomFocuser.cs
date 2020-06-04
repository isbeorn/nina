using ASCOM.DeviceInterface;
using ASCOM.DriverAccess;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace NINA.Model.MyFocuser {
    public class AbsoluteAscomFocuser : IFocuserV3Ex {
        private readonly IFocuserV3 focuser;
        public AbsoluteAscomFocuser(IFocuserV3 focuser) {
            this.focuser = focuser;
        }

        public bool Connected { get => focuser.Connected; set => focuser.Connected = value; }

        public string Description => focuser.Description;

        public string DriverInfo => focuser.DriverInfo;

        public string DriverVersion => focuser.DriverVersion;

        public short InterfaceVersion => focuser.InterfaceVersion;

        public string Name => focuser.Name;

        public ArrayList SupportedActions => focuser.SupportedActions;

        public bool Absolute => true;

        public bool IsMoving => focuser.IsMoving;

        public bool Link { get => focuser.Link; set => focuser.Link = value; }

        public int MaxIncrement => focuser.MaxIncrement;

        public int MaxStep => focuser.MaxStep;

        public int Position => focuser.Position;

        public double StepSize => focuser.StepSize;

        public bool TempComp { get => focuser.TempComp; set => focuser.TempComp = value; }

        public bool TempCompAvailable => focuser.TempCompAvailable;

        public double Temperature => focuser.Temperature;

        public string Action(string actionName, string actionParameters) {
            return focuser.Action(actionName, actionParameters);
        }

        public void CommandBlind(string command, bool raw = false) {
            focuser.CommandBlind(command, raw);
        }

        public bool CommandBool(string command, bool raw = false) {
            return focuser.CommandBool(command, raw);
        }

        public string CommandString(string command, bool raw = false) {
            return focuser.CommandString(command, raw);
        }

        public void Dispose() {
            focuser.Dispose();
        }

        public void Halt() {
            focuser.Halt();
        }

        public void Move(int position) {
            throw new NotSupportedException("MoveAsync should be used instead of Move");
        }

        public void SetupDialog() {
            focuser.SetupDialog();
        }

        public async Task MoveAsync(int position, CancellationToken ct) {
            if (Connected && !TempComp) {
                while (position != focuser.Position) {
                    focuser.Move(position);
                    while (IsMoving) {
                        await Utility.Utility.Wait(TimeSpan.FromSeconds(1), ct);
                    }
                }
            }
        }
    }
}
