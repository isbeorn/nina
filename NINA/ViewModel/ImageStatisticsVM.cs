using NINA.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Model.MyCamera;

namespace NINA.ViewModel {
    public class ImageStatisticsVM : DockableVM {

        public ImageStatisticsVM() {
            Title = "LblStatistics";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["HistogramSVG"];

            ContentId = nameof(ImageStatisticsVM);
            Statistics = new ImageStatistics { };
        }

        private ImageStatistics _statistics;
        public ImageStatistics Statistics {
            get {
                return _statistics;
            }
            set {
                _statistics = value;
                RaisePropertyChanged();
            }
        }

        public void Add(ImageStatistics stats) {
            Statistics = stats;
        }
    }    
}
