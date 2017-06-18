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
    class FilterWheelVM: BaseVM {
        public FilterWheelVM() :base() {
            Name = "Filter Wheel";            
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
            FW = (Model.MyFilterWheel.IFilterWheel)EquipmentChooserVM.Show(EquipmentChooserVM.EquipmentType.FilterWheel);
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
    }

    
}
