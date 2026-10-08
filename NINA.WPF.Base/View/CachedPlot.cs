#region "copyright"

/*
    Copyright (c) 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using OxyPlot;
using OxyPlot.Wpf;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Media;

namespace NINA.WPF.Base.View {
    public class CachedPlot : Plot {
        protected override IRenderContext CreateRenderContext() => new CachedRenderContext(Canvas);

        private sealed class CachedRenderContext : CanvasRenderContext {
            private const int MaximumMeasurements = 256;
            private readonly Dictionary<(string Text, string Family, double Size, double Weight,
                TextFormattingMode Formatting, double Dpi, string Culture, string UICulture), OxySize> measurements = new();

            public CachedRenderContext(Canvas canvas) : base(canvas) { }

            public override OxySize MeasureText(string text, string fontFamily, double fontSize, double fontWeight) {
                if (string.IsNullOrEmpty(text) || TextMeasurementMethod != TextMeasurementMethod.TextBlock) {
                    return base.MeasureText(text, fontFamily, fontSize, fontWeight);
                }

                var key = (text, fontFamily, fontSize, fontWeight, TextFormattingMode, DpiScale,
                    CultureInfo.CurrentCulture.Name, CultureInfo.CurrentUICulture.Name);
                if (measurements.TryGetValue(key, out var size)) return size;

                // Keep WPF's shaping and font fallback while reusing repeated axis-label measurements.
                size = base.MeasureText(text, fontFamily, fontSize, fontWeight);
                if (measurements.Count >= MaximumMeasurements) measurements.Clear();
                measurements.Add(key, size);
                return size;
            }

            public override void CleanUp() {
                // OxyPlot calls this after each render; the next pass must observe changed styles/resources.
                measurements.Clear();
                base.CleanUp();
            }
        }
    }
}
