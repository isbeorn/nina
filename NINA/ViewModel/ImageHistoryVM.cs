#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.Profile;
using NINA.Model.ImageData;
using System.Windows.Input;

namespace NINA.ViewModel {

    public class ImageHistoryVM : DockableVM {

        public ImageHistoryVM(IProfileService profileService) : base(profileService) {
            Title = "LblHFRHistory";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["HFRHistorySVG"];

            _nextStatHistoryId = 1;
            ImgStatHistory = new AsyncObservableLimitedSizedStack<AllImageStatistics>(100);

            PlotClearCommand = new RelayCommand((object o) => PlotClear());
        }

        private int _nextStatHistoryId;
        private AsyncObservableLimitedSizedStack<AllImageStatistics> _imgStatHistory;

        public AsyncObservableLimitedSizedStack<AllImageStatistics> ImgStatHistory {
            get {
                return _imgStatHistory;
            }
            set {
                _imgStatHistory = value;
                RaisePropertyChanged();
            }
        }

        public void Add(AllImageStatistics stats) {
            if (stats?.StarDetectionAnalysis?.DetectedStars > 0) {
                if (!this.ImgStatHistory.Contains(stats)) {
                    this.ImgStatHistory.Add(stats);
                }
            }
        }

        public void PlotClear() {
            this.ImgStatHistory.Clear();
        }

        public ICommand PlotClearCommand { get; private set; }
    }
}