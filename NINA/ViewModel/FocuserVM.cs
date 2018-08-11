using NINA.Model;
using NINA.Model.MyFocuser;
using NINA.Utility;
using NINA.Utility.Mediator;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel {

    internal class FocuserVM : DockableVM, IFocuserVM {

        public FocuserVM(IProfileService profileService, FocuserMediator focuserMediator) : base(profileService) {
            Title = "LblFocuser";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["FocusSVG"];

            this.focuserMediator = focuserMediator;
            this.focuserMediator.RegisterFocuserVM(this);

            ContentId = nameof(FocuserVM);
            ChooseFocuserCommand = new AsyncCommand<bool>(() => ChooseFocuser());
            CancelChooseFocuserCommand = new RelayCommand(CancelChooseFocuser);
            DisconnectCommand = new RelayCommand(DisconnectDiag);
            RefreshFocuserListCommand = new RelayCommand(RefreshFocuserList);
            MoveFocuserCommand = new AsyncCommand<int>(() => MoveFocuser(TargetPosition), (p) => FocuserInfo.Connected && FocuserInfo.TempComp == false);
            HaltFocuserCommand = new RelayCommand(HaltFocuser);
            ToggleTempCompCommand = new RelayCommand(ToggleTempComp);

            updateTimer = new DeviceUpdateTimer(
                GetFocuserValues,
                UpdateFocuserValues,
                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval
            );

            Mediator.Instance.Register((o) => { RefreshFocuserList(o); }, MediatorMessages.ProfileChanged);
        }

        private void ToggleTempComp(object obj) {
            if (FocuserInfo.Connected) {
                Focuser.TempComp = (bool)obj;
            }
        }

        private void HaltFocuser(object obj) {
            _cancelMove?.Cancel();
            Focuser.Halt();
        }

        private CancellationTokenSource _cancelMove;

        public async Task<int> MoveFocuser(int position) {
            _cancelMove = new CancellationTokenSource();
            int pos = -1;
            await Task.Run(() => {
                try {
                    while (Focuser.Position != position) {
                        FocuserInfo.IsMoving = true;
                        _cancelMove.Token.ThrowIfCancellationRequested();
                        Focuser.Move(position);
                    }
                    FocuserInfo.Position = position;
                    pos = position;
                    BroadcastFocuserInfo();
                } catch (OperationCanceledException) {
                }
            });
            return pos;
        }

        public async Task<int> MoveFocuserRelative(int offset) {
            int pos = -1;
            if (Focuser?.Connected == true) {
                pos = Focuser.Position + offset;
                await MoveFocuser(pos);
            }
            return pos;
        }

        private CancellationTokenSource _cancelChooseFocuserSource;

        private readonly SemaphoreSlim ss = new SemaphoreSlim(1, 1);

        public async Task<bool> ChooseFocuser() {
            await ss.WaitAsync();
            try {
                Disconnect();
                updateTimer?.Stop();

                if (FocuserChooserVM.SelectedDevice.Id == "No_Device") {
                    profileService.ActiveProfile.FocuserSettings.Id = FocuserChooserVM.SelectedDevice.Id;
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

                            FocuserInfo = new FocuserInfo {
                                Connected = true,
                                IsMoving = Focuser.IsMoving,
                                Name = Focuser.Name,
                                Position = Focuser.Position,
                                TempComp = Focuser.TempComp,
                                Temperature = Focuser.Temperature
                            };

                            Notification.ShowSuccess(Locale.Loc.Instance["LblFocuserConnected"]);

                            updateTimer.Interval = profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
                            updateTimer.Start();

                            TargetPosition = Focuser.Position;
                            profileService.ActiveProfile.FocuserSettings.Id = Focuser.Id;
                            return true;
                        } else {
                            FocuserInfo.Connected = false;
                            this.Focuser = null;
                            return false;
                        }
                    } catch (OperationCanceledException) {
                        if (FocuserInfo.Connected) { Disconnect(); }
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

        private Dictionary<string, object> GetFocuserValues() {
            Dictionary<string, object> focuserValues = new Dictionary<string, object>();
            focuserValues.Add(nameof(FocuserInfo.Connected), _focuser?.Connected ?? false);
            focuserValues.Add(nameof(FocuserInfo.Position), _focuser?.Position ?? 0);
            focuserValues.Add(nameof(FocuserInfo.Temperature), _focuser?.Temperature ?? double.NaN);
            focuserValues.Add(nameof(FocuserInfo.IsMoving), _focuser?.IsMoving ?? false);
            focuserValues.Add(nameof(FocuserInfo.TempComp), _focuser?.TempComp ?? false);
            return focuserValues;
        }

        private void UpdateFocuserValues(Dictionary<string, object> focuserValues) {
            object o = null;
            focuserValues.TryGetValue(nameof(FocuserInfo.Connected), out o);
            FocuserInfo.Connected = (bool)(o ?? false);

            focuserValues.TryGetValue(nameof(FocuserInfo.Position), out o);
            FocuserInfo.Position = (int)(o ?? 0);

            focuserValues.TryGetValue(nameof(FocuserInfo.Temperature), out o);
            FocuserInfo.Temperature = (double)(o ?? double.NaN);

            focuserValues.TryGetValue(nameof(FocuserInfo.IsMoving), out o);
            FocuserInfo.IsMoving = (bool)(o ?? false);

            focuserValues.TryGetValue(nameof(FocuserInfo.TempComp), out o);
            FocuserInfo.TempComp = (bool)(o ?? false);

            BroadcastFocuserInfo();
        }

        private FocuserInfo focuserInfo;

        public FocuserInfo FocuserInfo {
            get {
                if (focuserInfo == null) {
                    focuserInfo = new FocuserInfo {
                        Connected = false
                    };
                }
                return focuserInfo;
            }
            set {
                focuserInfo = value;
                RaisePropertyChanged();
            }
        }

        private void BroadcastFocuserInfo() {
            this.focuserMediator.UpdateFocuserInfo(FocuserInfo);
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
            FocuserInfo.Connected = false;
            updateTimer?.Stop();
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
                    _focuserChooserVM = new FocuserChooserVM(profileService);
                }
                return _focuserChooserVM;
            }
            set {
                _focuserChooserVM = value;
            }
        }

        private DeviceUpdateTimer updateTimer;
        private FocuserMediator focuserMediator;

        public ICommand RefreshFocuserListCommand { get; private set; }

        public IAsyncCommand ChooseFocuserCommand { get; private set; }
        public ICommand CancelChooseFocuserCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }

        public ICommand MoveFocuserCommand { get; private set; }

        public ICommand HaltFocuserCommand { get; private set; }
        public ICommand ToggleTempCompCommand { get; private set; }
    }

    internal class FocuserChooserVM : EquipmentChooserVM {

        public FocuserChooserVM(IProfileService profileService) : base(typeof(FocuserChooserVM), profileService) {
        }

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

            DetermineSelectedDevice(profileService.ActiveProfile.FocuserSettings.Id);
        }
    }
}