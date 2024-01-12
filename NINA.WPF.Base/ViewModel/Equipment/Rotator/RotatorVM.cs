#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MyRotator;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Utility.Notification;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Astrometry;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Model;
using NINA.Core.Locale;
using NINA.Core.MyMessageBox;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Equipment.Equipment;
using NINA.Equipment.Interfaces;
using Nito.AsyncEx;
using System.Linq;
using NINA.Core.Utility.Extensions;

namespace NINA.WPF.Base.ViewModel.Equipment.Rotator {

    public class RotatorVM : DockableVM, IRotatorVM {

        public RotatorVM(IProfileService profileService,
                         IRotatorMediator rotatorMediator,
                         IDeviceChooserVM rotatorChooserVM,
                         IApplicationResourceDictionary resourceDictionary,
                         IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            Title = Loc.Instance["LblRotator"];
            ImageGeometry = (System.Windows.Media.GeometryGroup)resourceDictionary["RotatorSVG"];
            HasSettings = true;

            this.rotatorMediator = rotatorMediator;
            this.rotatorMediator.RegisterHandler(this);
            this.applicationStatusMediator = applicationStatusMediator;
            DeviceChooserVM = rotatorChooserVM;

            ConnectCommand = new AsyncCommand<bool>(() => Task.Run(Connect), (object o) => DeviceChooserVM.SelectedDevice != null);
            CancelConnectCommand = new RelayCommand(CancelConnectRotator);
            DisconnectCommand = new AsyncCommand<bool>(() => Task.Run(DisconnectDiag));
            RescanDevicesCommand = new AsyncCommand<bool>(async o => { await Rescan(); return true; }, o => !RotatorInfo.Connected);
            _ = RescanDevicesCommand.ExecuteAsync(null);
            MoveCommand = new AsyncCommand<float>(() => Task.Run(() => Move(TargetPosition, CancellationToken.None)), (p) => RotatorInfo.Connected && RotatorInfo.Synced);
            MoveMechanicalCommand = new AsyncCommand<float>(() => Task.Run(() => MoveMechanical(TargetPosition, CancellationToken.None)), (p) => RotatorInfo.Connected);
            HaltCommand = new RelayCommand(Halt, (p) => RotatorInfo.Connected);
            ReverseCommand = new RelayCommand(Reverse, (p) => RotatorInfo.Connected);

            updateTimer = new DeviceUpdateTimer(
                GetRotatorValues,
                UpdateRotatorValues,
                profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval
            );

            profileService.ProfileChanged += async (object sender, EventArgs e) => {
                await RescanDevicesCommand.ExecuteAsync(null);
            };
        }

        public async Task<IList<string>> Rescan() {
            return await Task.Run(async () => {
                await DeviceChooserVM.GetEquipment();
                return DeviceChooserVM.Devices.Select(x => x.Id).ToList();
            });
        }

        private void Reverse(object obj) {
            try {
                if (obj is bool) {
                    var reverse = (bool)obj;
                    if (Rotator != null && RotatorInfo.Connected) {
                        Rotator.Reverse = reverse;
                        profileService.ActiveProfile.RotatorSettings.Reverse2 = reverse;
                    }
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        private void Halt(object obj) {
            try {
                _moveCts?.Cancel();
                Rotator?.Halt();
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public void Sync(float skyAngle) {
            try {
                if (RotatorInfo.Connected) {
                    Logger.Info($"Syncing Rotator to Sky Angle {skyAngle}°");
                    Rotator.Sync(skyAngle);
                    RotatorInfo.Position = Rotator.Position;
                    RotatorInfo.Synced = true;
                    BroadcastRotatorInfo();
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public async Task<float> Move(float requestedPosition, CancellationToken ct) {
            _moveCts?.Dispose();
            _moveCts = new CancellationTokenSource();
            float pos = float.NaN;
            await Task.Run(async () => {
                try {
                    RotatorInfo.IsMoving = true;

                    var adjustedTargetPosition = GetTargetPosition(requestedPosition);
                    if (Math.Abs(adjustedTargetPosition - requestedPosition) > 0.1) {
                        Logger.Info($"Adjusted rotator target to {adjustedTargetPosition}");
                        Notification.ShowInformation(String.Format(Loc.Instance["LblRotatorRangeAdjusted"], adjustedTargetPosition));
                    }

                    applicationStatusMediator.StatusUpdate(
                        new ApplicationStatus() {
                            Source = Title,
                            Status = string.Format(Loc.Instance["LblMovingRotatorToPosition"], Math.Round(adjustedTargetPosition, 2))
                        }
                    );

                    Logger.Debug($"Move rotator to {adjustedTargetPosition}°");
                    var anyCTS = CancellationTokenSource.CreateLinkedTokenSource(_moveCts.Token, ct);
                    using (anyCTS.Token.Register(() => Rotator?.Halt())) {
                        await Rotator.MoveAbsolute(adjustedTargetPosition, anyCTS.Token);
                        while (Rotator.IsMoving || !Angle.ByDegree(Rotator.Position).Equals(Angle.ByDegree(adjustedTargetPosition), Angle.ByDegree(1.0d))) {
                            anyCTS.Token.ThrowIfCancellationRequested();
                            await Task.Delay(TimeSpan.FromSeconds(1));
                            Logger.Trace($"Waiting for rotator to reach destination. IsMoving: {RotatorInfo.IsMoving} - Current Position {RotatorInfo.Position} - Target Position {adjustedTargetPosition}");
                        }
                    }
                    RotatorInfo.Position = adjustedTargetPosition;
                    pos = adjustedTargetPosition;
                    await updateTimer.WaitForNextUpdate(ct);
                    BroadcastRotatorInfo();
                } catch (OperationCanceledException) {
                } finally {
                    applicationStatusMediator.StatusUpdate(
                        new ApplicationStatus() {
                            Source = Title,
                            Status = string.Empty
                        }
                    );
                }
            });
            return pos;
        }

        public async Task<float> MoveMechanical(float requestedPosition, CancellationToken ct) {
            return await MoveMechanical(requestedPosition, TimeSpan.FromSeconds(1), ct);
        }

        public async Task<float> MoveMechanical(float requestedPosition, TimeSpan waitTime, CancellationToken ct) {
            _moveCts?.Dispose();
            _moveCts = new CancellationTokenSource();
            float pos = float.NaN;
            await Task.Run(async () => {
                try {
                    RotatorInfo.IsMoving = true;

                    var adjustedTargetPosition = GetTargetMechanicalPosition(requestedPosition);
                    if (Math.Abs(adjustedTargetPosition - requestedPosition) > 0.1) {
                        Logger.Info($"Adjusted rotator mechanical target to {adjustedTargetPosition}");
                        Notification.ShowInformation(String.Format(Loc.Instance["LblRotatorRangeAdjusted"], adjustedTargetPosition));
                    }

                    applicationStatusMediator.StatusUpdate(
                        new ApplicationStatus() {
                            Source = Title,
                            Status = string.Format(Loc.Instance["LblMovingRotatorToMechanicalPosition"], Math.Round(adjustedTargetPosition, 2))
                        }
                    );

                    Logger.Debug($"Move rotator mechanical to {adjustedTargetPosition}°");
                    var anyCTS = CancellationTokenSource.CreateLinkedTokenSource(_moveCts.Token, ct);
                    using (anyCTS.Token.Register(() => Rotator?.Halt())) {
                        await Rotator.MoveAbsoluteMechanical(adjustedTargetPosition, anyCTS.Token);
                        while (Rotator.IsMoving || !Angle.ByDegree(Rotator.MechanicalPosition).Equals(Angle.ByDegree(adjustedTargetPosition), Angle.ByDegree(1.0d))) {
                            anyCTS.Token.ThrowIfCancellationRequested();
                            await Task.Delay(waitTime);
                            Logger.Trace($"Waiting for rotator to reach destination. IsMoving: {RotatorInfo.IsMoving} - Current Position {RotatorInfo.MechanicalPosition} - Target Position {adjustedTargetPosition}");
                        }
                    }
                    RotatorInfo.Position = adjustedTargetPosition;
                    pos = adjustedTargetPosition;
                    await updateTimer.WaitForNextUpdate(ct);
                    BroadcastRotatorInfo();
                } catch (OperationCanceledException) {
                } finally {
                    applicationStatusMediator.StatusUpdate(
                        new ApplicationStatus() {
                            Source = Title,
                            Status = string.Empty
                        }
                    );
                }
            });
            return pos;
        }

        public async Task<float> MoveRelative(float offset, CancellationToken ct) {
            return await MoveRelative(offset, TimeSpan.FromSeconds(1), ct);
        }

        public async Task<float> MoveRelative(float offset, TimeSpan waitTime, CancellationToken ct) {
            if (Rotator?.Connected == true) {
                return await MoveMechanical(Rotator.MechanicalPosition + offset, waitTime, ct);
            }
            return -1;
        }

        private void UpdateRotatorValues(Dictionary<string, object> rotatorValues) {
            object o = null;
            rotatorValues.TryGetValue(nameof(RotatorInfo.Connected), out o);
            RotatorInfo.Connected = (bool)(o ?? false);

            rotatorValues.TryGetValue(nameof(RotatorInfo.Position), out o);
            RotatorInfo.Position = (float)(o ?? 0f);

            rotatorValues.TryGetValue(nameof(RotatorInfo.StepSize), out o);
            RotatorInfo.StepSize = (float)(o ?? 0f);

            rotatorValues.TryGetValue(nameof(RotatorInfo.IsMoving), out o);
            RotatorInfo.IsMoving = (bool)(o ?? false);

            rotatorValues.TryGetValue(nameof(RotatorInfo.Reverse), out o);
            RotatorInfo.Reverse = (bool)(o ?? false);

            rotatorValues.TryGetValue(nameof(RotatorInfo.Synced), out o);
            RotatorInfo.Synced = (bool)(o ?? false);

            rotatorValues.TryGetValue(nameof(RotatorInfo.MechanicalPosition), out o);
            RotatorInfo.MechanicalPosition = (float)(o ?? 0f);

            BroadcastRotatorInfo();
        }

        private Dictionary<string, object> GetRotatorValues() {
            Dictionary<string, object> rotatorValues = new Dictionary<string, object>();
            rotatorValues.Add(nameof(RotatorInfo.Connected), Rotator?.Connected ?? false);
            rotatorValues.Add(nameof(RotatorInfo.Position), Rotator?.Position ?? 0);
            rotatorValues.Add(nameof(RotatorInfo.IsMoving), Rotator?.IsMoving ?? false);
            rotatorValues.Add(nameof(RotatorInfo.StepSize), Rotator?.StepSize ?? 0);
            rotatorValues.Add(nameof(RotatorInfo.Reverse), Rotator?.Reverse ?? false);
            rotatorValues.Add(nameof(RotatorInfo.Synced), Rotator?.Synced ?? false);
            rotatorValues.Add(nameof(RotatorInfo.MechanicalPosition), Rotator?.MechanicalPosition ?? 0f);

            return rotatorValues;
        }

        private DeviceUpdateTimer updateTimer;
        private RotatorInfo rotatorInfo;

        public RotatorInfo RotatorInfo {
            get {
                if (rotatorInfo == null) {
                    rotatorInfo = DeviceInfo.CreateDefaultInstance<RotatorInfo>();
                }
                return rotatorInfo;
            }
            set {
                rotatorInfo = value;
                RaisePropertyChanged();
            }
        }

        public RotatorInfo GetDeviceInfo() {
            return RotatorInfo;
        }

        public IDeviceChooserVM DeviceChooserVM { get; private set; }

        private float targetPosition;

        public float TargetPosition {
            get => targetPosition;
            set { targetPosition = value; RaisePropertyChanged(); }
        }

        private IRotator rotator;
        public IRotator Rotator {
            get => rotator;
            private set {
                rotator = value;
                RaisePropertyChanged();
            }
        }
        private IRotatorMediator rotatorMediator;
        private IApplicationStatusMediator applicationStatusMediator;

        public IAsyncCommand ConnectCommand { get; private set; }
        public ICommand CancelConnectCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }
        public IAsyncCommand RescanDevicesCommand { get; private set; }
        public IAsyncCommand MoveCommand { get; private set; }
        public IAsyncCommand MoveMechanicalCommand { get; private set; }
        public ICommand HaltCommand { get; private set; }
        public ICommand ReverseCommand { get; private set; }

        private CancellationTokenSource _connectRotatorCts;
        private CancellationTokenSource _moveCts;
        private readonly SemaphoreSlim ss = new SemaphoreSlim(1, 1);

        public event Func<object, EventArgs, Task> Connected;
        public event Func<object, EventArgs, Task> Disconnected;

        public async Task<bool> Connect() {
            await ss.WaitAsync();
            try {
                await Disconnect();
                if (updateTimer != null) {
                    await updateTimer.Stop();
                }

                if (DeviceChooserVM.SelectedDevice.Id == "No_Device") {
                    profileService.ActiveProfile.RotatorSettings.Id = DeviceChooserVM.SelectedDevice.Id;
                    return false;
                }

                applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = Loc.Instance["LblConnecting"]
                    }
                );

                var rotator = (IRotator)DeviceChooserVM.SelectedDevice;
                _connectRotatorCts?.Dispose();
                _connectRotatorCts = new CancellationTokenSource();
                if (rotator != null) {
                    try {
                        var connected = await rotator?.Connect(_connectRotatorCts.Token);
                        _connectRotatorCts.Token.ThrowIfCancellationRequested();
                        if (connected) {
                            this.Rotator = rotator;

                            if (this.Rotator.CanReverse) {
                                this.Rotator.Reverse = profileService.ActiveProfile.RotatorSettings.Reverse2;
                            }

                            RotatorInfo = new RotatorInfo {
                                Connected = true,
                                IsMoving = Rotator.IsMoving,
                                Name = Rotator.Name,
                                Description = Rotator.Description,
                                Position = Rotator.Position,
                                StepSize = Rotator.StepSize,
                                DriverInfo = Rotator.DriverInfo,
                                DriverVersion = Rotator.DriverVersion,
                                CanReverse = Rotator.CanReverse,
                                Reverse = Rotator.Reverse,
                                DeviceId = Rotator.Id,
                                SupportedActions = Rotator.SupportedActions,
                            };

                            Notification.ShowSuccess(Loc.Instance["LblRotatorConnected"]);

                            updateTimer.Interval = profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
                            updateTimer.Start();

                            TargetPosition = Rotator.Position;
                            profileService.ActiveProfile.RotatorSettings.Id = Rotator.Id;
                            profileService.ActiveProfile.RotatorSettings.Reverse2 = this.Rotator.Reverse;

                            await (Connected?.InvokeAsync(this, new EventArgs()) ?? Task.CompletedTask);
                            Logger.Info($"Successfully connected Rotator. Id: {Rotator.Id} Name: {Rotator.Name} Driver Version: {Rotator.DriverVersion}");

                            return true;
                        } else {
                            RotatorInfo.Connected = false;
                            this.Rotator = null;
                            return false;
                        }
                    } catch (OperationCanceledException) {
                        if (RotatorInfo.Connected) { await Disconnect(); }
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

        private void CancelConnectRotator(object o) {
            try { _connectRotatorCts?.Cancel(); } catch { }
        }

        public async Task Disconnect() {
            try {
                if (RotatorInfo.Connected) {
                    if (updateTimer != null) {
                        await updateTimer.Stop();
                    }
                    Rotator?.Disconnect();
                    Rotator = null;
                    RotatorInfo = DeviceInfo.CreateDefaultInstance<RotatorInfo>();
                    BroadcastRotatorInfo();
                    await (Disconnected?.InvokeAsync(this, new EventArgs()) ?? Task.CompletedTask);
                    Logger.Info("Disconnected Rotator");
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        public string Action(string actionName, string actionParameters = "") {
            return RotatorInfo?.Connected == true ? Rotator.Action(actionName, actionParameters) : null;
        }

        public string SendCommandString(string command, bool raw = true) {
            return RotatorInfo?.Connected == true ? Rotator.SendCommandString(command, raw) : null;
        }

        public bool SendCommandBool(string command, bool raw = true) {
            return RotatorInfo?.Connected == true ? Rotator.SendCommandBool(command, raw) : false;
        }

        public void SendCommandBlind(string command, bool raw = true) {
            if (RotatorInfo?.Connected == true) {
                Rotator.SendCommandBlind(command, raw);
            }
        }

        private async Task<bool> DisconnectDiag() {
            var diag = MyMessageBox.Show(Loc.Instance["LblDisconnectRotator"], "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (diag == System.Windows.MessageBoxResult.OK) {
                await Disconnect();
            }
            return true;
        }

        private void BroadcastRotatorInfo() {
            rotatorMediator.Broadcast(GetDeviceInfo());
        }

        public float GetTargetPosition(float position) {
            if (!Rotator.Synced) {
                // This indicates a code bug from the caller, so this message is not localized
                throw new Exception("Rotator not synced!");
            }

            // Focuser position should be in [0, 360)
            position = AstroUtil.EuclidianModulus(position, 360);
            var offset = Rotator.MechanicalPosition - Rotator.Position;
            var mechanicalPosition = AstroUtil.EuclidianModulus(position + offset, 360);
            var targetMechanicalPosition = GetTargetMechanicalPosition(mechanicalPosition);
            return AstroUtil.EuclidianModulus(targetMechanicalPosition - offset + 360, 360);
        }

        public float GetTargetMechanicalPosition(float position) {
            // Focuser position should be in [0, 360)
            position = AstroUtil.EuclidianModulus(position, 360);
            var rangeType = profileService.ActiveProfile.RotatorSettings.RangeType;
            var rangeStart = profileService.ActiveProfile.RotatorSettings.RangeStartMechanicalPosition;
            float rangeStartDistance = AstroUtil.EuclidianModulus(position - rangeStart + 360, 360);
            float targetMechanicalPosition;
            if (rangeType == Core.Enum.RotatorRangeTypeEnum.FULL) {
                targetMechanicalPosition = position;
            } else if (rangeType == Core.Enum.RotatorRangeTypeEnum.HALF) {
                if (rangeStartDistance < 180.0) {
                    targetMechanicalPosition = position;
                } else {
                    targetMechanicalPosition = position + 180;
                }
            } else if (rangeType == Core.Enum.RotatorRangeTypeEnum.QUARTER) {
                if (rangeStartDistance < 90.0) {
                    targetMechanicalPosition = position;
                } else if (rangeStartDistance < 180.0) {
                    targetMechanicalPosition = position + 270;
                } else if (rangeStartDistance < 270.0) {
                    targetMechanicalPosition = position + 180;
                } else {
                    targetMechanicalPosition = position + 90;
                }
            } else {
                throw new NotImplementedException();
            }

            return AstroUtil.EuclidianModulus(targetMechanicalPosition, 360);
        }
        public IDevice GetDevice() {
            return Rotator;
        }
    }
}