#region "copyright"

/*
    Copyright © 2016 - 2025 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.INDI;
using NINA.INDI.Devices;
using NINA.INDI.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyFocuser {

    public partial class IndiFocuser : IndiDevice<IINDIFocuser>, IFocuser, IDisposable {

        public IndiFocuser(INDIDeviceInfo info) : base(info) {
        }

        public bool IsMoving => GetProperty(nameof(IINDIFocuser.IsMoving), false);

        public int MaxIncrement => GetProperty(nameof(IINDIFocuser.MaxIncrement), -1);

        public int MaxStep {
            get => GetProperty(nameof(IINDIFocuser.MaxStep), -1);
            set {
                if (ShouldBeConnected) {
                    SetProperty(nameof(IINDIFocuser.MaxStep), value);
                }
            }
        }

        private bool _isAbsolute = true;

        //Used for relative focusers
        private int internalPosition;

        public int Position {
            get {
                if (_isAbsolute) {
                    var val = GetProperty(nameof(IINDIFocuser.Position), -1); ;
                    return val;
                } else {
                    return internalPosition;
                }
            }
        }

        public double StepSize => GetProperty(nameof(IINDIFocuser.StepSize), double.NaN);

        public bool TempCompAvailable => GetProperty(nameof(IINDIFocuser.TempCompAvailable), false);

        public bool TempComp {
            get {
                if (TempCompAvailable) {
                    return GetProperty(nameof(IINDIFocuser.TempComp), false);
                } else {
                    return false;
                }
            }
            set {
                if (ShouldBeConnected && TempCompAvailable) {
                    SetProperty(nameof(IINDIFocuser.TempComp), value);
                }
            }
        }

        public double Temperature => GetProperty(nameof(IINDIFocuser.Temperature), double.NaN);

        private bool _canReverse = true;

        [ObservableProperty]
        private bool reversed = false;

        partial void OnReversedChanged(bool value) {
            if (ShouldBeConnected && _canReverse) {
                try {
                    device.Reverse = value;
                } catch (NotImplementedException) {
                    _canReverse = false;
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
            }
        }

        [RelayCommand]
        public void ResetPosition() {
            if (Position > 0) {
                device.SyncPosition(0);
                RaisePropertyChanged(nameof(Position));
            }
        }

        public Task Move(int position, CancellationToken ct, int waitInMs = 1000) {
            if (_isAbsolute) {
                return MoveInternalAbsolute(position, ct, waitInMs);
            } else {
                return MoveInternalRelative(position, ct, waitInMs);
            }
        }

        private static TimeSpan SameFocuserPositionTimeout = TimeSpan.FromMinutes(1);

        private async Task MoveInternalAbsolute(int position, CancellationToken ct, int waitInMs = 1000) {
            if (ShouldBeConnected) {
                var reEnableTempComp = TempComp;
                if (reEnableTempComp) {
                    TempComp = false;
                }

                var lastPosition = int.MinValue;
                int samePositionCount = 0;
                var lastMovementTime = DateTime.Now;
                while (position != Position && !ct.IsCancellationRequested) {
                    await device.MoveAsync(position, ct);
                    InvalidatePropertyCache();

                    if (lastPosition == Position) {
                        ++samePositionCount;
                        var samePositionTime = DateTime.Now - lastMovementTime;
                        if (samePositionTime >= SameFocuserPositionTimeout) {
                            throw new Exception($"Focuser stuck at position {lastPosition} beyond {SameFocuserPositionTimeout} timeout");
                        }

                        // Make sure we wait in between Move requests when no progress is being made
                        // to avoid spamming the driver and spiking the CPU
                        await CoreUtil.Wait(TimeSpan.FromSeconds(1), ct);
                    } else {
                        lastMovementTime = DateTime.Now;
                    }
                    lastPosition = device.Position;
                }

                if (reEnableTempComp) {
                    TempComp = true;
                }
            }
        }

        private async Task MoveInternalRelative(int position, CancellationToken ct, int waitInMs = 1000) {
            if (ShouldBeConnected) {
                var reEnableTempComp = TempComp;
                if (reEnableTempComp) {
                    TempComp = false;
                }

                var relativeOffsetRemaining = position - this.Position;
                while (relativeOffsetRemaining != 0 && !ct.IsCancellationRequested) {
                    var moveAmount = Math.Min(MaxIncrement, Math.Abs(relativeOffsetRemaining));
                    if (relativeOffsetRemaining < 0) {
                        moveAmount *= -1;
                    }
                    await device.MoveAsync(moveAmount, ct);
                    InvalidatePropertyCache();

                    while (IsMoving && !ct.IsCancellationRequested) {
                        await CoreUtil.Wait(TimeSpan.FromMilliseconds(waitInMs), ct);
                    }
                    relativeOffsetRemaining -= moveAmount;
                    internalPosition += moveAmount;
                }

                if (reEnableTempComp) {
                    TempComp = true;
                }
            }
        }

        private bool _canHalt;

        public void Halt() {
            if (ShouldBeConnected && _canHalt) {
                try {
                    device.Halt();
                } catch (NotImplementedException) {
                    _canHalt = false;
                } catch (Exception ex) {
                    Logger.Error(ex);
                }
            }
        }

        protected override string ConnectionLostMessage => "FocuserConnectionLost";

        private void Initialize() {
            internalPosition = device.MaxStep / 2;
            _isAbsolute = device.Absolute;
            if (!_isAbsolute) {
                Logger.Info("The focuser is a relative focuser. Simulating absolute focuser behavior");
            }
            _canHalt = true;
        }

        protected override Task PostConnect() {
            Initialize();
            return Task.CompletedTask;
        }

        protected override IINDIFocuser GetInstance() {
            return device ??= new INDIFocuser(_device);
        }
    }
}
