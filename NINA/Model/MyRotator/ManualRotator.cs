#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Utility;
using NINA.Utility.Profile;
using NINA.Utility.WindowService;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyRotator {

    internal class ManualRotator : BaseINPC, IRotator {
        private IProfileService profileService;

        public ManualRotator(IProfileService profileService) {
            this.profileService = profileService;
            this.profileService.LocaleChanged += ProfileService_LocaleChanged;
        }

        private void ProfileService_LocaleChanged(object sender, EventArgs e) {
            RaisePropertyChanged(nameof(Name));
            RaisePropertyChanged(nameof(Description));
        }

        public bool IsMoving { get; set; }

        public bool Connected { get; set; }

        public float Position { get; set; }

        public float TargetPosition { get; set; }

        public bool HasSetupDialog {
            get {
                return false;
            }
        }

        public string Id {
            get {
                return "Manual Rotator";
            }
        }

        public string Name {
            get {
                return Locale.Loc.Instance["LblManualRotator"];
            }
        }

        public string Description {
            get {
                return Locale.Loc.Instance["LblManualRotatorDescription"];
            }
        }

        public string DriverInfo {
            get {
                return "n.A.";
            }
        }

        public string DriverVersion {
            get {
                return "1.0";
            }
        }

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
            set {
                windowService = value;
            }
        }

        public float Rotation {
            get {
                return Math.Abs(TargetPosition - Position);
            }
        }

        public float AbsTargetPosition {
            get {
                if (TargetPosition < 0) return TargetPosition + 360;
                return TargetPosition % 360;
            }
        }

        public string Direction {
            get {
                if (TargetPosition - Position < 0) {
                    return Locale.Loc.Instance["LblCounterclockwise"];
                } else {
                    return Locale.Loc.Instance["LblClockwise"];
                }
            }
        }

        public void Move(float position) {
            IsMoving = true;

            TargetPosition = Position + position;
            if (TargetPosition - Position > 180) {
                TargetPosition = TargetPosition - 360;
            }

            if (TargetPosition - Position < -180) {
                TargetPosition = TargetPosition + 360;
            }

            var clockwise = TargetPosition - Position > 0;

            var task = WindowService.ShowDialog(this, Locale.Loc.Instance["LblRotationRequired"], System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow);
            task.Wait();

            Position = TargetPosition % 360;
            if (Position < 0) { Position += 360; }

            IsMoving = false;
        }

        public void MoveAbsolute(float position) {
            Move(position - Position);
        }

        public void SetupDialog() {
        }
    }
}