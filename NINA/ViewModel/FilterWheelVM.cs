using NINA.EquipmentChooser;
using NINA.Model;
using NINA.Model.MyFilterWheel;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel {
    class FilterWheelVM: DockableVM {
        public FilterWheelVM() :base() {
            Title = "LblFilterWheel";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["FWSVG"];

            ContentId = nameof(FilterWheelVM);
            ChooseFWCommand = new RelayCommand(ChooseFW);
            DisconnectCommand = new RelayCommand(DisconnectFW);
            RefreshFWListCommand = new RelayCommand(RefreshFWList);

            RegisterMediatorMessages();
        }        

        private void RegisterMediatorMessages() {
            Mediator.Instance.RegisterAsync(async (object o) => {
                var args = (object[])o;                
                if(args[0] != null) {
                    CancellationTokenSource token = null;
                    if (args.Length > 1) { token = (CancellationTokenSource)args[1]; }
                    FilterInfo filter = (FilterInfo)args[0];
                    if (SelectedFilter != filter) { _selectedFilter = filter; RaisePropertyChanged(nameof(SelectedFilter)); }

                    await ChangeFilter(filter, token);                    
                }                              
            }, AsyncMediatorMessages.ChangeFilterWheelPosition);
        }

        private CancellationTokenSource _changeFilterCancellationSource;
        private Task _changeFilterTask;
        private bool ChangeFilterHelper() {
            _changeFilterCancellationSource?.Cancel();
            try {
                if(_changeFilterCancellationSource != null) {
                    _changeFilterTask?.Wait(_changeFilterCancellationSource.Token);
                }                
            } catch (OperationCanceledException) {

            }
            _changeFilterCancellationSource = new CancellationTokenSource();
            _changeFilterTask = ChangeFilter(SelectedFilter, _changeFilterCancellationSource);
            
            return true;
        }

        private async Task<bool> ChangeFilter(FilterInfo filter, CancellationTokenSource token = null) {
            if (FW?.Connected == true && FW?.Position != filter.Position) {
                Task changeFocus = null;
                if (Settings.FocuserUseFilterWheelOffsets) {
                    if (this._prevFilter != null) {
                        int offset = this.SelectedFilter.FocusOffset - this._prevFilter.FocusOffset;
                        changeFocus =  Mediator.Instance.NotifyAsync(AsyncMediatorMessages.MoveFocuserRelative,offset);
                    }
                }

                FW.Position = filter.Position;
                var changeFilter = Task.Run(async () => {
                    while (FW.Position == -1) {
                        await Task.Delay(1000);
                        token?.Token.ThrowIfCancellationRequested();
                    }
                });

                if(changeFocus != null) {
                    await changeFocus;
                }                
                await changeFilter;
            }
            return true;
        }

        private void RefreshFWList(object obj) {
            FilterWheelChooserVM.GetEquipment();
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

        private FilterInfo _prevFilter;
        private FilterInfo _selectedFilter;
        public FilterInfo SelectedFilter {
            get {
                return _selectedFilter;
            }
            set {
                _prevFilter = _selectedFilter;
                _selectedFilter = value;
                ChangeFilterHelper();
                RaisePropertyChanged();
            }
        }

        private void ChooseFW(object obj) {
            FW = (IFilterWheel)FilterWheelChooserVM.SelectedDevice;
            if (FW?.Connect() == true) {
            
                Settings.FilterWheelId = FW.Id;
                if(FW.Position > -1) {
                    SelectedFilter = FW.Filters[FW.Position];
                }                
            } else {
                FW = null;
            }
        }

        private void DisconnectFW(object obj) {
            var diag = MyMessageBox.MyMessageBox.Show("Disconnect Filter Wheel?", "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);            
            if (diag == System.Windows.MessageBoxResult.OK) {
                FW.Disconnect();
                FW = null;
                RaisePropertyChanged(nameof(FW));
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

        public ICommand ChooseFWCommand { get; private set; }

        public ICommand DisconnectCommand { get; private set; }

        public ICommand RefreshFWListCommand { get; private set; }
    }

    class FilterWheelChooserVM : EquipmentChooserVM {
        public override void GetEquipment() {
            Devices.Clear();
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
