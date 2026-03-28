#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace NINA.View.Sequencer.Converter {

    public class TreeViewDepthToColorConverter : IMultiValueConverter {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (values.Length < 2) return null;

            Color baseColor = Color.FromRgb(91, 143, 191);

            if (values[0] is SolidColorBrush scb) {
                baseColor = scb.Color;
            }
            Brush[] palette = DistinctColorPalette.Generate(baseColor, 10, minContrastRatio: 3.0);

            if (values[1] is TreeViewItem item) {
                int depth = GetDepth(item);
                return palette[depth % palette.Length];
            }

            return palette[0];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

        private int GetDepth(TreeViewItem item) {
            int depth = 0;
            var parent = ItemsControl.ItemsControlFromItemContainer(item);
            while (parent is TreeViewItem treeViewItem) {
                depth++;
                parent = ItemsControl.ItemsControlFromItemContainer(treeViewItem);
            }
            return depth;
        }
    }

    public static class DistinctColorPalette {

        public static Brush[] Generate(Color baseColor, int count, double minContrastRatio = 3.0) {
            var result = new List<Brush>(count);

            RgbToHsl(baseColor, out double baseHue, out _, out _);
            double baseLuminance = RelativeLuminance(baseColor);

            bool makeLighter = baseLuminance < 0.45;
            double startHue = (baseHue + 180.0) % 360.0;
            const double goldenAngle = 137.50776405003785;

            double minLight = makeLighter ? 0.50 : 0.18;
            double maxLight = makeLighter ? 0.68 : 0.38;
            double startLight = makeLighter ? 0.58 : 0.28;

            for (int i = 0; i < count; i++) {
                double hue = (startHue + i * goldenAngle) % 360.0;

                double sat = Clamp(0.50 + ((i % 2) * 0.08), 0.45, 0.65);

                Color candidate = HslToRgb(hue, sat, startLight);
                candidate = AdjustForContrast(candidate, baseColor, minContrastRatio, makeLighter, minLight, maxLight);

                int safety = 0;
                while (result.Count > 0 &&
                       result.Select(b => ((SolidColorBrush)b).Color).Any(existing => TooSimilar(existing, candidate)) &&
                       safety++ < 12) {
                    hue = (hue + 18.0) % 360.0;
                    candidate = HslToRgb(hue, sat, startLight);
                    candidate = AdjustForContrast(candidate, baseColor, minContrastRatio, makeLighter, minLight, maxLight);
                }

                var brush = new SolidColorBrush(candidate);
                brush.Freeze();
                result.Add(brush);
            }

            return result.ToArray();
        }

        private static bool TooSimilar(Color a, Color b) {
            RgbToHsl(a, out double ha, out _, out double la);
            RgbToHsl(b, out double hb, out _, out double lb);

            double hueDiff = HueDistance(ha, hb);
            double lightDiff = Math.Abs(la - lb);

            return hueDiff < 22.0 && lightDiff < 0.12;
        }

        private static double HueDistance(double a, double b) {
            double d = Math.Abs(a - b) % 360.0;
            return d > 180.0 ? 360.0 - d : d;
        }

        private static Color AdjustForContrast(
            Color candidate,
            Color reference,
            double minContrastRatio,
            bool goLighter,
            double minLight,
            double maxLight) {

            RgbToHsl(candidate, out double h, out double s, out double l);

            for (int i = 0; i < 30; i++) {
                if (ContrastRatio(candidate, reference) >= minContrastRatio) {
                    return candidate;
                }

                l += goLighter ? 0.025 : -0.025;
                l = Clamp(l, minLight, maxLight);
                candidate = HslToRgb(h, s, l);
            }

            return candidate;
        }

        private static double ContrastRatio(Color a, Color b) {
            double l1 = RelativeLuminance(a);
            double l2 = RelativeLuminance(b);

            if (l1 < l2)
                (l1, l2) = (l2, l1);

            return (l1 + 0.05) / (l2 + 0.05);
        }

        private static double RelativeLuminance(Color c) {
            double r = SrgbToLinear(c.R / 255.0);
            double g = SrgbToLinear(c.G / 255.0);
            double b = SrgbToLinear(c.B / 255.0);

            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }

        private static double SrgbToLinear(double x) {
            return x <= 0.04045
                ? x / 12.92
                : Math.Pow((x + 0.055) / 1.055, 2.4);
        }

        private static void RgbToHsl(Color color, out double h, out double s, out double l) {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            l = (max + min) / 2.0;

            if (delta == 0) {
                h = 0;
                s = 0;
                return;
            }

            s = l > 0.5
                ? delta / (2.0 - max - min)
                : delta / (max + min);

            if (max == r) {
                h = 60.0 * (((g - b) / delta) % 6.0);
            } else if (max == g) {
                h = 60.0 * (((b - r) / delta) + 2.0);
            } else {
                h = 60.0 * (((r - g) / delta) + 4.0);
            }
            if (h < 0) {
                h += 360.0;
            }
        }

        private static Color HslToRgb(double h, double s, double l) {
            h = ((h % 360.0) + 360.0) % 360.0;
            s = Clamp(s, 0, 1);
            l = Clamp(l, 0, 1);

            double c = (1 - Math.Abs(2 * l - 1)) * s;
            double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
            double m = l - c / 2.0;

            double r1, g1, b1;

            if (h < 60) {
                (r1, g1, b1) = (c, x, 0);
            } else if (h < 120) {
                (r1, g1, b1) = (x, c, 0);
            } else if (h < 180) {
                (r1, g1, b1) = (0, c, x);
            } else if (h < 240) {
                (r1, g1, b1) = (0, x, c);
            } else if (h < 300) {
                (r1, g1, b1) = (x, 0, c);
            } else {
                (r1, g1, b1) = (c, 0, x);
            }

            byte r = (byte)Math.Round((r1 + m) * 255);
            byte g = (byte)Math.Round((g1 + m) * 255);
            byte b = (byte)Math.Round((b1 + m) * 255);

            return Color.FromRgb(r, g, b);
        }

        private static double Clamp(double value, double min, double max) {
            return value < min ? min : (value > max ? max : value);
        }
    }
}