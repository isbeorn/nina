#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyFilterWheel;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Profile;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel.Equipment.FilterWheel {

    internal class FilterWheelVM : DockableVM, IFilterWheelVM {

        public FilterWheelVM(IProfileService profileService, IFilterWheelMediator filterWheelMediator, IFocuserMediator focuserMediator, IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            Title = "LblFilterWheel";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["FWSVG"];

            this.filterWheelMediator = filterWheelMediator;
            this.filterWheelMediator.RegisterHandler(this);

            this.focuserMediator = focuserMediator;
            this.applicationStatusMediator = applicationStatusMediator;

            ChooseFWCommand = new AsyncCommand<bool>(() => ChooseFW());
            CancelChooseFWCommand = new RelayCommand(CancelChooseFW);
            DisconnectCommand = new AsyncCommand<bool>(() => DisconnectFW());
            RefreshFWListCommand = new RelayCommand(RefreshFWList, o => !(FW?.Connected == true));
            ChangeFilterCommand = new AsyncCommand<bool>(async () => {
                _changeFilterCancellationSource?.Dispose();
                _changeFilterCancellationSource = new CancellationTokenSource();
                await ChangeFilter(TargetFilter, _changeFilterCancellationSource.Token);
                return true;
            }, (object o) => FilterWheelInfo.Connected && !FilterWheelInfo.IsMoving);

            profileService.ProfileChanged += (object sender, EventArgs e) => {
                RefreshFWList(null);
            };
        }

        private CancellationTokenSource _changeFilterCancellationSource;

        //Instantiate a Singleton of the Semaphore with a value of 1. This means that only 1 thread can be granted access at a time.
        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        public async Task<FilterInfo> ChangeFilter(FilterInfo inputFilter, CancellationToken token = new CancellationToken(), IProgress<ApplicationStatus> progress = null) {
            progress?.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblSwitchingFilter"] });

            //Lock access so only one instance can change the filter
            await semaphoreSlim.WaitAsync(token);
            try {
                if (FW?.Connected == true) {
                    var prevFilter = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Where(x => x.Position == FilterWheelInfo.SelectedFilter?.Position).FirstOrDefault();
                    var filter = FW.Filters.Where((x) => x.Position == inputFilter.Position).FirstOrDefault();
                    if (filter == null) {
                        Notification.ShowWarning(string.Format(Locale.Loc.Instance["LblFilterNotFoundForPosition"], (inputFilter.Position + 1)));
                        return null;
                    }

                    if (FW?.Position != filter.Position) {
                        FilterWheelInfo.IsMoving = true;
                        Task changeFocus = null;
                        if (profileService.ActiveProfile.FocuserSettings.UseFilterWheelOffsets) {
                            if (prevFilter != null) {
                                var newFilter = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Where(x => x.Position == filter.Position).FirstOrDefault();
                                if (newFilter != null) {
                                    int offset = newFilter.FocusOffset - prevFilter.FocusOffset;
                                    if (offset != 0) {
                                        changeFocus = focuserMediator.MoveFocuserRelative(offset);
                                    }
                                }
                            }
                        }

                        FW.Position = filter.Position;
                        var changeFilter = Task.Run(async () => {
                            while (FW.Position == -1) {
                                await Task.Delay(1000);
                                token.ThrowIfCancellationRequested();
                            }
                        });

                        if (changeFocus != null) {
                            await changeFocus;
                        }

                        await changeFilter;
                    }
                    FilterWheelInfo.SelectedFilter = filter;
                } else {
                    await Disconnect();
                }
            } finally {
                BroadcastFilterWheelInfo();
                //unlock access
                FilterWheelInfo.IsMoving = false;
                semaphoreSlim.Release();
            }
            progress?.Report(new ApplicationStatus() { Status = string.Empty });
            return FilterWheelInfo.SelectedFilter;
        }

        private void RefreshFWList(object obj) {
            FilterWheelChooserVM.GetEquipment();
        }

        private IFilterWheel _fW;

        public IFilterWheel FW {
            get {
                return _fW;
            }
            private set {
                _fW = value;
                RaisePropertyChanged();
            }
        }

        private FilterInfo targetFilter;

        public FilterInfo TargetFilter {
            get {
                return targetFilter;
            }
            set {
                targetFilter = value;
                RaisePropertyChanged();
            }
        }

        private readonly SemaphoreSlim ss = new SemaphoreSlim(1, 1);

        private async Task<bool> ChooseFW() {
            await ss.WaitAsync();
            try {
                await Disconnect();

                if (FilterWheelChooserVM.SelectedDevice.Id == "No_Device") {
                    profileService.ActiveProfile.FilterWheelSettings.Id = FilterWheelChooserVM.SelectedDevice.Id;
                    return false;
                }

                applicationStatusMediator.StatusUpdate(
                    new ApplicationStatus() {
                        Source = Title,
                        Status = Locale.Loc.Instance["LblConnecting"]
                    }
                );

                var fW = (IFilterWheel)FilterWheelChooserVM.SelectedDevice;
                _cancelChooseFilterWheelSource?.Dispose();
                _cancelChooseFilterWheelSource = new CancellationTokenSource();
                if (fW != null) {
                    try {
                        var connected = await fW?.Connect(_cancelChooseFilterWheelSource.Token);
                        _cancelChooseFilterWheelSource.Token.ThrowIfCancellationRequested();
                        if (connected) {
                            this.FW = fW;

                            FilterWheelInfo = new FilterWheelInfo {
                                Connected = true,
                                IsMoving = false,
                                Name = FW.Name
                            };

                            Notification.ShowSuccess(Locale.Loc.Instance["LblFilterwheelConnected"]);
                            profileService.ActiveProfile.FilterWheelSettings.Id = FW.Id;
                            if (FW.Position > -1) {
                                FilterWheelInfo.SelectedFilter = FW.Filters[FW.Position];
                            }

                            // Auto import filters to profile, when profile does not have any filters set yet.
                            if (FW.Filters.Count > 0 && profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Count == 0) {
                                var l = FW.Filters.OrderBy(x => x.Position);
                                foreach (var filter in l) {
                                    profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters.Add(filter);
                                }
                            }

                            TargetFilter = FilterWheelInfo.SelectedFilter;

                            BroadcastFilterWheelInfo();

                            Logger.Info($"Successfully connected Filter Wheel. Id: {FW.Id} Name: {FW.Name} Driver Version: {FW.DriverVersion}");

                            return true;
                        } else {
                            this.FW = null;
                            return false;
                        }
                    } catch (OperationCanceledException) {
                        if (fW?.Connected == true) { await Disconnect(); }
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

        private void CancelChooseFW(object o) {
            _cancelChooseFilterWheelSource?.Cancel();
        }

        private CancellationTokenSource _cancelChooseFilterWheelSource;

        private async Task<bool> DisconnectFW() {
            var diag = MyMessageBox.MyMessageBox.Show("Disconnect Filter Wheel?", "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (diag == System.Windows.MessageBoxResult.OK) {
                await Disconnect();
            }
            return true;
        }

        public Task Disconnect() {
            if (FW != null) {
                _changeFilterCancellationSource?.Cancel();
                FW.Disconnect();
                FW = null;
                FilterWheelInfo = DeviceInfo.CreateDefaultInstance<FilterWheelInfo>();
                RaisePropertyChanged(nameof(FW));
                BroadcastFilterWheelInfo();
                Logger.Info("Disconnected Filter Wheel");
            }
            return Task.CompletedTask;
        }

        private FilterWheelChooserVM _filterWheelChooserVM;
        private IFilterWheelMediator filterWheelMediator;
        private IFocuserMediator focuserMediator;
        private IApplicationStatusMediator applicationStatusMediator;
        private FilterWheelInfo filterWheelInfo;

        public FilterWheelInfo FilterWheelInfo {
            get {
                if (filterWheelInfo == null) {
                    filterWheelInfo = DeviceInfo.CreateDefaultInstance<FilterWheelInfo>();
                }
                return filterWheelInfo;
            }
            set {
                filterWheelInfo = value;
                RaisePropertyChanged();
            }
        }

        private void BroadcastFilterWheelInfo() {
            this.filterWheelMediator.Broadcast(FilterWheelInfo);
        }

        public ICollection<FilterInfo> GetAllFilters() {
            if (FilterWheelInfo.Connected) {
                return FW?.Filters;
            } else {
                return null;
            }
        }

        public Task<bool> Connect() {
            return ChooseFW();
        }

        public FilterWheelInfo GetDeviceInfo() {
            return FilterWheelInfo;
        }

        public FilterWheelChooserVM FilterWheelChooserVM {
            get {
                if (_filterWheelChooserVM == null) {
                    _filterWheelChooserVM = new FilterWheelChooserVM(profileService);
                    _filterWheelChooserVM.GetEquipment();
                }
                return _filterWheelChooserVM;
            }
            set {
                _filterWheelChooserVM = value;
            }
        }

        public IAsyncCommand ChooseFWCommand { get; private set; }
        public ICommand CancelChooseFWCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }
        public ICommand RefreshFWListCommand { get; private set; }
        public IAsyncCommand ChangeFilterCommand { get; private set; }
    }
}