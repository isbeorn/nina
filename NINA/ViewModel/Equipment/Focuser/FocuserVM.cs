#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyFocuser;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Profile;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel.Equipment.Focuser {

    internal class FocuserVM : DockableVM, IFocuserVM {

        public FocuserVM(IProfileService profileService, IFocuserMediator focuserMediator, IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            Title = "LblFocuser";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["FocusSVG"];

            this.focuserMediator = focuserMediator;
            this.focuserMediator.RegisterHandler(this);
            this.applicationStatusMediator = applicationStatusMediator;

            ChooseFocuserCommand = new AsyncCommand<bool>(() => ChooseFocuser());
            CancelChooseFocuserCommand = new RelayCommand(CancelChooseFocuser);
            DisconnectCommand = new RelayCommand(DisconnectDiag);
            RefreshFocuserListCommand = new RelayCommand(RefreshFocuserList);
            MoveFocuserInSmallCommand = new AsyncCommand<int>(() => MoveFocuserRelative((int)Math.Round(profileService.ActiveProfile.FocuserSettings.AutoFocusStepSize / -2d)), (p) => FocuserInfo.Connected);
            MoveFocuserInLargeCommand = new AsyncCommand<int>(() => MoveFocuserRelative(profileService.ActiveProfile.FocuserSettings.AutoFocusStepSize * -5), (p) => FocuserInfo.Connected);
            MoveFocuserOutSmallCommand = new AsyncCommand<int>(() => MoveFocuserRelative((int)Math.Round(profileService.ActiveProfile.FocuserSettings.AutoFocusStepSize / 2d)), (p) => FocuserInfo.Connected);
            MoveFocuserOutLargeCommand = new AsyncCommand<int>(() => MoveFocuserRelative(profileService.ActiveProfile.FocuserSettings.AutoFocusStepSize * 5), (p) => FocuserInfo.Connected);
            MoveFocuserCommand = new AsyncCommand<int>(() => MoveFocuser(TargetPosition), (p) => FocuserInfo.Connected);
            HaltFocuserCommand = new RelayCommand(HaltFocuser);
            ToggleTempCompCommand = new RelayCommand(ToggleTempComp);

            updateTimer = new DeviceUpdateTimer(
                GetFocuserValues,
                UpdateFocuserValues,
                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval
            );

            profileService.ProfileChanged += (object sender, EventArgs e) => {
                RefreshFocuserList(null);
            };
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

        private enum Direction {
            IN,
            OUT,
            NONE
        }

        private Direction _lastFocuserDirection = Direction.NONE;
        private int _focuserOffset = 0;
        private CancellationTokenSource _cancelMove;

        public async Task<int> MoveFocuser(int position) {
            _cancelMove?.Dispose();
            _cancelMove = new CancellationTokenSource();
            int pos = -1;
            int initialPos = this.Position;
            int backlashCompensation = GetBacklashCompensation(initialPos, position);
            position += backlashCompensation;
            position += _focuserOffset;
            await Task.Run(async () => {
                try {
                    var tempComp = false;
                    if (Focuser.TempCompAvailable && Focuser.TempComp) {
                        tempComp = true;
                        ToggleTempComp(false);
                    }
                    while (Focuser.Position != position) {
                        FocuserInfo.IsMoving = true;
                        _cancelMove.Token.ThrowIfCancellationRequested();
                        await Focuser.Move(position, _cancelMove.Token);
                    }
                    //Wait for focuser to settle
                    if (profileService.ActiveProfile.FocuserSettings.FocuserSettleTime > 0) {
                        FocuserInfo.IsSettling = true;
                        TimeSpan totalSettleTime = TimeSpan.FromSeconds(profileService.ActiveProfile.FocuserSettings.FocuserSettleTime);
                        TimeSpan elapsedSettleTime = TimeSpan.Zero;
                        while (elapsedSettleTime.TotalMilliseconds < totalSettleTime.TotalMilliseconds) {
                            applicationStatusMediator.StatusUpdate(new ApplicationStatus { Source = Title, Status = Locale.Loc.Instance["LblSettle"], Progress = elapsedSettleTime.TotalSeconds, MaxProgress = (int)totalSettleTime.TotalSeconds, ProgressType = ApplicationStatus.StatusProgressType.ValueOfMaxValue });
                            await Utility.Utility.Delay(TimeSpan.FromSeconds(1), _cancelMove.Token);
                            elapsedSettleTime = elapsedSettleTime.Add(TimeSpan.FromSeconds(1));
                        }
                    }

                    _lastFocuserDirection = MoveDirection(initialPos, position);
                    _focuserOffset += backlashCompensation;

                    FocuserInfo.Position = this.Position;
                    pos = this.Position;
                    ToggleTempComp(tempComp);
                    BroadcastFocuserInfo();
                } catch (OperationCanceledException) {
                } finally {
                    FocuserInfo.IsSettling = false;
                    applicationStatusMediator.StatusUpdate(new ApplicationStatus { Source = Title, Status = string.Empty });
                }
            });
            return pos;
        }

        private int GetBacklashCompensation(int oldPos, int newPos) {
            if (newPos > oldPos && _lastFocuserDirection == Direction.IN) {
                return profileService.ActiveProfile.FocuserSettings.BacklashOut;
            } else if (newPos < oldPos && _lastFocuserDirection == Direction.OUT) {
                return profileService.ActiveProfile.FocuserSettings.BacklashIn * -1;
            } else {
                return 0;
            }
        }

        private Direction MoveDirection(int oldPos, int newPos) {
            if (newPos > oldPos) {
                return Direction.OUT;
            } else if (newPos < oldPos) {
                return Direction.IN;
            } else {
                return _lastFocuserDirection;
            }
        }

        public async Task<int> MoveFocuserRelative(int offset) {
            int pos = -1;
            if (Focuser?.Connected == true) {
                pos = this.Position + offset;
                pos = await MoveFocuser(pos);
            }
            return pos;
        }

        private CancellationTokenSource _cancelChooseFocuserSource;

        private readonly SemaphoreSlim ss = new SemaphoreSlim(1, 1);

        private async Task<bool> ChooseFocuser() {
            await ss.WaitAsync();
            try {
                Disconnect();
                updateTimer?.Stop();

                if (FocuserChooserVM.SelectedDevice.Id == "No_Device") {
                    profileService.ActiveProfile.FocuserSettings.Id = FocuserChooserVM.SelectedDevice.Id;
                    return false;
                }

                applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = Locale.Loc.Instance["LblConnecting"]
                    }
                );

                var focuser = (IFocuser)FocuserChooserVM.SelectedDevice;
                _cancelChooseFocuserSource?.Dispose();
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
                                Position = this.Position,
                                StepSize = Focuser.StepSize,
                                TempComp = Focuser.TempComp,
                                Temperature = Focuser.Temperature
                            };

                            Notification.ShowSuccess(Locale.Loc.Instance["LblFocuserConnected"]);

                            updateTimer.Interval = profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
                            updateTimer.Start();

                            TargetPosition = this.Position;
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
                applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = string.Empty
                    }
                );
            }
        }

        private void CancelChooseFocuser(object o) {
            _cancelChooseFocuserSource?.Cancel();
        }

        private Dictionary<string, object> GetFocuserValues() {
            Dictionary<string, object> focuserValues = new Dictionary<string, object>();
            focuserValues.Add(nameof(FocuserInfo.Connected), _focuser?.Connected ?? false);
            focuserValues.Add(nameof(FocuserInfo.Position), this?.Position ?? 0);
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
                    focuserInfo = DeviceInfo.CreateDefaultInstance<FocuserInfo>();
                }
                return focuserInfo;
            }
            set {
                focuserInfo = value;
                RaisePropertyChanged();
            }
        }

        private void BroadcastFocuserInfo() {
            this.focuserMediator.Broadcast(FocuserInfo);
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

        public int Position {
            get {
                return Focuser.Position - _focuserOffset;
            }
        }

        private void DisconnectDiag(object obj) {
            var diag = MyMessageBox.MyMessageBox.Show("Disconnect Focuser?", "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (diag == System.Windows.MessageBoxResult.OK) {
                Disconnect();
            }
        }

        public void Disconnect() {
            updateTimer?.Stop();
            Focuser?.Disconnect();
            Focuser = null;
            FocuserInfo = DeviceInfo.CreateDefaultInstance<FocuserInfo>();
            BroadcastFocuserInfo();
            RaisePropertyChanged(nameof(Focuser));
        }

        public void RefreshFocuserList(object obj) {
            FocuserChooserVM.GetEquipment();
        }

        public Task<bool> Connect() {
            return ChooseFocuser();
        }

        public FocuserInfo GetDeviceInfo() {
            return FocuserInfo;
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
                    _focuserChooserVM.GetEquipment();
                }
                return _focuserChooserVM;
            }
            set {
                _focuserChooserVM = value;
            }
        }

        private DeviceUpdateTimer updateTimer;
        private IFocuserMediator focuserMediator;
        private IApplicationStatusMediator applicationStatusMediator;

        public ICommand RefreshFocuserListCommand { get; private set; }

        public IAsyncCommand ChooseFocuserCommand { get; private set; }
        public ICommand CancelChooseFocuserCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }

        public ICommand MoveFocuserCommand { get; private set; }
        public ICommand MoveFocuserInSmallCommand { get; private set; }
        public ICommand MoveFocuserInLargeCommand { get; private set; }
        public ICommand MoveFocuserOutSmallCommand { get; private set; }
        public ICommand MoveFocuserOutLargeCommand { get; private set; }
        public ICommand HaltFocuserCommand { get; private set; }
        public ICommand ToggleTempCompCommand { get; private set; }
    }
}