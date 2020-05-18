#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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
