#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.Generic;
using System.Drawing;

namespace NINA.ViewModel.FramingAssistant {

    internal class FramingConstellationBoundary {
        private static Pen boundaryPen = new Pen(Color.FromArgb(128, Color.Khaki), 0.1f);
        public List<PointF> Points = new List<PointF>();

        public void Draw(Graphics g) {
            if (this.Points.Count > 1) {
                g.DrawPolygon(boundaryPen, this.Points.ToArray());
            }
        }
    }
}
