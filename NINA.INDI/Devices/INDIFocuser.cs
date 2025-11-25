#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.INDI.Enums;
using NINA.INDI.Protocol;
using NINA.INDI.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Utility;

namespace NINA.INDI.Devices {

    public class INDIFocuser : INDIDevice, IINDIFocuser {






        public override void OnTextPropertyUpdated(INDITextProperty p) {
            base.OnTextPropertyUpdated(p);
        }

        public override void OnNumberPropertyUpdated(INDINumberProperty p) {
            base.OnNumberPropertyUpdated(p);

            switch (p.Name) {
                case "ABS_FOCUS_POSITION":
                    // Absolute position
                    Absolute = true;

                    // Check if moving based on state
                    IsMoving = p.State == PropertyState.Busy;
                    break;
            }
        }

        public override void OnSwitchPropertyUpdated(INDISwitchProperty p) {
            base.OnSwitchPropertyUpdated(p);
        }

        public override void OnBlobPropertyUpdated(INDIBlobProperty p) {
            base.OnBlobPropertyUpdated(p);
        }





        public INDIFocuser(INDIDeviceInfo device) : base(device) {
        }

        /// <summary>
        /// Specify critical properties that must arrive before Connect() completes
        /// </summary>
        protected override string[] GetRequiredConnectionProperties() {
            return ["ABS_FOCUS_POSITION"];
        }

        public bool Absolute { get; private set; }
        public bool IsMoving { get; private set; }
        public int MaxIncrement => MaxStep;
        public int MaxStep {
            get => (int)GetNumberPropertyValue("FOCUS_MAX", "FOCUS_MAX_VALUE");
            set {
                if (!Connected) {
                    Logger.Warning("Cannot set MaxStep: not connected");
                    return;
                }
                SetNumberValue("FOCUS_MAX", "FOCUS_MAX_VALUE", value);
                Logger.Info($"Set focuser MaxStep to {value}");
            }
        }

        public int Position => (int)GetNumberPropertyValue("ABS_FOCUS_POSITION", "FOCUS_ABSOLUTE_POSITION");

        public double StepSize => MaxIncrement;
        public bool TempComp => false;
        public bool TempCompAvailable => false;
        public double Temperature => GetNumberPropertyValue("FOCUS_TEMPERATURE", "TEMPERATURE") ?? double.NaN;


        public bool Reverse {
            get => GetSwitchPropertyValue("FOCUS_REVERSE_MOTION", "ENABLED") ?? false;
            set {
                try {
                    SetSwitchValue("FOCUS_REVERSE_MOTION", "ENABLED", value);
                } catch (ArgumentException) {
                    throw new NotImplementedException();
                }
            }
        }

        public void SyncPosition(int position) {
            SetNumberValue("FOCUS_SYNC", "FOCUS_SYNC_VALUE", position);
        }


        public void Halt() {
            try {
                SetSwitchValue("FOCUS_ABORT_MOTION", "ABORT", true);
            } catch (ArgumentException) {
                throw new NotImplementedException();
            }
        }

        public void Move(int position) {
            if (!Connected) {
                Logger.Warning("Cannot move focuser: not connected");
                return;
            }
            IsMoving = true;
            SetNumberValue("ABS_FOCUS_POSITION", "FOCUS_ABSOLUTE_POSITION", position);
            Logger.Info($"Commanded focuser to move to position {position}");
        }

        public async Task MoveAsync(int position, CancellationToken ct = default) {
            if (!Connected) {
                throw new InvalidOperationException("Cannot move focuser: not connected");
            }

            // Initiate the move
            Move(position);

            while (IsMoving && !ct.IsCancellationRequested) {
                await Task.Delay(100, ct);
            }

            Logger.Debug($"Focuser reached position {Position}");
        }

        #region Unsupported

        public IList<string> SupportedActions { get; }

        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }

        public void CommandBlind(string command, bool raw = false) {
            throw new NotImplementedException();
        }

        public bool CommandBool(string command, bool raw = false) {
            throw new NotImplementedException();
        }

        public string CommandString(string command, bool raw = false) {
            throw new NotImplementedException();
        }

        #endregion
    }
}
