using ASCOM.DeviceInterface;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFocuser {
    class RelativeAscomFocuser : IFocuserV3Ex {
        private int position;
        private readonly IFocuserV3 focuser;
        public RelativeAscomFocuser(IFocuserV3 relativeFocuser) {
            if (relativeFocuser.Absolute) {
                throw new InvalidOperationException($"Focuser {relativeFocuser.Name} is an absolute focuser");
            }
            this.focuser = relativeFocuser;
            this.position = 5000;
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

        public int Position {
            get {
                return this.position;
            }
        }

        public double StepSize => focuser.StepSize;

        public bool TempComp { get => focuser.TempComp && focuser.TempCompAvailable; set => focuser.TempComp = value; }

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

        public async Task MoveAsync(int pos, CancellationToken ct) {
            if (Connected) {
                bool reEnableTempComp = TempComp;
                if (reEnableTempComp) {
                    TempComp = false;
                }

                var relativeOffsetRemaining = pos - this.Position;
                while (relativeOffsetRemaining != 0) {
                    var moveAmount = Math.Min(MaxStep, Math.Abs(relativeOffsetRemaining));
                    if (relativeOffsetRemaining < 0) {
                        moveAmount *= -1;
                    }
                    focuser.Move(moveAmount);
                    while (IsMoving) {
                        await Utility.Utility.Wait(TimeSpan.FromSeconds(1), ct);
                    }
                    relativeOffsetRemaining -= moveAmount;
                    this.position += moveAmount;
                }

                if (reEnableTempComp) {
                    TempComp = true;
                }
            }
        }
    }
}
