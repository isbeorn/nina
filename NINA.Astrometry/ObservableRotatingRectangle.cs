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
using System;

namespace NINA.Astrometry {

    public class ObservableRotatingRectangle : ObservableRectangle {

        public ObservableRotatingRectangle(double x, double y, double width, double height) : base(x, y, width, height) {
        }

        public ObservableRotatingRectangle(double rotationOffset, double x, double y, double width, double height) : this(x,y,width,height) {
            _rotationOffset = rotationOffset;
            OriginalOffset = _rotationOffset;
        }

        public double OriginalOffset { get; }

        private double _rotationOffset;
        public double RotationOffset {
            get => _rotationOffset;
            set => _rotationOffset = AstroUtil.MathMod(value, 360);
        }

        private double _rotation;

        public double Rotation {
            get => _rotation;
            set {
                _rotation = AstroUtil.MathMod(value, 360);
                if (_rotation < 0) { _rotation += 360; }

                RaisePropertyChanged();
                RaisePropertyChanged(nameof(TotalRotation));
            }
        }


        public double TotalRotation {
            get {
                var rotation = _rotationOffset + Rotation;
                rotation = AstroUtil.MathMod(rotation, 360);
                return Math.Round(rotation, 2);
            }
            set =>
                //This will rise property changed for TotalRotation
                Rotation = AstroUtil.MathMod(value - _rotationOffset, 360);
        }
    }
}