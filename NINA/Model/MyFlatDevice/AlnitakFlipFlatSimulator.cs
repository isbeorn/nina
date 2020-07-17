#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile;
using NINA.Utility;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFlatDevice {

    internal class AlnitakFlipFlatSimulator : BaseINPC, IFlatDevice {
        private readonly IProfileService _profileService;

        public AlnitakFlipFlatSimulator(IProfileService profileService) {
            _profileService = profileService;
            CoverState = CoverState.NeitherOpenNorClosed;
        }

        public bool HasSetupDialog => false;

        public string Id => "flip_flat_simulator";
        public string Name => "Flip-Flat Simulator";
        public string Category => "Alnitak Astrosystems";

        public bool Connected { get; private set; }

        public string Description => $"{Name} on port {PortName}. Firmware version: 200";
        public string DriverInfo => "Simulates an Alnitak FlipFlat.";
        public string DriverVersion => "1.0";

        public Task<bool> Connect(CancellationToken token) {
            Connected = true;
            RaiseAllPropertiesChanged();
            return Task.Run(() => Connected, token);
        }

        public void Disconnect() {
            Connected = false;
        }

        public void SetupDialog() {
        }

        private CoverState _coverState;

        public CoverState CoverState {
            get => _coverState;
            private set {
                _coverState = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(LocalizedCoverState));
            }
        }

        public string LocalizedCoverState => Locale.Loc.Instance[$"LblFlatDevice{_coverState}"];

        public int MaxBrightness => 255;
        public int MinBrightness => 0;

        public async Task<bool> Open(CancellationToken ct, int delay = 300) {
            if (!Connected) await Task.Run(() => false, ct);
            return await Task.Run(() => {
                _lightOn = false;
                CoverState = CoverState.Open;
                return true;
            }, ct);
        }

        public async Task<bool> Close(CancellationToken ct, int delay = 300) {
            if (!Connected) await Task.Run(() => false, ct);
            return await Task.Run(() => {
                CoverState = CoverState.Closed;
                return true;
            }, ct);
        }

        private bool _lightOn;

        public bool LightOn {
            get {
                if (!Connected) {
                    return false;
                }

                return CoverState == CoverState.Closed && _lightOn;
            }
            set {
                if (!Connected) return;
                if (CoverState != CoverState.Closed) return;
                _lightOn = value;
                RaisePropertyChanged();
            }
        }

        private double _brightness;

        public double Brightness {
            get => !Connected ? 0 : _brightness;
            set {
                if (Connected) {
                    if (value < 0) {
                        value = 0;
                    }
                    if (value > 1) {
                        value = 1;
                    }
                    _brightness = value;
                }
                RaisePropertyChanged();
            }
        }

        public string PortName {
            get => "NO_PORT";
            set {
            }
        }

        public bool SupportsOpenClose => true;
    }
}