using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.Utility.Profile;

namespace NINA.ViewModel {

    public class ImageHistoryVM : DockableVM {

        public ImageHistoryVM(IProfileService profileService) : base(profileService) {
            Title = "LblHFRHistory";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["HFRHistorySVG"];

            ContentId = nameof(ImageHistoryVM);
            _nextStatHistoryId = 1;
            ImgStatHistory = new AsyncObservableLimitedSizedStack<ImageStatistics>(100);
        }

        private int _nextStatHistoryId;
        private AsyncObservableLimitedSizedStack<ImageStatistics> _imgStatHistory;

        public AsyncObservableLimitedSizedStack<ImageStatistics> ImgStatHistory {
            get {
                return _imgStatHistory;
            }
            set {
                _imgStatHistory = value;
                RaisePropertyChanged();
            }
        }

        public void Add(ImageStatistics stats) {
            if (stats?.DetectedStars > 0 && stats.Id == 0) {
                stats.Id = _nextStatHistoryId;
                _nextStatHistoryId++;
                this.ImgStatHistory.Add(stats);
            }
        }
    }
}