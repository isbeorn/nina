using NINA.Model;
using NINA.Model.MyTelescope;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Profile;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Threading;

namespace NINA.ViewModel {

    public class BrightStarsVM : DockableVM, ITelescopeConsumer {
        private ObservableCollection<BrightStar> brightStars;
        private BrightStar selectedBrightStar;
        private bool telescopeConnected;

        public BrightStarsVM(IProfileService profileService, ITelescopeMediator telescopeMediator) : base(profileService) {
            Title = "Bright Stars";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ContrastSVG"];

            LoadBrightStars();
            profileService.ActiveProfile.AstrometrySettings.PropertyChanged +=
                delegate (object sender, PropertyChangedEventArgs args) {
                    if (args.PropertyName == nameof(profileService.ActiveProfile.AstrometrySettings.Longitude) ||
                        args.PropertyName == nameof(profileService.ActiveProfile.AstrometrySettings.Latitude)) {
                        {
                            CalculateStarAltitude();
                        }
                    }
                };

            var updateTimer = new DispatcherTimer(TimeSpan.FromSeconds(15), DispatcherPriority.Background, (sender, args) => LoadBrightStars(), Dispatcher.CurrentDispatcher);
            updateTimer.Start();

            SlewToCoordinatesCommand = new AsyncCommand<bool>(async () => await telescopeMediator.SlewToCoordinatesAsync(SelectedBrightStar.Coordinates));
        }

        public bool TelescopeConnected {
            get => telescopeConnected;
            set {
                telescopeConnected = value;
                RaisePropertyChanged();
            }
        }

        public BrightStar SelectedBrightStar {
            get => selectedBrightStar;
            set {
                selectedBrightStar = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<BrightStar> BrightStars {
            get => brightStars;
            set {
                var selectedBrightStarName = selectedBrightStar?.Name;
                brightStars = value;
                RaisePropertyChanged();
                SelectedBrightStar = BrightStars.SingleOrDefault(b => b.Name == selectedBrightStarName) ?? BrightStars.First();
            }
        }

        public IAsyncCommand SlewToCoordinatesCommand { get; }

        private async void LoadBrightStars() {
            var db = new DatabaseInteraction(profileService.ActiveProfile.ApplicationSettings.DatabaseLocation);
            BrightStars = new ObservableCollection<BrightStar>(await db.GetBrightStars());
            CalculateStarAltitude();
        }

        private void CalculateStarAltitude() {
            var longitude = profileService.ActiveProfile.AstrometrySettings.Longitude;
            var latitude = profileService.ActiveProfile.AstrometrySettings.Latitude;
            foreach (var star in BrightStars) {
                star.CalculateAltitude(latitude, longitude);
            }

            BrightStars = new ObservableCollection<BrightStar>(BrightStars.Where(b => b.Altitude > 10).OrderByDescending(b => b.Altitude));
        }

        public void UpdateDeviceInfo(TelescopeInfo deviceInfo) {
            TelescopeConnected = deviceInfo.Connected;
        }
    }
}