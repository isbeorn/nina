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