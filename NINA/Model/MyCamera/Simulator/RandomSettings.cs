#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility;

namespace NINA.Model.MyCamera.Simulator {

    public class RandomSettings : BaseINPC {

        public RandomSettings() {
            ImageWidth = 640;
            ImageHeight = 480;
            ImageMean = 5000;
            ImageStdDev = 100;
        }

        private object lockObj = new object();

        private int imageWidth;

        public int ImageWidth {
            get => imageWidth;
            set {
                lock (lockObj) {
                    imageWidth = value;
                }
                RaisePropertyChanged();
                //RaisePropertyChanged(nameof(CameraXSize));
            }
        }

        private int imageHeight;

        public int ImageHeight {
            get => imageHeight;
            set {
                lock (lockObj) {
                    imageHeight = value;
                }
                RaisePropertyChanged();
                //RaisePropertyChanged(nameof(CameraYSize));
            }
        }

        private int imageMean;

        public int ImageMean {
            get => imageMean;
            set {
                lock (lockObj) {
                    imageMean = value;
                }
                RaisePropertyChanged();
            }
        }

        private int imageStdDev;

        public int ImageStdDev {
            get => imageStdDev;
            set {
                lock (lockObj) {
                    imageStdDev = value;
                }
                RaisePropertyChanged();
            }
        }
    }
}
