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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NINACustomControlLibrary {

    public sealed class Arc : Shape {

        public Point Center {
            get => (Point)GetValue(CenterProperty);
            set => SetValue(CenterProperty, value);
        }

        // Using a DependencyProperty as the backing store for Center.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CenterProperty =
            DependencyProperty.Register(nameof(Center), typeof(Point), typeof(Arc),
                new FrameworkPropertyMetadata(new Point(), FrameworkPropertyMetadataOptions.AffectsRender));

        public double StartAngle {
            get => (double)GetValue(StartAngleProperty);
            set => SetValue(StartAngleProperty, value);
        }

        // Using a DependencyProperty as the backing store for StartAngle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StartAngleProperty =
            DependencyProperty.Register(nameof(StartAngle), typeof(double), typeof(Arc),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double EndAngle {
            get => (double)GetValue(EndAngleProperty);
            set => SetValue(EndAngleProperty, value);
        }

        // Using a DependencyProperty as the backing store for EndAngle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EndAngleProperty =
            DependencyProperty.Register(nameof(EndAngle), typeof(double), typeof(Arc),
                new FrameworkPropertyMetadata(Math.PI / 2.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double Radius {
            get => (double)GetValue(RadiusProperty);
            set => SetValue(RadiusProperty, value);
        }

        // Using a DependencyProperty as the backing store for Radius.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RadiusProperty =
            DependencyProperty.Register(nameof(Radius), typeof(double), typeof(Arc),
                new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool SmallAngle {
            get => (bool)GetValue(SmallAngleProperty);
            set => SetValue(SmallAngleProperty, value);
        }

        // Using a DependencyProperty as the backing store for SmallAngle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SmallAngleProperty =
            DependencyProperty.Register(nameof(SmallAngle), typeof(bool), typeof(Arc),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        static Arc() => DefaultStyleKeyProperty.OverrideMetadata(typeof(Arc), new FrameworkPropertyMetadata(typeof(Arc)));

        protected override Geometry DefiningGeometry {
            get {
                var radStart = StartAngle * (Math.PI / 180);
                var radEnd = EndAngle * (Math.PI / 180);
                double a0 = radStart < 0 ? radStart + 2 * Math.PI : radStart;
                double a1 = radEnd < 0 ? radEnd + 2 * Math.PI : radEnd;

                if (a1 < a0)
                    a1 += Math.PI * 2;

                SweepDirection d = SweepDirection.Counterclockwise;
                bool large;

                if (SmallAngle) {
                    large = false;
                    d = (a1 - a0) > Math.PI ? SweepDirection.Counterclockwise : SweepDirection.Clockwise;
                } else
                    large = (Math.Abs(a1 - a0) < Math.PI);

                Point p0 = Center + new Vector(Math.Cos(a0), Math.Sin(a0)) * Radius;
                Point p1 = Center + new Vector(Math.Cos(a1), Math.Sin(a1)) * Radius;

                List<PathSegment> segments = new List<PathSegment>
                {
                new ArcSegment(p1, new Size(Radius, Radius), 0.0, large, d, true)
            };

                List<PathFigure> figures = new List<PathFigure>
                {
                new PathFigure(p0, segments, false)
                {
                    IsClosed = false
                }
            };

                return new PathGeometry(figures, FillRule.EvenOdd, null);
            }
        }
    }
}