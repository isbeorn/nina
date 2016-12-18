using NINA.Model;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel {
    class FilterWheelVM: BaseVM {
        public FilterWheelVM() {
            Name = "Filter Wheel";
            FW = new FilterWheelModel();
            ChooseFWCommand = new RelayCommand(chooseFW);
            DisconnectCommand = new RelayCommand(disconnectFW);
        }

        private FilterWheelModel _fW;
        public FilterWheelModel FW {
            get {
                return _fW;
            }
            set {
                _fW = value;
                RaisePropertyChanged();
            }
        }

        private void chooseFW(object obj) {            
            if (FW.connect()) {
                
            }
        }

        private void disconnectFW(object obj) {
            System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show("Disconnect Filter Wheel?", "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Question, System.Windows.MessageBoxResult.Cancel);
            if (result == System.Windows.MessageBoxResult.OK) {
                FW.disconnect();
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
