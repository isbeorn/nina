using NINA.EquipmentChooser;
using NINA.Model;
using NINA.Model.MyFilterWheel;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel {
    class FilterWheelVM: DockableVM {
        public FilterWheelVM() :base() {
            Title = "Filter Wheel";
            ContentId = nameof(FilterWheelVM);
            CanClose = false;
            ChooseFWCommand = new RelayCommand(ChooseFW);
            DisconnectCommand = new RelayCommand(DisconnectFW);
        }

        private IFilterWheel _fW;
        public IFilterWheel FW {
            get {
                return _fW;
            }
            set {
                _fW = value;
                RaisePropertyChanged();
                Mediator.Instance.Notify(MediatorMessages.FilterWheelChanged, _fW);
            }
        }

        private void ChooseFW(object obj) {
            FW = (IFilterWheel)FilterWheelChooserVM.SelectedDevice;//(Model.MyFilterWheel.IFilterWheel)EquipmentChooserVM.Show(EquipmentChooserVM.EquipmentType.FilterWheel);
            if (FW?.Connect() == true) {
            
                Settings.FilterWheelId = FW.Id;                
            } else {
                FW = null;
            }
        }

        private void DisconnectFW(object obj) {
            System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show("Disconnect Filter Wheel?", "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Question, System.Windows.MessageBoxResult.Cancel);
            if (result == System.Windows.MessageBoxResult.OK) {
                FW.Disconnect();
                FW = null;
                RaisePropertyChanged(nameof(FW));
            }
        }

        private ICommand _chooseFWCommand;
        public ICommand ChooseFWCommand {
            get {
                return _chooseFWCommand;
            }
            set {
                _chooseFWCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _disconnectCommand;
        public ICommand DisconnectCommand {
            get {
                return _disconnectCommand;
            }
            set {
                _disconnectCommand = value;
                RaisePropertyChanged();
            }
        }

        private FilterWheelChooserVM _filterWheelChooserVM;
        public FilterWheelChooserVM FilterWheelChooserVM {
            get {
                if (_filterWheelChooserVM == null) {
                    _filterWheelChooserVM = new FilterWheelChooserVM();
                }
                return _filterWheelChooserVM;
            }
            set {
                _filterWheelChooserVM = value;
            }
        }
    }

    class FilterWheelChooserVM : EquipmentChooserVM {
        public override void GetEquipment() {
            var ascomDevices = new ASCOM.Utilities.Profile();

            foreach (ASCOM.Utilities.KeyValuePair device in ascomDevices.RegisteredDevices("FilterWheel")) {

                try {
                    AscomFilterWheel cam = new AscomFilterWheel(device.Key, device.Value);
                    Devices.Add(cam);
                } catch (Exception) {
                    //only add filter wheels which are supported. e.g. x86 drivers will not work in x64
                }
            }

            if (Devices.Count > 0) {
                var selected = (from device in Devices where device.Id == Settings.FilterWheelId select device).First();
                SelectedDevice = selected;
            }
        }
    }



}
