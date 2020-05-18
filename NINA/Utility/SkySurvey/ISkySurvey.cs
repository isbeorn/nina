#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility.Astrometry;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Utility.SkySurvey {

    internal interface ISkySurvey {

        Task<SkySurveyImage> GetImage(string name, Coordinates coordinates, double fieldOfView, int width, int height,
            CancellationToken ct, IProgress<int> progress);
    }

    internal class SkySurveyImage {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Source { get; set; }
        public BitmapSource Image { get; set; }
        public double FoVWidth { get; set; }
        public double FoVHeight { get; set; }
        public double Rotation { get; set; }
        public Coordinates Coordinates { get; set; }
        public string Name { get; internal set; }
    }
}
