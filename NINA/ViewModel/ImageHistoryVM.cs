using NINA.Model.MyCamera;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.ViewModel {
    public class ImageHistoryVM :DockableVM {

        public ImageHistoryVM() {
            Title = Locale.Loc.Instance["LblImageHistory"];
            ContentId = nameof(ImageHistoryVM);
            CanClose = false;
            _nextStatHistoryId = 1;
            ImgStatHistory = new AsyncObservableLimitedSizedStack<ImageStatistics>(25);
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
            if(stats?.DetectedStars > 0 && stats.Id == 0) {
                stats.Id = _nextStatHistoryId;
                _nextStatHistoryId++;
                this.ImgStatHistory.Add(stats);
            }            
        }
    }
}
