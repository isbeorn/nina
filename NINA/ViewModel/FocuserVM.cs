using NINA.Model;
using NINA.Model.MyFocuser;
using NINA.Utility;
using NINA.Utility.Mediator;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace NINA.ViewModel {
    class FocuserVM : DockableVM {

        public FocuserVM() {
            Title = "LblFocuser";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["FocusSVG"];

            ContentId = nameof(FocuserVM);
            ChooseFocuserCommand = new AsyncCommand<bool>(() => ChooseFocuser());
            CancelChooseFocuserCommand = new RelayCommand(CancelChooseFocuser);
            DisconnectCommand = new RelayCommand(DisconnectDiag);
            RefreshFocuserListCommand = new RelayCommand(RefreshFocuserList);
            MoveFocuserCommand = new AsyncCommand<int>(() => MoveFocuser(TargetPosition), (p) => Connected && TempComp == false);
            HaltFocuserCommand = new RelayCommand(HaltFocuser);

            Mediator.Instance.RegisterAsyncRequest(
                new MoveFocuserMessageHandle(async (MoveFocuserMessage msg) => {
                    if (msg.Absolute) {
                        return await MoveFocuser(msg.Position);
                    } else {
                        return await MoveFocuserRelative(msg.Position);
                    }
                })
            );

            Mediator.Instance.RegisterAsyncRequest(
                new ConnectFocuserMessageHandle(async (ConnectFocuserMessage msg) => {
                    await ChooseFocuserCommand.ExecuteAsync(null);
                    return true;
                })
            );

            Mediator.Instance.Register((o) => { RefreshFocuserList(o); }, MediatorMessages.ProfileChanged);
        }

        private void HaltFocuser(object obj) {
            _cancelMove?.Cancel();
            Focuser.Halt();
        }

        CancellationTokenSource _cancelMove;

        private async Task<int> MoveFocuser(int position) {
            _cancelMove = new CancellationTokenSource();
            int pos = -1;
            await Task.Run(() => {
                try {
                    while (Focuser.Position != position) {
                        IsMoving = true;
                        _cancelMove.Token.ThrowIfCancellationRequested();
                        Focuser.Move(position);
                    }
                    Position = position;
                    pos = position;
                } catch (OperationCanceledException) {

                }

            });
            return pos;
        }

        private async Task<int> MoveFocuserRelative(int offset) {
            int pos = -1;
            if (Focuser?.Connected == true) {
                pos = Focuser.Position + offset;
                await MoveFocuser(pos);
            }
            return pos;
        }

        private void UpdateFocuser_Tick(object sender, EventArgs e) {
            if (Focuser?.Connected == true) {
                Focuser.UpdateValues();
                this.Position = Focuser.Position;
            }
        }

        CancellationTokenSource _cancelChooseFocuserSource;

        private readonly SemaphoreSlim ss = new SemaphoreSlim(1, 1);
        public async Task<bool> ChooseFocuser() {
            await ss.WaitAsync();
            try {
                Disconnect();
                _cancelUpdateFocuserValues?.Cancel();

                if (FocuserChooserVM.SelectedDevice.Id == "No_Device") {
                    ProfileManager.Instance.ActiveProfile.FocuserSettings.Id = FocuserChooserVM.SelectedDevice.Id;
                    return false;
                }

                Mediator.Instance.Request(new StatusUpdateMessage() {
                    Status = new ApplicationStatus() {
                        Source = Title,
                        Status = Locale.Loc.Instance["LblConnecting"]
                    }
                });

                var focuser = (IFocuser)FocuserChooserVM.SelectedDevice;
                _cancelChooseFocuserSource = new CancellationTokenSource();
                if (focuser != null) {
                    try {
                        var connected = await focuser?.Connect(_cancelChooseFocuserSource.Token);
                        _cancelChooseFocuserSource.Token.ThrowIfCancellationRequested();
                        if (connected) {
                            this.Focuser = focuser;
                            Connected = true;
                            Notification.ShowSuccess(Locale.Loc.Instance["LblFocuserConnected"]);
                            _updateFocuserValuesProgress = new Progress<Dictionary<string, object>>(UpdateFocuserValues);
                            _cancelUpdateFocuserValues = new CancellationTokenSource();
                            _updateFocuserValuesTask = Task.Run(() => GetFocuserValues(_updateFocuserValuesProgress, _cancelUpdateFocuserValues.Token));

                            TargetPosition = Focuser.Position;
                            ProfileManager.Instance.ActiveProfile.FocuserSettings.Id = Focuser.Id;
                            return true;
                        } else {
                            Connected = false;
                            this.Focuser = null;
                            return false;
                        }
                    } catch (OperationCanceledException) {
                        if (Connected) { Disconnect(); }
                        return false;
                    }

                } else {
                    return false;
                }
            } finally {
                ss.Release();
                Mediator.Instance.Request(new StatusUpdateMessage() {
                    Status = new ApplicationStatus() {
                        Source = Title,
                        Status = string.Empty
                    }
                });
            }
        }

        private void CancelChooseFocuser(object o) {
            _cancelChooseFocuserSource?.Cancel();
        }

        private void GetFocuserValues(IProgress<Dictionary<string, object>> p, CancellationToken token) {
            Dictionary<string, object> focuserValues = new Dictionary<string, object>();
            try {
                do {
                    token.ThrowIfCancellationRequested();

                    focuserValues.Clear();
                    focuserValues.Add(nameof(Connected), _focuser?.Connected ?? false);
                    focuserValues.Add(nameof(Position), _focuser?.Position ?? 0);
                    focuserValues.Add(nameof(Temperature), _focuser?.Temperature ?? double.NaN);
                    focuserValues.Add(nameof(IsMoving), _focuser?.IsMoving ?? false);
                    focuserValues.Add(nameof(TempComp), _focuser?.TempComp ?? false);

                    p.Report(focuserValues);

                    token.ThrowIfCancellationRequested();

                    Thread.Sleep((int)(ProfileManager.Instance.ActiveProfile.ApplicationSettings.DevicePollingInterval * 1000));

                } while (Connected == true);
            } catch (OperationCanceledException) {

            } finally {
                focuserValues.Clear();
                focuserValues.Add(nameof(Connected), false);
                p.Report(focuserValues);
            }
        }

        private void UpdateFocuserValues(Dictionary<string, object> focuserValues) {
            object o = null;
            focuserValues.TryGetValue(nameof(Connected), out o);
            Connected = (bool)(o ?? false);

            focuserValues.TryGetValue(nameof(Position), out o);
            Position = (int)(o ?? 0);

            focuserValues.TryGetValue(nameof(Temperature), out o);
            Temperature = (double)(o ?? double.NaN);

            focuserValues.TryGetValue(nameof(IsMoving), out o);
            IsMoving = (bool)(o ?? false);

            focuserValues.TryGetValue(nameof(TempComp), out o);
            TempComp = (bool)(o ?? false);
        }




        private bool _connected;
        public bool Connected {
            get {
                return _connected;
            }
            private set {
                _connected = value;
                RaisePropertyChanged();
                Mediator.Instance.Notify(MediatorMessages.FocuserConnectedChanged, _connected);
            }
        }

        private int _position;
        public int Position {
            get {
                return _position;
            }
            private set {
                _position = value;
                RaisePropertyChanged();
            }
        }

        private double _temperature;
        public double Temperature {
            get {
                return _temperature;
            }
            private set {
                _temperature = value;
                RaisePropertyChanged();
                Mediator.Instance.Notify(MediatorMessages.FocuserTemperatureChanged, _temperature);
            }
        }

        private bool _isMoving;
        public bool IsMoving {
            get {
                return _isMoving;
            }
            private set {
                _isMoving = value;
                RaisePropertyChanged();
            }
        }

        private bool _tempComp;
        public bool TempComp {
            get {
                return _tempComp;
            }
            set {
                var prev = _tempComp;
                _tempComp = value;
                if (_focuser?.Connected == true && prev != _tempComp) {
                    _focuser.TempComp = _tempComp;
                }
                RaisePropertyChanged();
            }
        }

        private int _targetPosition;
        public int TargetPosition {
            get {
                return _targetPosition;
            }
            set {
                _targetPosition = value;
                RaisePropertyChanged();
            }
        }

        private void DisconnectDiag(object obj) {
            var diag = MyMessageBox.MyMessageBox.Show("Disconnect Focuser?", "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (diag == System.Windows.MessageBoxResult.OK) {
                Disconnect();
            }
        }

        public void Disconnect() {

            Connected = false;
            _cancelUpdateFocuserValues?.Cancel();
            do {
                Task.Delay(100);
            } while (!_updateFocuserValuesTask?.IsCompleted == true);
            Focuser?.Disconnect();
            Focuser = null;
            RaisePropertyChanged(nameof(Focuser));

        }

        public void RefreshFocuserList(object obj) {
            FocuserChooserVM.GetEquipment();
        }

        private IFocuser _focuser;
        public IFocuser Focuser {
            get {
                return _focuser;
            }
            private set {
                _focuser = value;
                RaisePropertyChanged();
            }
        }

        private FocuserChooserVM _focuserChooserVM;
        public FocuserChooserVM FocuserChooserVM {
            get {
                if (_focuserChooserVM == null) {
                    _focuserChooserVM = new FocuserChooserVM();
                }
                return _focuserChooserVM;
            }
            set {
                _focuserChooserVM = value;
            }
        }

        IProgress<Dictionary<string, object>> _updateFocuserValuesProgress;
        private CancellationTokenSource _cancelUpdateFocuserValues;
        private Task _updateFocuserValuesTask;

        public ICommand RefreshFocuserListCommand { get; private set; }

        public IAsyncCommand ChooseFocuserCommand { get; private set; }
        public ICommand CancelChooseFocuserCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }

        public ICommand MoveFocuserCommand { get; private set; }

        public ICommand HaltFocuserCommand { get; private set; }
    }

    class FocuserChooserVM : EquipmentChooserVM {
        public override void GetEquipment() {
            Devices.Clear();

            Devices.Add(new DummyDevice(Locale.Loc.Instance["LblNoFocuser"]));

            var ascomDevices = new ASCOM.Utilities.Profile();

            foreach (ASCOM.Utilities.KeyValuePair device in ascomDevices.RegisteredDevices("Focuser")) {

                try {
                    AscomFocuser focuser = new AscomFocuser(device.Key, device.Value);
                    Devices.Add(focuser);
                } catch (Exception) {
                    //only add filter wheels which are supported. e.g. x86 drivers will not work in x64
                }
            }

            DetermineSelectedDevice(ProfileManager.Instance.ActiveProfile.FocuserSettings.Id);
        }
    }
}
