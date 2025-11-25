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

    public class INDIRotator : INDIDevice, IINDIRotator {

        private bool _isMoving;






        public override void OnTextPropertyUpdated(INDITextProperty p) {
            base.OnTextPropertyUpdated(p);
        }

        public override void OnNumberPropertyUpdated(INDINumberProperty p) {
            base.OnNumberPropertyUpdated(p);

            switch (p.Name) {
                case "ABS_ROTATOR_ANGLE":
                    // Check if moving based on state
                    _isMoving = p.State == PropertyState.Busy;
                    break;
            }
        }

        public override void OnSwitchPropertyUpdated(INDISwitchProperty p) {
            base.OnSwitchPropertyUpdated(p);
        }

        public override void OnBlobPropertyUpdated(INDIBlobProperty p) {
            base.OnBlobPropertyUpdated(p);
        }





        public INDIRotator(INDIDeviceInfo device) : base(device) {
        }

        /// <summary>
        /// Specify critical properties that must arrive before Connect() completes
        /// </summary>
        protected override string[] GetRequiredConnectionProperties() {
            return ["ABS_ROTATOR_ANGLE"];
        }

        public bool CanReverse {
            get {
                var prop = GetSwitchProperty("ROTATOR_REVERSE");
                return prop != null;
            }
        }

        public bool IsMoving => _isMoving;

        public float MechanicalPosition => Position; // INDI doesn't distinguish mechanical vs sky position

        public float Position => (float)(GetNumberPropertyValue("ABS_ROTATOR_ANGLE", "ANGLE") ?? 0.0);

        public bool Reverse {
            get => GetSwitchPropertyValue("ROTATOR_REVERSE", "ENABLED") ?? false;
            set {
                if (!Connected) {
                    Logger.Warning("Cannot set rotator reverse: not connected");
                    return;
                }
                try {
                    SetSwitchValue("ROTATOR_REVERSE", "ENABLED", value);
                    Logger.Info($"Set rotator reverse to {value}");
                } catch (ArgumentException) {
                    throw new NotImplementedException("Rotator does not support reverse");
                }
            }
        }

        public float StepSize => 1.0f; // INDI rotators typically work in degrees

        public float TargetPosition => (float)(GetNumberPropertyValue("ABS_ROTATOR_ANGLE", "ANGLE") ?? 0.0);

        public void Halt() {
            if (!Connected) {
                Logger.Warning("Cannot halt rotator: not connected");
                return;
            }
            try {
                SetSwitchValue("ROTATOR_ABORT_MOTION", "ABORT", true);
                _isMoving = false;
                Logger.Info("Halted rotator");
            } catch (ArgumentException) {
                Logger.Warning("Rotator does not support abort");
            }
        }

        public async Task MoveAsync(float position, CancellationToken ct = default) {
            if (!Connected) {
                throw new InvalidOperationException("Cannot move rotator: not connected");
            }

            // Initiate the move
            MoveAbsolute(position);

            // Wait for move to complete
            while (_isMoving && !ct.IsCancellationRequested) {
                await Task.Delay(100, ct);
            }

            Logger.Debug($"Rotator reached position {Position}");
        }

        public void MoveAbsolute(float position) {
            if (!Connected) {
                Logger.Warning("Cannot move rotator: not connected");
                return;
            }
            _isMoving = true;
            SetNumberValue("ABS_ROTATOR_ANGLE", "ANGLE", position);
            Logger.Info($"Commanded rotator to move to position {position}°");
        }

        public bool MoveMechanical(float position) {
            // INDI doesn't distinguish mechanical vs sky position
            MoveAbsolute(position);
            return true;
        }

        public void Sync(float position) {
            if (!Connected) {
                Logger.Warning("Cannot sync rotator: not connected");
                return;
            }
            try {
                SetNumberValue("ROTATOR_SYNC", "SYNC", position);
                Logger.Info($"Synced rotator to position {position}°");
            } catch (ArgumentException) {
                Logger.Warning("Rotator does not support sync");
            }
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
