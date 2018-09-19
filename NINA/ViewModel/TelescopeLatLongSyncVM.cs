using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel {

    internal class TelescopeLatLongSyncVM {

        public TelescopeLatLongSyncVM(
                bool canTelescopeSync,
                double nINALatitude,
                double nINALongitude,
                double telescopeLatitude,
                double telescopeLongitude) {
            SyncTelescopeCommand = new RelayCommand(SyncTelescope, (object o) => canTelescopeSync);
            SyncNINACommand = new RelayCommand(SyncNINA);
            SyncNoneCommand = new RelayCommand(SyncNone);
            this.NINALatitude = nINALatitude;
            this.NINALongitude = nINALongitude;
            this.TelescopeLatitude = telescopeLatitude;
            this.TelescopeLongitude = telescopeLongitude;
        }

        public double NINALatitude { get; private set; }
        public double NINALongitude { get; private set; }
        public double TelescopeLatitude { get; private set; }
        public double TelescopeLongitude { get; private set; }

        public enum LatLongSyncMode {
            NONE,
            TELESCOPE,
            NINA
        }

        public LatLongSyncMode Mode { get; set; }

        private void SyncNone(object obj) {
            Mode = LatLongSyncMode.NONE;
        }

        private void SyncNINA(object obj) {
            Mode = LatLongSyncMode.NINA;
        }

        private void SyncTelescope(object obj) {
            Mode = LatLongSyncMode.TELESCOPE;
        }

        public ICommand SyncTelescopeCommand { get; set; }
        public ICommand SyncNINACommand { get; set; }
        public ICommand SyncNoneCommand { get; set; }
    }
}