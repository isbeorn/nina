#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Image.Interfaces;
using System.Threading.Tasks;

namespace NINA.Image.ImageData {

    public class AllImageStatistics : BaseINPC {
        public ImageProperties ImageProperties { get; private set; }
        public Task<IImageStatistics> ImageStatistics { get; private set; }
        public IStarDetectionAnalysis StarDetectionAnalysis { get; private set; }

        private AllImageStatistics(
            ImageProperties imageProperties,
            Task<IImageStatistics> imageStatistics,
            IStarDetectionAnalysis starDetectionAnalysis) {
            this.ImageProperties = imageProperties;
            this.ImageStatistics = imageStatistics;
            this.StarDetectionAnalysis = starDetectionAnalysis;

            this.StarDetectionAnalysis.PropertyChanged += Child_PropertyChanged;
        }

        private void Child_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            this.ChildChanged(sender, e);
        }

        public static AllImageStatistics Create(IImageData imageData) {
            return new AllImageStatistics(imageData.Properties, imageData.Statistics.Task, imageData.StarDetectionAnalysis);
        }
    }
}