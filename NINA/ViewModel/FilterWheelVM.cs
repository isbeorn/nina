#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

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
using NINA.Model.MyFilterWheel;
using NINA.Utility;
using NINA.Utility.Mediator;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel {

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
            DisconnectCommand = new RelayCommand(DisconnectFW);
            RefreshFWListCommand = new RelayCommand(RefreshFWList);
            ChangeFilterCommand = new AsyncCommand<bool>(async () => {
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
                                    changeFocus = focuserMediator.MoveFocuserRelative(offset);
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
                    Disconnect();
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
                Disconnect();

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

                            TargetFilter = FilterWheelInfo.SelectedFilter;

                            BroadcastFilterWheelInfo();

                            return true;
                        } else {
                            this.FW = null;
                            return false;
                        }
                    } catch (OperationCanceledException) {
                        if (fW?.Connected == true) { Disconnect(); }
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

        private void DisconnectFW(object obj) {
            var diag = MyMessageBox.MyMessageBox.Show("Disconnect Filter Wheel?", "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (diag == System.Windows.MessageBoxResult.OK) {
                Disconnect();
            }
        }

        public void Disconnect() {
            if (FW != null) {
                _changeFilterCancellationSource?.Cancel();
                FW.Disconnect();
                FW = null;
                FilterWheelInfo = DeviceInfo.CreateDefaultInstance<FilterWheelInfo>();
                RaisePropertyChanged(nameof(FW));
                BroadcastFilterWheelInfo();
            }
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

    internal class FilterWheelChooserVM : EquipmentChooserVM {

        public FilterWheelChooserVM(IProfileService profileService) : base(typeof(FilterWheelChooserVM), profileService) {
        }

        public override void GetEquipment() {
            Devices.Clear();

            Devices.Add(new DummyDevice(Locale.Loc.Instance["LblNoFilterwheel"]));

            try {
                foreach (IFilterWheel fw in ASCOMInteraction.GetFilterWheels(profileService)) {
                    Devices.Add(fw);
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }

            Devices.Add(new ManualFilterWheel(this.profileService));

            DetermineSelectedDevice(profileService.ActiveProfile.FilterWheelSettings.Id);
        }
    }
}