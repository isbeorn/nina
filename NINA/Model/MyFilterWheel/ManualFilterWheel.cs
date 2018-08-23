using NINA.Utility;
using NINA.Utility.Profile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFilterWheel {

    internal class ManualFilterWheel : BaseINPC, IFilterWheel {

        public ManualFilterWheel(IProfileService profileService) {
            this.profileService = profileService;
            this.profileService.LocaleChanged += ProfileService_LocaleChanged;
        }

        private void ProfileService_LocaleChanged(object sender, EventArgs e) {
            RaisePropertyChanged(nameof(Description));
        }

        private bool connected;
        private IProfileService profileService;

        public bool Connected {
            get {
                return connected;
            }
            set {
                connected = value;
                RaisePropertyChanged();
            }
        }

        public string Description {
            get {
                return Locale.Loc.Instance["LblManualFilterWheelDescription"];
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

        public short InterfaceVersion {
            get {
                return 1;
            }
        }

        public int[] FocusOffsets {
            get {
                return this.Filters.Select((x) => x.FocusOffset).ToArray();
            }
        }

        public string[] Names {
            get {
                return this.Filters.Select((x) => x.Name).ToArray();
            }
        }

        private short position;

        public short Position {
            get {
                return position;
            }

            set {
                MyMessageBox.MyMessageBox.Show(
                    string.Format(Locale.Loc.Instance["LblPleaseChangeToFilter"], this.Filters[value].Name),
                    Locale.Loc.Instance["LblFilterChangeRequired"],
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxResult.OK);
                position = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<FilterInfo> Filters {
            get {
                return this.profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
            }
        }

        public ArrayList SupportedActions {
            get {
                return new ArrayList();
            }
        }

        public bool HasSetupDialog {
            get {
                return false;
            }
        }

        public string Id {
            get {
                return "Manual Filter Wheel";
            }
        }

        public string Name {
            get {
                return Locale.Loc.Instance["LblManualFilterWheel"];
            }
        }

        public Task<bool> Connect(CancellationToken token) {
            Connected = true;
            return Task.FromResult(true);
        }

        public void Disconnect() {
            Connected = false;
        }

        public void SetupDialog() {
        }
    }
}