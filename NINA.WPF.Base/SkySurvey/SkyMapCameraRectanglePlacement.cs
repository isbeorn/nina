#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using NINA.Core.Utility;
using System.ComponentModel;
using System.Windows;

namespace NINA.WPF.Base.SkySurvey {

    public sealed class SkyMapCameraRectanglePlacement : BaseINPC {
        private static readonly PropertyChangedEventArgs HeightChanged = new PropertyChangedEventArgs(nameof(Height));
        private static readonly PropertyChangedEventArgs IdChanged = new PropertyChangedEventArgs(nameof(Id));
        private static readonly PropertyChangedEventArgs InverseRotationChanged = new PropertyChangedEventArgs(nameof(InverseRotation));
        private static readonly PropertyChangedEventArgs RotationChanged = new PropertyChangedEventArgs(nameof(Rotation));
        private static readonly PropertyChangedEventArgs WidthChanged = new PropertyChangedEventArgs(nameof(Width));
        private static readonly PropertyChangedEventArgs XChanged = new PropertyChangedEventArgs(nameof(X));
        private static readonly PropertyChangedEventArgs YChanged = new PropertyChangedEventArgs(nameof(Y));
        private double rotation;
        private double x;
        private double y;

        public SkyMapCameraRectanglePlacement(FramingRectangle rectangle) {
            Rectangle = rectangle;
            Update(rectangle.X, rectangle.Y, rectangle.Rotation);
        }

        public double Height => Rectangle.Height;
        public int Id => Rectangle.Id;
        public double InverseRotation => -Rotation;
        private FramingRectangle Rectangle { get; set; }
        public double Width => Rectangle.Width;

        public double Rotation {
            get => rotation;
            private set {
                if (rotation != value) {
                    rotation = value;
                    OnPropertyChanged(RotationChanged);
                    OnPropertyChanged(InverseRotationChanged);
                }
            }
        }

        public double X {
            get => x;
            private set {
                if (x != value) {
                    x = value;
                    OnPropertyChanged(XChanged);
                }
            }
        }

        public double Y {
            get => y;
            private set {
                if (y != value) {
                    y = value;
                    OnPropertyChanged(YChanged);
                }
            }
        }

        public void SetRectangle(FramingRectangle rectangle) {
            if (ReferenceEquals(Rectangle, rectangle)) {
                return;
            }
            Rectangle = rectangle;
            OnPropertyChanged(WidthChanged);
            OnPropertyChanged(HeightChanged);
            OnPropertyChanged(IdChanged);
        }

        public void Update(SkyMapViewportProjection projection, double positionAngle) {
            if (Rectangle.Coordinates is null) {
                return;
            }
            Point center = projection.Project(Rectangle.Coordinates);
            double projectedRotation = projection.RotationForPositionAngle(
                Rectangle.Coordinates,
                positionAngle,
                center) + 90;
            Update(
                center.X - Rectangle.Width / 2,
                center.Y - Rectangle.Height / 2,
                AstroUtil.EuclidianModulus(projectedRotation, 360));
        }

        public void Update(double x, double y, double rotation) {
            X = x;
            Y = y;
            Rotation = rotation;
        }
    }
}
