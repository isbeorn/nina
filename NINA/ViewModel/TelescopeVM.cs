using NINA.Model;
using NINA.Model.MyTelescope;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace NINA.ViewModel {

    internal class TelescopeVM : DockableVM {

        public TelescopeVM(IProfileService profileService) : base(profileService) {
            this.profileService = profileService;
            Title = "LblTelescope";
            ContentId = nameof(TelescopeVM);
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["TelescopeSVG"];

            ChooseTelescopeCommand = new AsyncCommand<bool>(() => ChooseTelescope());
            CancelChooseTelescopeCommand = new RelayCommand(CancelChooseTelescope);
            DisconnectCommand = new RelayCommand(DisconnectTelescope);
            StepperMoveRateCommand = new RelayCommand(StepMoveRate);
            ParkCommand = new AsyncCommand<bool>(ParkTelescope);
            UnparkCommand = new RelayCommand(UnparkTelescope);
            SlewToCoordinatesCommand = new RelayCommand(SlewToCoordinates);
            RefreshTelescopeListCommand = new RelayCommand(RefreshTelescopeList);

            MoveCommand = new RelayCommand(Move);
            StopMoveCommand = new RelayCommand(StopMove);
            StopSlewCommand = new RelayCommand(StopSlew);

            Mediator.Instance.RegisterRequest(
                new SetTelescopeTrackingMessageHandle((SetTelescopeTrackingMessage msg) => {
                    if (Telescope?.Connected == true) {
                        Telescope.Tracking = msg.Tracking;
                        return true;
                    } else {
                        return false;
                    }
                })
            );

            Mediator.Instance.RegisterAsyncRequest(
                new SlewTocoordinatesMessageHandle(async (SlewToCoordinatesMessage msg) => {
                    return await SlewToCoordinatesAsync(msg.Coordinates);
                })
            );

            Mediator.Instance.RegisterRequest(
                new SendSnapPortMessageHandle((SendSnapPortMessage msg) => {
                    return SendToSnapPort(msg.Start);
                })
            );

            Mediator.Instance.RegisterAsyncRequest(
                new ConnectTelescopeMessageHandle(async (ConnectTelescopeMessage msg) => {
                    await ChooseTelescopeCommand.ExecuteAsync(null);
                    return true;
                })
            );

            Mediator.Instance.Register((o) => { RefreshTelescopeList(o); }, MediatorMessages.ProfileChanged);
        }

        private bool SendToSnapPort(bool start) {
            if (Telescope?.Connected == true) {
                string command = string.Empty;
                if (start) {
                    command = profileService.ActiveProfile.TelescopeSettings.SnapPortStart;
                } else {
                    command = profileService.ActiveProfile.TelescopeSettings.SnapPortStop;
                }
                _telescope?.SendCommandString(command);
                return true;
            } else {
                Notification.ShowError(Locale.Loc.Instance["LblTelescopeNotConnectedForCommand"]);
                return false;
            }
        }

        private void RefreshTelescopeList(object obj) {
            TelescopeChooserVM.GetEquipment();
        }

        private void UpdateTelescope_Tick(object sender, EventArgs e) {
            if (Telescope?.Connected == true) {
                Telescope.UpdateValues();
            }
        }

        private async Task<bool> ParkTelescope() {
            return await Task.Run<bool>(() => { Telescope.Park(); return true; });
        }

        private void UnparkTelescope(object o) {
            Telescope.Unpark();
        }

        //private DispatcherTimer _updateTelescope;

        private ITelescope _telescope;

        public ITelescope Telescope {
            get {
                return _telescope;
            }
            private set {
                _telescope = value;
                RaisePropertyChanged();
                Mediator.Instance.Notify(MediatorMessages.TelescopeChanged, _telescope);
            }
        }

        private TelescopeChooserVM _telescopeChooserVM;

        public TelescopeChooserVM TelescopeChooserVM {
            get {
                if (_telescopeChooserVM == null) {
                    _telescopeChooserVM = new TelescopeChooserVM(profileService);
                }
                return _telescopeChooserVM;
            }
            set {
                _telescopeChooserVM = value;
            }
        }

        private readonly SemaphoreSlim ss = new SemaphoreSlim(1, 1);

        private async Task<bool> ChooseTelescope() {
            await ss.WaitAsync();
            try {
                Disconnect();
                _cancelUpdateTelescopeValues?.Cancel();

                if (TelescopeChooserVM.SelectedDevice.Id == "No_Device") {
                    profileService.ActiveProfile.TelescopeSettings.Id = TelescopeChooserVM.SelectedDevice.Id;
                    return false;
                }

                Mediator.Instance.Request(new StatusUpdateMessage() {
                    Status = new ApplicationStatus() {
                        Source = Title,
                        Status = Locale.Loc.Instance["LblConnecting"]
                    }
                });

                var telescope = (ITelescope)TelescopeChooserVM.SelectedDevice;
                _cancelChooseTelescopeSource = new CancellationTokenSource();
                if (telescope != null) {
                    try {
                        var connected = await telescope?.Connect(_cancelChooseTelescopeSource.Token);
                        _cancelChooseTelescopeSource.Token.ThrowIfCancellationRequested();
                        if (connected) {
                            Telescope = telescope;

                            if (Telescope.CanSetSiteLatLong && (Telescope.SiteLatitude != profileService.ActiveProfile.AstrometrySettings.Latitude || Telescope.SiteLongitude != profileService.ActiveProfile.AstrometrySettings.Longitude)) {
                                if (MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblSyncLatLongText"], Locale.Loc.Instance["LblSyncLatLong"], System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes) {
                                    Telescope.SiteLatitude = profileService.ActiveProfile.AstrometrySettings.Latitude;
                                    Telescope.SiteLongitude = profileService.ActiveProfile.AstrometrySettings.Longitude;
                                }
                            }

                            _updateTelescopeValuesProgress = new Progress<Dictionary<string, object>>(UpdateTelescopeValues);
                            _cancelUpdateTelescopeValues = new CancellationTokenSource();
                            _updateTelescopeValuesTask = Task.Run(async () => await GetTelescopeValues(_updateTelescopeValuesProgress, _cancelUpdateTelescopeValues.Token));

                            Notification.ShowSuccess(Locale.Loc.Instance["LblTelescopeConnected"]);
                            profileService.ActiveProfile.TelescopeSettings.Id = Telescope.Id;
                            return true;
                        } else {
                            Telescope = null;
                            return false;
                        }
                    } catch (OperationCanceledException) {
                        if (telescope?.Connected == true) { Disconnect(); }
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

        private bool _connected;

        public bool Connected {
            get {
                return _connected;
            }
            private set {
                var prevVal = _connected;
                _connected = value;
                RaisePropertyChanged();
                if (prevVal != _connected) {
                    Mediator.Instance.Notify(MediatorMessages.TelescopeConnectedChanged, _connected);
                }
            }
        }

        private string _altitudeString;

        public string AltitudeString {
            get {
                return _altitudeString;
            }
            private set {
                _altitudeString = value;
                RaisePropertyChanged();
            }
        }

        private string _azimuthString;

        public string AzimuthString {
            get {
                return _azimuthString;
            }
            private set {
                _azimuthString = value;
                RaisePropertyChanged();
            }
        }

        private string _declinationString;

        public string DeclinationString {
            get {
                return _declinationString;
            }
            private set {
                _declinationString = value;
                RaisePropertyChanged();
            }
        }

        private string _rightAscensionString;

        public string RightAscensionString {
            get {
                return _rightAscensionString;
            }
            private set {
                _rightAscensionString = value;
                RaisePropertyChanged();
            }
        }

        private string _siderealTimeString;

        public string SiderealTimeString {
            get {
                return _siderealTimeString;
            }
            private set {
                _siderealTimeString = value;
                RaisePropertyChanged();
            }
        }

        private string _hoursToMeridianString;

        public string HoursToMeridianString {
            get {
                return _hoursToMeridianString;
            }
            private set {
                _hoursToMeridianString = value;
                RaisePropertyChanged();
            }
        }

        private bool _tracking;

        public bool Tracking {
            get {
                return _tracking;
            }
            private set {
                _tracking = value;
                RaisePropertyChanged();
            }
        }

        private bool _atPark;

        public bool AtPark {
            get {
                return _atPark;
            }
            private set {
                _atPark = value;
                RaisePropertyChanged();
            }
        }

        private IProgress<Dictionary<string, object>> _updateTelescopeValuesProgress;
        private CancellationTokenSource _cancelUpdateTelescopeValues;
        private Task _updateTelescopeValuesTask;

        private void UpdateTelescopeValues(Dictionary<string, object> telescopeValues) {
            object o = null;
            telescopeValues.TryGetValue(nameof(Connected), out o);
            Connected = (bool)(o ?? false);

            telescopeValues.TryGetValue(nameof(AltitudeString), out o);
            AltitudeString = (string)(o ?? string.Empty);

            telescopeValues.TryGetValue(nameof(AzimuthString), out o);
            AzimuthString = (string)(o ?? string.Empty);

            telescopeValues.TryGetValue(nameof(DeclinationString), out o);
            DeclinationString = (string)(o ?? string.Empty);

            telescopeValues.TryGetValue(nameof(RightAscensionString), out o);
            RightAscensionString = (string)(o ?? string.Empty);

            telescopeValues.TryGetValue(nameof(SiderealTimeString), out o);
            SiderealTimeString = (string)(o ?? string.Empty);

            telescopeValues.TryGetValue(nameof(HoursToMeridianString), out o);
            HoursToMeridianString = (string)(o ?? string.Empty);

            telescopeValues.TryGetValue(nameof(AtPark), out o);
            AtPark = (bool)(o ?? false);

            telescopeValues.TryGetValue(nameof(Tracking), out o);
            Tracking = (bool)(o ?? false);
        }

        private async Task GetTelescopeValues(IProgress<Dictionary<string, object>> progress, CancellationToken token) {
            Dictionary<string, object> telescopeValues = new Dictionary<string, object>();
            try {
                do {
                    token.ThrowIfCancellationRequested();

                    var sw = Stopwatch.StartNew();

                    telescopeValues.Clear();
                    telescopeValues.Add(nameof(Connected), _telescope?.Connected ?? false);
                    telescopeValues.Add(nameof(AtPark), _telescope?.AtPark ?? false);
                    telescopeValues.Add(nameof(Tracking), _telescope?.Tracking ?? false);

                    telescopeValues.Add(nameof(AltitudeString), _telescope?.AltitudeString ?? string.Empty);
                    telescopeValues.Add(nameof(AzimuthString), _telescope?.AzimuthString ?? string.Empty);
                    telescopeValues.Add(nameof(DeclinationString), _telescope?.DeclinationString ?? string.Empty);
                    telescopeValues.Add(nameof(RightAscensionString), _telescope?.RightAscensionString ?? string.Empty);
                    telescopeValues.Add(nameof(SiderealTimeString), _telescope?.SiderealTimeString ?? string.Empty);
                    telescopeValues.Add(nameof(HoursToMeridianString), _telescope?.HoursToMeridianString ?? string.Empty);

                    progress.Report(telescopeValues);

                    token.ThrowIfCancellationRequested();
                    await Utility.Utility.Delay(
                        TimeSpan.FromSeconds(
                            Math.Max(0.5, profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval - sw.Elapsed.TotalSeconds)
                        ), token
                    );
                } while (Connected == true);
            } catch (OperationCanceledException) {
            } finally {
                telescopeValues.Clear();
                telescopeValues.Add(nameof(Connected), false);
                progress.Report(telescopeValues);
            }
        }

        private void CancelChooseTelescope(object o) {
            _cancelChooseTelescopeSource?.Cancel();
        }

        private CancellationTokenSource _cancelChooseTelescopeSource;

        private void DisconnectTelescope(object obj) {
            var diag = MyMessageBox.MyMessageBox.Show("Disconnect Telescope?", "", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
            if (diag == System.Windows.MessageBoxResult.OK) {
                Disconnect();
            }
        }

        public void Disconnect() {
            _cancelUpdateTelescopeValues?.Cancel();
            Telescope?.Disconnect();
            Telescope = null;
        }

        private void StepMoveRate(object obj) {
            string cmd = obj.ToString();
            if (cmd == "+") {
                Telescope.MovingRate++;
            } else {
                Telescope.MovingRate--;
            }
        }

        private void Move(object obj) {
            string cmd = obj.ToString();
            if (cmd == "W") {
                Telescope.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, -Telescope.MovingRate);
            }
            if (cmd == "O") {
                Telescope.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, Telescope.MovingRate);
            }
            if (cmd == "N") {
                Telescope.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary, Telescope.MovingRate);
            }
            if (cmd == "S") {
                Telescope.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary, -Telescope.MovingRate);
            }
        }

        private void StopMove(object obj) {
            string cmd = obj.ToString();
            if (cmd == "W") {
                Telescope.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, 0);
            }
            if (cmd == "O") {
                Telescope.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, 0);
            }
            if (cmd == "N") {
                Telescope.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary, 0);
            }
            if (cmd == "S") {
                Telescope.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary, 0);
            }
        }

        private void StopSlew(object obj) {
            Telescope.StopSlew();
        }

        private int _targetDeclinationDegrees;

        public int TargetDeclinationDegrees {
            get {
                return _targetDeclinationDegrees;
            }

            set {
                _targetDeclinationDegrees = value;
                RaisePropertyChanged();
            }
        }

        private int _targetDeclinationMinutes;

        public int TargetDeclinationMinutes {
            get {
                return _targetDeclinationMinutes;
            }

            set {
                _targetDeclinationMinutes = value;
                RaisePropertyChanged();
            }
        }

        private double _targetDeclinationSeconds;

        public double TargetDeclinationSeconds {
            get {
                return _targetDeclinationSeconds;
            }

            set {
                _targetDeclinationSeconds = value;
                RaisePropertyChanged();
            }
        }

        private int _targetRightAscencionHours;

        public int TargetRightAscencionHours {
            get {
                return _targetRightAscencionHours;
            }

            set {
                _targetRightAscencionHours = value;
                RaisePropertyChanged();
            }
        }

        private int _targetRightAscencionMinutes;

        public int TargetRightAscencionMinutes {
            get {
                return _targetRightAscencionMinutes;
            }

            set {
                _targetRightAscencionMinutes = value;
                RaisePropertyChanged();
            }
        }

        private double _targetRightAscencionSeconds;

        public double TargetRightAscencionSeconds {
            get {
                return _targetRightAscencionSeconds;
            }

            set {
                _targetRightAscencionSeconds = value;
                RaisePropertyChanged();
            }
        }

        private async Task<bool> SlewToCoordinatesAsync(Coordinates coords) {
            coords = coords.Transform(profileService.ActiveProfile.AstrometrySettings.EpochType);
            if (Telescope?.Connected == true) {
                await Task.Run(() => {
                    Telescope.SlewToCoordinates(coords.RA, coords.Dec);
                });
                await Utility.Utility.Delay(TimeSpan.FromSeconds(profileService.ActiveProfile.TelescopeSettings.SettleTime), new CancellationToken());
                return true;
            } else {
                return false;
            }
        }

        private void SlewToCoordinates(Coordinates coords) {
            coords = coords.Transform(profileService.ActiveProfile.AstrometrySettings.EpochType);
            if (Telescope?.Connected == true) {
                Telescope.SlewToCoordinatesAsync(coords.RA, coords.Dec);
            }
        }

        private void SlewToCoordinates(object obj) {
            var targetRightAscencion = Utility.Utility.AscomUtil.HMSToHours(TargetRightAscencionHours + ":" + TargetRightAscencionMinutes + ":" + TargetRightAscencionSeconds);
            var targetDeclination = Utility.Utility.AscomUtil.DMSToDegrees(TargetDeclinationDegrees + ":" + TargetDeclinationMinutes + ":" + TargetDeclinationSeconds);

            var coords = new Coordinates(targetRightAscencion, targetDeclination, Epoch.J2000, Coordinates.RAType.Hours);
            SlewToCoordinates(coords);
        }

        public ICommand SlewToCoordinatesCommand { get; private set; }

        public IAsyncCommand ChooseTelescopeCommand { get; private set; }
        public ICommand CancelChooseTelescopeCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }

        public ICommand MoveCommand { get; private set; }

        public ICommand StopMoveCommand { get; private set; }

        public IAsyncCommand ParkCommand { get; private set; }

        public ICommand UnparkCommand { get; private set; }

        public ICommand StopSlewCommand { get; private set; }

        public ICommand StepperMoveRateCommand { get; private set; }

        public ICommand RefreshTelescopeListCommand { get; private set; }
    }

    internal class TelescopeChooserVM : EquipmentChooserVM {

        public TelescopeChooserVM(IProfileService profileService) : base(typeof(TelescopeChooserVM), profileService) {
        }

        public override void GetEquipment() {
            Devices.Clear();

            Devices.Add(new DummyDevice(Locale.Loc.Instance["LblNoTelescope"]));

            var ascomDevices = new ASCOM.Utilities.Profile();

            foreach (ASCOM.Utilities.KeyValuePair device in ascomDevices.RegisteredDevices("Telescope")) {
                try {
                    AscomTelescope cam = new AscomTelescope(device.Key, device.Value, profileService);
                    Devices.Add(cam);
                } catch (Exception) {
                    //only add telescopes which are supported. e.g. x86 drivers will not work in x64
                }
            }

            DetermineSelectedDevice(profileService.ActiveProfile.TelescopeSettings.Id);
        }
    }
}