using NINA.Model;
using NINA.Model.MyTelescope;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Profile;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace NINA.ViewModel {

    public class FocusTargetsVM : DockableVM, ITelescopeConsumer {
        private ObservableCollection<FocusTarget> focusTargets;
        private FocusTarget selectedFocusTarget;
        private bool telescopeConnected;
        private readonly Timer updateTimer;

        public FocusTargetsVM(IProfileService profileService, ITelescopeMediator telescopeMediator, IApplicationResourceDictionary resourceDictionary) : base(profileService) {
            Title = "LblManualFocusTargets";
            ImageGeometry = (System.Windows.Media.GeometryGroup)resourceDictionary["FocusTargetsSVG"];

            telescopeMediator.RegisterConsumer(this);

            new Task(LoadFocusTargets).Start();

            updateTimer = new Timer(TimeSpan.FromMinutes(1).TotalMilliseconds) { AutoReset = true };
            updateTimer.Elapsed += (sender, args) => CalculateVisibleStars();

            SlewToCoordinatesCommand = new AsyncCommand<bool>(async () => await telescopeMediator.SlewToCoordinatesAsync(SelectedFocusTarget.Coordinates));
        }

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

        private List<FocusTarget> allFocusTargets = new List<FocusTarget>();

        public IAsyncCommand SlewToCoordinatesCommand { get; }

        private async void LoadFocusTargets() {
            var db = new DatabaseInteraction(profileService.ActiveProfile.ApplicationSettings.DatabaseLocation);
            allFocusTargets = new List<FocusTarget>(await db.GetBrightStars());
            CalculateVisibleStars();
        }

        private void CalculateVisibleStars() {
            var longitude = profileService.ActiveProfile.AstrometrySettings.Longitude;
            var latitude = profileService.ActiveProfile.AstrometrySettings.Latitude;
            foreach (var target in allFocusTargets) {
                target.CalculateAltAz(latitude, longitude);
            }

            FocusTargets = new ObservableCollection<FocusTarget>(allFocusTargets.Where(b => b.Altitude > 10).OrderByDescending(b => b.Altitude));
        }

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            if (TelescopeConnected != deviceInfo.Connected) {
                TelescopeConnected = deviceInfo.Connected;
            }
        }

        public override void Hide(object o) {
            IsVisible = !IsVisible;
            if (IsVisible) {
                CalculateVisibleStars();
                updateTimer.Start();
            } else {
                updateTimer.Stop();
            }
        }
    }
}