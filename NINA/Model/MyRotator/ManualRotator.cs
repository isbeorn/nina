using NINA.Utility;
using NINA.Utility.Profile;
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

        public void Move(float position) {
            IsMoving = true;
            MyMessageBox.MyMessageBox.Show(
                    string.Format(Locale.Loc.Instance["LblPleaseRotate"], position),
                    Locale.Loc.Instance["LblRotationRequired"],
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxResult.OK
            );
            Position = (Position + position) % 360;
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