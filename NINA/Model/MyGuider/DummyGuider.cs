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
using System;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 1998

namespace NINA.Model.MyGuider {

    public class DummyGuider : BaseINPC, IGuider {
        private IProfileService profileService;

        public DummyGuider(IProfileService profileService) {
            this.profileService = profileService;
        }

        public string Name => Locale.Loc.Instance["LblNoGuider"];

        public string Id => "No_Guider";

        private bool _connected;

        public event EventHandler<IGuideStep> GuideEvent;

        public bool Connected {
            get => _connected;
            set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        public double PixelScale { get; set; }
        public string State => string.Empty;

        public async Task<bool> Connect() {
            profileService.ActiveProfile.GuiderSettings.GuiderName = Id;

            Connected = false;

            return Connected;
        }

        public async Task<bool> AutoSelectGuideStar() {
            return true;
        }

        public bool Disconnect() {
            Connected = false;

            return Connected;
        }

        public async Task<bool> Pause(bool pause, CancellationToken ct) {
            return true;
        }

        public async Task<bool> StartGuiding(CancellationToken ct) {
            return true;
        }

        public async Task<bool> StopGuiding(CancellationToken ct) {
            return true;
        }

        public async Task<bool> Dither(CancellationToken ct) {
            return true;
        }
    }
}
