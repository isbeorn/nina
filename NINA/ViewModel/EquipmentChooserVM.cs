using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel {
    abstract class EquipmentChooserVM : BaseVM {

        public EquipmentChooserVM() {
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


    }
}
