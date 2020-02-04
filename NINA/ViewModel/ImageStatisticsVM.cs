#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Model.ImageData;
using NINA.Profile;
using System;
using System.Threading.Tasks;

namespace NINA.ViewModel {

    public class ImageStatisticsVM : DockableVM {
        private AllImageStatistics _statistics;

        public ImageStatisticsVM(IProfileService profileService) : base(profileService) {
            Title = "LblStatistics";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["HistogramSVG"];
        }

        public AllImageStatistics Statistics {
            get {
                return _statistics;
            }
            set {
                _statistics = value;
                RaisePropertyChanged();
            }
        }

        public async Task UpdateStatistics(IImageData imageData) {
            var exposureTime = imageData.MetaData.Image.ExposureTime;
            var statistics = await AllImageStatistics.Create(imageData);
            statistics.PropertyChanged += Child_PropertyChanged;
            Statistics = statistics;
            if (exposureTime >= 0) {
                var imageStatistics = await imageData.Statistics.Task;
            }
        }

        private void Child_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            this.ChildChanged(sender, e);
        }
    }
}