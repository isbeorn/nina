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
using NINA.Astrometry;

namespace NINA.Astrometry {

    public class FramingRectangle : ObservableRotatingRectangle {

        public FramingRectangle(double rotationOffset, double x, double y, double width, double height) : base(rotationOffset, x, y, width, height) {
            
            this.OriginalX = x;
            this.OriginalY = y;
        }

        public double OriginalX { get; }
        public double OriginalY { get; }
        public Coordinates OriginalCoordinates { get; set; }

        private int id;

        public int Id {
            get => id;
            set {
                id = value;
                RaisePropertyChanged();
            }
        }

        private string name;
        public string Name {
            get => name;
            set {
                name = value;
                RaisePropertyChanged();
            }
        }

        private Coordinates coordinates;

        public Coordinates Coordinates {
            get => coordinates;
            set {
                coordinates = value;
                RaisePropertyChanged();
            }
        }

        private double dsoPositionAngle;
        public double DSOPositionAngle {
            get => dsoPositionAngle;
            set {
                dsoPositionAngle = AstroUtil.EuclidianModulus(value, 360);
                RaisePropertyChanged();
            }
        }
    }
}