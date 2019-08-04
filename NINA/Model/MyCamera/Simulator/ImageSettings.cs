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

using NINA.Utility;
using System.IO;
using NINA.Model.ImageData;

namespace NINA.Model.MyCamera.Simulator {

    public class ImageSettings : BaseINPC {
        private object lockObj = new object();
        private bool isBayered;

        public bool IsBayered {
            get => isBayered;
            set {
                isBayered = value;
                RaisePropertyChanged();
            }
        }

        public string RawType { get; set; } = ".cr2";
        private MemoryStream rawImageStream = null;

        public MemoryStream RAWImageStream {
            get => rawImageStream;
            set {
                lock (lockObj) {
                    rawImageStream = value;
                }
                RaisePropertyChanged();
            }
        }

        private IImageData _image;

        public IImageData Image {
            get => _image;
            set {
                lock (lockObj) {
                    _image = value;
                }
                RaisePropertyChanged();
                //RaisePropertyChanged(nameof(CameraXSize));
                //RaisePropertyChanged(nameof(CameraYSize));
            }
        }
    }
}