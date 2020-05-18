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

        private IRenderedImage _image;

        public IRenderedImage Image {
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

        private ImageMetaData _metaData = null;

        public ImageMetaData MetaData {
            get => _metaData;
            set {
                lock (lockObj) {
                    _metaData = value;
                }
                RaisePropertyChanged();
            }
        }
    }
}
