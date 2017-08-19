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
            Title = "Image History";
            ContentId = nameof(ImageHistoryVM);
            CanClose = false;
            _nextStatHistoryId = 1;
            ImgStatHistory = new AsyncObservableCollection<ImageStatistics>();
        }

        private int _nextStatHistoryId;
        private AsyncObservableCollection<ImageStatistics> _imgStatHistory;
        public AsyncObservableCollection<ImageStatistics> ImgStatHistory {
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
                if (this.ImgStatHistory.Count > 25) {
                    this.ImgStatHistory.RemoveAt(0);
                }
                stats.Id = _nextStatHistoryId;
                _nextStatHistoryId++;
                this.ImgStatHistory.Add(stats);
            }            
        }
    }
}
