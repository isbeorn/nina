using NINA.Utility;
using NINA.Utility.Profile;
using NINA.Utility.WindowService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            WindowService.ShowDialog(this, Locale.Loc.Instance["LblRotationRequired"], System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.ToolWindow);

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