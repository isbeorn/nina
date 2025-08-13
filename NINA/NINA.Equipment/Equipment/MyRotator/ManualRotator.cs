#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.Core.Utility.WindowService;
using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Astrometry;
using NINA.Core.Locale;
using NINA.Equipment.Interfaces;
using System.Collections.Generic;

namespace NINA.Equipment.Equipment.MyRotator {

    public class ManualRotator : BaseINPC, IRotator {
        private IProfileService profileService;

        public ManualRotator(IProfileService profileService) {
            this.profileService = profileService;
            this.profileService.LocaleChanged += ProfileService_LocaleChanged;
        }

        private void ProfileService_LocaleChanged(object sender, EventArgs e) {
            RaisePropertyChanged(nameof(Name));
            RaisePropertyChanged(nameof(Description));
        }

        public string Category { get; } = "N.I.N.A.";
        public bool CanReverse => true;
        private bool reverse;

        public bool Reverse {
            get => reverse;
            set {
                reverse = value;
                RaisePropertyChanged();
            }
        }

        private bool synced;

        public bool Synced {
            get => synced;
            private set {
                synced = value;
                RaisePropertyChanged();
            }
        }

        public bool IsMoving { get; set; }

        public bool Connected { get; set; }

        public float Position { get; set; }

        public float StepSize { get; set; }

        public float TargetPosition { get; set; }

        public bool HasSetupDialog => false;

        public string Id => "Manual Rotator";

        public string Name => "Manual Rotator";

        public string DisplayName => Loc.Instance["LblManualRotator"];

        public string Description => Loc.Instance["LblManualRotatorDescription"];

        public string DriverInfo => "n.A.";

        public string DriverVersion => "1.0";

        public Task<bool> Connect(CancellationToken token) {
            Connected = true;
            return Task.FromResult(Connected);
        }

        public void Disconnect() {
            Connected = false;
        }

        public void Halt() {
        }

        private IWindowService windowService;

        public IWindowService WindowService {
            get {
                if (windowService == null) {
                    windowService = new WindowService();
                }
                return windowService;
            }
            set => windowService = value;
        }

        public float Rotation => Math.Abs(TargetPosition - Position);

        public float AbsTargetPosition {
            get {
                if (TargetPosition < 0) return TargetPosition + 360;
                return TargetPosition % 360;
            }
        }

        public string Direction {
            get {
                if ((TargetPosition - Position < 0 && !Reverse) || (TargetPosition - Position >= 0 && Reverse)) {
                    return Loc.Instance["LblCounterclockwise"];
                } else {
                    return Loc.Instance["LblClockwise"];
                }
            }
        }

        public float MechanicalPosition => Position;

        public void Sync(float skyAngle) {
            Position = skyAngle;
            Synced = true;
        }

        public async Task<bool> Move(float position, CancellationToken ct) {
            IsMoving = true;

            TargetPosition = Position + position;
            if (TargetPosition - Position > 180) {
                TargetPosition = TargetPosition - 360;
            }

            if (TargetPosition - Position < -180) {
                TargetPosition = TargetPosition + 360;
            }

            // Reference: https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/
            var window = WindowService.ShowDialog(this, Loc.Instance["LblRotationRequired"], System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow);
            var cancelTaskSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (ct.Register(() => cancelTaskSource.SetCanceled())) {
                await Task.WhenAny(cancelTaskSource.Task, window.Task);
            }

            if (ct.IsCancellationRequested) {
                _ = WindowService.Close();
                ct.ThrowIfCancellationRequested();
            }
            Position = AstroUtil.EuclidianModulus(TargetPosition, 360);

            IsMoving = false;
            return true;
        }

        public async Task<bool> MoveAbsolute(float position, CancellationToken ct) {
            return await Move(position - Position, ct);
        }

        public void SetupDialog() {
        }

        public async Task<bool> MoveAbsoluteMechanical(float position, CancellationToken ct) {
            return await MoveAbsolute(position, ct);
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
    }
}