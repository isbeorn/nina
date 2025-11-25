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

    public class INDIFilterWheel : INDIDevice, IINDIFilterWheel {

        private bool _isMoving;






        public override void OnTextPropertyUpdated(INDITextProperty p) {
            base.OnTextPropertyUpdated(p);

            switch (p.Name) {
                case "FILTER_NAME":
                    // Filter names updated
                    Logger.Debug($"Filter names updated: {p.Texts.Count} filters");
                    break;
            }
        }

        public override void OnNumberPropertyUpdated(INDINumberProperty p) {
            base.OnNumberPropertyUpdated(p);

            switch (p.Name) {
                case "FILTER_SLOT":
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





        public INDIFilterWheel(INDIDeviceInfo device) : base(device) {
        }

        /// <summary>
        /// Specify critical properties that must arrive before Connect() completes
        /// </summary>
        protected override string[] GetRequiredConnectionProperties() {
            return ["FILTER_SLOT"];
        }

        public int[] FocusOffsets {
            get {
                var prop = GetNumberProperty("FILTER_FOCUS_OFFSET");
                if (prop == null) return Array.Empty<int>();
                
                var offsets = new int[prop.Numbers.Count];
                for (int i = 0; i < prop.Numbers.Count; i++) {
                    offsets[i] = (int)prop.Numbers[i].Value;
                }
                return offsets;
            }
        }

        public string[] Names {
            get {
                var prop = GetTextProperty("FILTER_NAME");
                if (prop == null) return Array.Empty<string>();
                
                var names = new string[prop.Texts.Count];
                for (int i = 0; i < prop.Texts.Count; i++) {
                    names[i] = prop.Texts[i].Value ?? $"Filter {i + 1}";
                }
                return names;
            }
        }

        public int Position {
            get => (int)GetNumberPropertyValue("FILTER_SLOT", "FILTER_SLOT_VALUE").GetValueOrDefault(1);
            set {
                if (!Connected) {
                    Logger.Warning("Cannot set filter position: not connected");
                    return;
                }
                _isMoving = true;
                SetNumberValue("FILTER_SLOT", "FILTER_SLOT_VALUE", value);
                Logger.Info($"Commanded filter wheel to position {value}");
            }
        }

        public async Task MoveToPositionAsync(int position, CancellationToken ct = default) {
            if (!Connected) {
                throw new InvalidOperationException("Cannot move filter wheel: not connected");
            }

            // Initiate the move
            Position = position;

            // Wait for move to complete
            while (_isMoving && !ct.IsCancellationRequested) {
                await Task.Delay(100, ct);
            }

            Logger.Debug($"Filter wheel reached position {Position}");
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
