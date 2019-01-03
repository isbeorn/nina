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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NINA.View {

    /// <summary>
    /// Interaction logic for ManualRotatorView.xaml
    /// </summary>
    public partial class ManualRotatorView : UserControl {

        public ManualRotatorView() {
            InitializeComponent();
        }

        private void PositionLineTransform_Changed(object sender, EventArgs e) {
            /*var transform = (RotateTransform)sender;
            var PositionLineP1 = transform.Transform(new Point(PositionLine.X1, PositionLine.Y1));
            var PositionLineP2 = transform.Transform(new Point(PositionLine.X2, PositionLine.Y2));
            PathFigure.StartPoint = PositionLineP2;*/
        }

        private void TargetPositionLineTransform_Changed(object sender, EventArgs e) {
            /*var transform = (RotateTransform)sender;
            var TargetPositionLineP1 = transform.Transform(new Point(TargetPositionLine.X1, TargetPositionLine.Y1));
            var TargetPositionLineP2 = transform.Transform(new Point(TargetPositionLine.X2, TargetPositionLine.Y2));
            ArcSegment.Point = TargetPositionLineP2;*/
        }

        private void root_SizeChanged(object sender, SizeChangedEventArgs e) {
            var geometry = new PathGeometry();

            var position = (float)this.DataContext.GetType().GetProperty("Position").GetValue(this.DataContext, null);

            var targetposition = (float)this.DataContext.GetType().GetProperty("TargetPosition").GetValue(this.DataContext, null);

            var figure = new PathFigure();
            figure.StartPoint = new Point(50, 50);

            var segment1 = new LineSegment(new Point(50, 0), true);

            var arc = new ArcSegment();
            arc.Point = new Point(100, 50);
            arc.Size = new Size(100, 100);
            arc.SweepDirection = SweepDirection.Clockwise;

            var segment2 = new LineSegment(new Point(50, 50), true);

            figure.Segments.Add(segment1);
            figure.Segments.Add(arc);
            figure.Segments.Add(segment2);

            geometry.Figures.Add(figure);

            //SlizePath.Data = geometry;

            //p.Segments.Add
            /*var root = (Grid)sender;
            var halfWidth = root.ActualWidth / 2d;
            var halfHeight = root.ActualHeight / 2d;
            PositionLine.X1 = halfWidth;
            PositionLine.Y1 = 0;
            PositionLine.X2 = halfWidth;
            PositionLine.Y2 = halfHeight;
            PositionLine.RenderTransformOrigin = new Point(halfWidth, halfHeight);
            TargetPositionLine.X1 = halfWidth;
            TargetPositionLine.Y1 = 0;
            TargetPositionLine.X2 = halfWidth;
            TargetPositionLine.Y2 = halfHeight;
            TargetPositionLine.RenderTransformOrigin = new Point(halfWidth, halfHeight);*/
        }
    }
}