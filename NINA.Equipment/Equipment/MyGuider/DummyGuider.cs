#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Interfaces;
using NINA.Core.Locale;
using NINA.Equipment.Interfaces;
using NINA.Core.Model;
using NINA.Astrometry;
using System.Collections.Generic;
using NINA.Equipment.Equipment.MyGuider.PHD2;

#pragma warning disable 1998

namespace NINA.Equipment.Equipment.MyGuider {

    public class DummyGuider : BaseINPC, IGuider {
        private IProfileService profileService;

        public DummyGuider(IProfileService profileService) {
            this.profileService = profileService;
        }

        public string Name => Loc.Instance["LblNoGuider"];

        public string Id => "No_Guider";

        private bool _connected;

        public event EventHandler<IGuideStep> GuideEvent { add { } remove { } }

        public bool Connected {
            get => _connected;
            set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        public double PixelScale { get; set; }
        public string State => string.Empty;

        public bool HasSetupDialog => false;

        public string Category => "Guiders";

        public string Description => "Dummy Guider";

        public string DriverInfo => "Dummy Guider";

        public string DriverVersion => "1.0";

        public async Task<bool> Connect(CancellationToken token) {
            profileService.ActiveProfile.GuiderSettings.GuiderName = Id;

            Connected = false;

            return Connected;
        }

        public async Task<bool> AutoSelectGuideStar() {
            return true;
        }

        public void Disconnect() {
            Connected = false;
        }

        public async Task<bool> Pause(bool pause, CancellationToken ct) {
            return true;
        }

        public async Task<bool> StartGuiding(bool forceCalibration, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            return true;
        }

        public async Task<bool> StopGuiding(CancellationToken ct) {
            return true;
        }

        public async Task<bool> Dither(IProgress<ApplicationStatus> progress, CancellationToken ct) {
            return true;
        }

        public bool CanClearCalibration => true;

        public bool CanSetShiftRate => false;
        public bool ShiftEnabled => false;
        public bool CanGetLockPosition => false;

        public SiderealShiftTrackingRate ShiftRate => SiderealShiftTrackingRate.Disabled;

        public async Task<bool> ClearCalibration(CancellationToken ct) {
            return true;
        }

        public void SetupDialog() {
        }

        public Task<bool> SetShiftRate(SiderealShiftTrackingRate shiftTrackingRate, CancellationToken ct) {
            return Task.FromResult(false);
        }

        public Task<bool> StopShifting(CancellationToken ct) {
            return Task.FromResult(true);
        }

        public IList<string> SupportedActions => new List<string>();

        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }

        public string SendCommandString(string command, bool raw) {
            throw new NotImplementedException();
        }

        public bool SendCommandBool(string command, bool raw) {
            throw new NotImplementedException();
        }

        public void SendCommandBlind(string command, bool raw) {
            throw new NotImplementedException();
        }

        public Task<LockPosition> GetLockPosition() {
            throw new NotImplementedException();
        }

    }
}