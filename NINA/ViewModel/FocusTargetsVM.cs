using NINA.Model;
using NINA.Model.MyTelescope;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Profile;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using NINA.Database;
using Nito.AsyncEx;

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

            AsyncContext.Run(LoadFocusTargets);

            updateTimer = new Timer(TimeSpan.FromMinutes(1).TotalMilliseconds) { AutoReset = true };
            updateTimer.Elapsed += (sender, args) => CalculateVisibleStars();
            if (IsVisible) {
                updateTimer.Start();
            }

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
                focusTargets = value;
                RaisePropertyChanged();
            }
        }

        private List<FocusTarget> allFocusTargets = new List<FocusTarget>();

        public IAsyncCommand SlewToCoordinatesCommand { get; }

        private async Task LoadFocusTargets() {
            try {
                var db = new DatabaseInteraction();
                allFocusTargets = await db.GetBrightStars();
                CalculateVisibleStars();
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        private void CalculateVisibleStars() {
            var selectedBrightStarName = selectedFocusTarget?.Name;
            var longitude = profileService.ActiveProfile.AstrometrySettings.Longitude;
            var latitude = profileService.ActiveProfile.AstrometrySettings.Latitude;
            foreach (var target in allFocusTargets) {
                target.CalculateAltAz(latitude, longitude);
            }

            FocusTargets = new ObservableCollection<FocusTarget>(allFocusTargets.Where(b => b.Altitude > 10).OrderByDescending(b => b.Altitude));
            SelectedFocusTarget = FocusTargets.SingleOrDefault(b => b.Name == selectedBrightStarName) ?? FocusTargets.FirstOrDefault();
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