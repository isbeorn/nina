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