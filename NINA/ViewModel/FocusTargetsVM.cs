using NINA.Locale;
using NINA.Model;
using NINA.Model.MyTelescope;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Profile;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;

namespace NINA.ViewModel {

    public class FocusTargetsVM : DockableVM, ITelescopeConsumer {
        private ObservableCollection<FocusTarget> focusTargets;
        private FocusTarget selectedFocusTarget;
        private bool telescopeConnected;

        public FocusTargetsVM(IProfileService profileService, ITelescopeMediator telescopeMediator, IApplicationResourceDictionary resourceDictionary) : base(profileService) {
            Title = "LblManualFocusTargets";
            ImageGeometry = (System.Windows.Media.GeometryGroup)resourceDictionary["FocusTargetsSVG"];

            telescopeMediator.RegisterConsumer(this);

            LoadFocusTargets();

            var updateTimer = new DispatcherTimer(TimeSpan.FromMinutes(1), DispatcherPriority.Background, (sender, args) => LoadFocusTargets(), Dispatcher.CurrentDispatcher);
            updateTimer.Start();

            SlewToCoordinatesCommand = new AsyncCommand<bool>(async () => await telescopeMediator.SlewToCoordinatesAsync(SelectedFocusTarget.Coordinates));
        }

        public ILoc Locale { get; set; } = Loc.Instance;

        public bool TelescopeConnected {
            get => telescopeConnected;
            set {
                telescopeConnected = value;
                RaisePropertyChanged();
            }
        }

        public FocusTarget SelectedFocusTarget {
            get => selectedFocusTarget;
            set {
                selectedFocusTarget = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<FocusTarget> FocusTargets {
            get => focusTargets;
            set {
                var selectedBrightStarName = selectedFocusTarget?.Name;
                focusTargets = value;
                RaisePropertyChanged();
                SelectedFocusTarget = value.SingleOrDefault(b => b.Name == selectedBrightStarName) ?? value.First();
            }
        }

        public IAsyncCommand SlewToCoordinatesCommand { get; }

        private async void LoadFocusTargets() {
            var db = new DatabaseInteraction(profileService.ActiveProfile.ApplicationSettings.DatabaseLocation);
            FocusTargets = new ObservableCollection<FocusTarget>(await db.GetBrightStars());
            CalculateTargetAltitude();
        }

        private void CalculateTargetAltitude() {
            var longitude = profileService.ActiveProfile.AstrometrySettings.Longitude;
            var latitude = profileService.ActiveProfile.AstrometrySettings.Latitude;
            foreach (var target in FocusTargets) {
                target.CalculateAltitude(latitude, longitude);
            }

            FocusTargets = new ObservableCollection<FocusTarget>(FocusTargets.Where(b => b.Altitude > 10).OrderByDescending(b => b.Altitude));
        }

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            TelescopeConnected = deviceInfo.Connected;
        }
    }
}