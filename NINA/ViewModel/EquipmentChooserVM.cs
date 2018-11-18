using NINA.Utility;
using NINA.Utility.Mediator;
using NINA.Utility.Profile;
using System;
using System.Linq;
using System.Windows.Input;

namespace NINA.ViewModel {

    internal abstract class EquipmentChooserVM : BaseVM {

        public EquipmentChooserVM(Type equipmentType, IProfileService profileService) : base(profileService) {
            this.profileService = profileService;
            SetupDialogCommand = new RelayCommand(OpenSetupDialog);
            GetEquipment();
        }

        private AsyncObservableCollection<Model.IDevice> _devices;

        public AsyncObservableCollection<Model.IDevice> Devices {
            get {
                if (_devices == null) {
                    _devices = new AsyncObservableCollection<Model.IDevice>();
                }
                return _devices;
            }
            set {
                _devices = value;
            }
        }

        public abstract void GetEquipment();

        private Model.IDevice _selectedDevice;

        public Model.IDevice SelectedDevice {
            get {
                return _selectedDevice;
            }
            set {
                _selectedDevice = value;
                RaisePropertyChanged();
            }
        }

        public ICommand SetupDialogCommand { get; private set; }

        private void OpenSetupDialog(object o) {
            if (SelectedDevice?.HasSetupDialog == true) {
                SelectedDevice.SetupDialog();
            }
        }

        public void DetermineSelectedDevice(string id) {
            if (Devices.Count > 0) {
                var items = (from device in Devices where device.Id == id select device);
                if (items.Count() > 0) {
                    SelectedDevice = items.First();
                } else {
                    SelectedDevice = Devices.First();
                }
            }
        }
    }
}