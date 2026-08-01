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
using NINA.Image.ImageAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;
using Pen = System.Drawing.Pen;
using Point = System.Windows.Point;

namespace NINA.Benchmark {

    internal sealed class LegacySkyMapRenderer : IDisposable {
        private const double MaximumDeclination = 89.999;
        private static readonly double[] DeclinationSteps = [0.5, 1, 2, 4, 12, 20];
        private static readonly double[] RightAscensionSteps = [1.25, 2.5, 3.75, 7.5, 15];
        private readonly IReadOnlyList<ConstellationBoundary> boundaries;
        private readonly Bitmap bitmap;
        private readonly IReadOnlyList<Constellation> constellations;
        private readonly IReadOnlyList<DeepSkyObject> deepSkyObjects;
        private readonly Graphics graphics;
        private readonly ViewportFoV viewport;
        private readonly Font constellationFont = new Font("Segoe UI", 11, FontStyle.Bold);
        private readonly Font dsoFont = new Font("Segoe UI", 10, FontStyle.Regular);
        private readonly Font gridFont = new Font("Segoe UI", 7, FontStyle.Italic);
        private readonly Font starFont = new Font("Segoe UI", 8, FontStyle.Italic);
        private readonly SolidBrush constellationBrush = new SolidBrush(Color.FromArgb(128, 255, 255, 153));
        private readonly Pen constellationPen = new Pen(Color.FromArgb(128, 0, 255, 0));
        private readonly SolidBrush dsoFillBrush = new SolidBrush(Color.FromArgb(10, 255, 255, 255));
        private readonly SolidBrush gridBrush = new SolidBrush(Color.SteelBlue);
        private readonly Pen gridPen = new Pen(Color.FromArgb(127, Color.SteelBlue));
        private readonly SolidBrush starBrush = new SolidBrush(Color.FromArgb(200, 255, 255, 255));
        private readonly SolidBrush starLabelBrush = new SolidBrush(Color.FromArgb(128, 255, 215, 0));

        public LegacySkyMapRenderer(
            ViewportFoV viewport,
            IReadOnlyList<Constellation> constellations,
            IReadOnlyList<DeepSkyObject> deepSkyObjects,
            IReadOnlyList<ConstellationBoundary> boundaries) {
            this.viewport = viewport;
            this.constellations = constellations;
            this.deepSkyObjects = deepSkyObjects;
            this.boundaries = boundaries;
            bitmap = new Bitmap((int)viewport.Width, (int)viewport.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        }

        public BitmapSource Render() {
            graphics.Clear(Color.Transparent);
            DrawConstellations();
            DrawDeepSkyObjects();
            DrawBoundaries();
            DrawGrid();
            BitmapSource image = ImageUtility.ConvertBitmap(bitmap, PixelFormats.Bgra32);
            image.Freeze();
            return image;
        }

        private void DrawConstellations() {
            foreach (Constellation constellation in constellations) {
                if (!constellation.Stars.Any(x => viewport.ContainsCoordinates(x.Coords))) {
                    continue;
                }

                foreach (Star star in constellation.Stars) {
                    if (!viewport.ContainsCoordinates(star.Coords)) {
                        continue;
                    }
                    Point center = star.Coords.XYProjection(viewport);
                    float radius = Math.Max(1, (-3.375f * star.Mag + 23.25f) / (float)(viewport.VFoV / 8));
                    graphics.FillEllipse(starBrush, (float)center.X - radius, (float)center.Y - radius, radius * 2, radius * 2);
                    if (!string.IsNullOrWhiteSpace(star.Name)) {
                        SizeF size = graphics.MeasureString(star.Name, starFont);
                        graphics.DrawString(star.Name, starFont, starLabelBrush, (float)center.X + radius - size.Width / 2, (float)center.Y + radius * 2 + 5);
                    }
                }

                foreach (Tuple<Star, Star> connection in constellation.StarConnections) {
                    if (!viewport.ContainsCoordinates(connection.Item1.Coords)
                        && !viewport.ContainsCoordinates(connection.Item2.Coords)) {
                        continue;
                    }
                    Point first = connection.Item1.Coords.XYProjection(viewport);
                    Point second = connection.Item2.Coords.XYProjection(viewport);
                    graphics.DrawLine(constellationPen, (float)first.X, (float)first.Y, (float)second.X, (float)second.Y);
                }

                Coordinates centerCoordinates = ConstellationCenter(constellation);
                Point label = centerCoordinates.XYProjection(viewport);
                SizeF labelSize = graphics.MeasureString(constellation.Name, constellationFont);
                graphics.DrawString(constellation.Name, constellationFont, constellationBrush, (float)label.X - labelSize.Width / 2, (float)label.Y);
            }
        }

        private void DrawDeepSkyObjects() {
            double minimumSize = 3 * Math.Min(viewport.ArcSecWidth, viewport.ArcSecHeight);
            double maximumSize = AstroUtil.DegreeToArcsec(2 * Math.Max(viewport.HFoV, viewport.VFoV));
            foreach (DeepSkyObject dso in deepSkyObjects.Where(x => x.Size > minimumSize && x.Size < maximumSize)) {
                if (!viewport.ContainsCoordinates(dso.Coordinates)) {
                    continue;
                }
                Point center = dso.Coordinates.XYProjection(viewport);
                double radiusX = (dso.Size ?? 30) / viewport.ArcSecWidth / 2;
                double radiusY = (dso.SizeMin ?? dso.Size ?? 30) / viewport.ArcSecHeight / 2;
                (Pen pen, SolidBrush brush) = DsoStyle(dso.DSOType);
                graphics.FillEllipse(dsoFillBrush, (float)(center.X - radiusX), (float)(center.Y - radiusY), (float)(radiusX * 2), (float)(radiusY * 2));
                graphics.DrawEllipse(pen, (float)(center.X - radiusX), (float)(center.Y - radiusY), (float)(radiusX * 2), (float)(radiusY * 2));
                SizeF size = graphics.MeasureString(dso.Name, dsoFont);
                graphics.DrawString(dso.Name, dsoFont, brush, (float)center.X - size.Width / 2, (float)(center.Y + radiusY + 5));
                pen.Dispose();
                brush.Dispose();
            }
        }

        private void DrawBoundaries() {
            using Pen pen = new Pen(Color.FromArgb(128, Color.Khaki), 0.5f);
            foreach (ConstellationBoundary boundary in boundaries) {
                if (!boundary.Boundaries.Any(viewport.ContainsCoordinates)) {
                    continue;
                }
                PointF[] points = boundary.Boundaries.Select(x => {
                    Point point = x.XYProjection(viewport);
                    return new PointF((float)point.X, (float)point.Y);
                }).ToArray();
                if (points.Length > 1) {
                    graphics.DrawPolygon(pen, points);
                }
            }
        }

        private void DrawGrid() {
            double decStep = ClosestStep(DeclinationSteps, viewport.VFoV / 4);
            double raStep = ClosestStep(RightAscensionSteps, viewport.HFoV / 4);
            double sampleStep = Math.Min(decStep, raStep) / 4;
            for (double ra = 0; ra < 360; ra += raStep) {
                if (!viewport.ContainsCoordinates(ra, viewport.CenterCoordinates.Dec)) {
                    continue;
                }
                List<PointF> points = [];
                for (double dec = -MaximumDeclination; dec <= MaximumDeclination; dec += sampleStep) {
                    Coordinates coordinates = CelestialCoordinates(ra, dec);
                    if (viewport.ContainsCoordinates(coordinates) || points.Count > 0) {
                        Point point = coordinates.XYProjection(viewport);
                        points.Add(new PointF((float)point.X, (float)point.Y));
                        if (!viewport.ContainsCoordinates(coordinates) && points.Count > 1) {
                            break;
                        }
                    }
                }
                DrawGridLine(points, $"{AstroUtil.HoursToHMS(AstroUtil.DegreesToHours(ra))[..^3]}h", 1);
            }
            for (double dec = -Math.Floor(MaximumDeclination / decStep) * decStep; dec < 90; dec += decStep) {
                List<PointF> points = [];
                for (double offset = -180; offset <= 180; offset += sampleStep) {
                    Coordinates coordinates = CelestialCoordinates(viewport.CenterCoordinates.RADegrees + offset, dec);
                    if (viewport.ContainsCoordinates(coordinates)) {
                        Point point = coordinates.XYProjection(viewport);
                        points.Add(new PointF((float)point.X, (float)point.Y));
                    }
                }
                DrawGridLine(points, $"{dec:N2}°", dec == 0 ? 3 : 1);
            }
        }

        private void DrawGridLine(List<PointF> points, string label, float width) {
            if (points.Count < 2) {
                return;
            }
            using Pen? pen = width == 1 ? null : new Pen(gridPen.Color, width);
            graphics.DrawLines(pen ?? gridPen, points.ToArray());
            PointF position = points.FirstOrDefault(x => x.X > 0 && x.Y > 0 && x.X < viewport.Width && x.Y < viewport.Height);
            if (position != PointF.Empty) {
                graphics.DrawString(label, gridFont, gridBrush, position);
            }
        }

        private static Coordinates ConstellationCenter(Constellation constellation) {
            double declination = (constellation.Stars.Min(x => x.Coords.Dec) + constellation.Stars.Max(x => x.Coords.Dec)) / 2;
            double rightAscension = (constellation.Stars.Min(x => x.Coords.RADegrees) + constellation.Stars.Max(x => x.Coords.RADegrees)) / 2;
            return CelestialCoordinates(rightAscension, declination);
        }

        private static Coordinates CelestialCoordinates(double rightAscension, double declination) {
            return new Coordinates(AstroUtil.EuclidianModulus(rightAscension, 360), declination, Epoch.J2000, Coordinates.RAType.Degrees);
        }

        private static double ClosestStep(double[] steps, double desired) {
            return steps.MinBy(x => Math.Abs(x - desired));
        }

        private static (Pen Pen, SolidBrush Brush) DsoStyle(string type) {
            Color color = type switch {
                "GALXY" or "GALCL" => Color.BurlyWood,
                "PLNNB" => Color.Cyan,
                "BRTNB" or "CL+NB" => Color.Violet,
                "GLOCL" => Color.Yellow,
                _ => Color.White
            };
            return (new Pen(Color.FromArgb(128, color)), new SolidBrush(color));
        }

        public void Dispose() {
            graphics.Dispose();
            bitmap.Dispose();
            constellationFont.Dispose();
            dsoFont.Dispose();
            gridFont.Dispose();
            starFont.Dispose();
            constellationBrush.Dispose();
            constellationPen.Dispose();
            dsoFillBrush.Dispose();
            gridBrush.Dispose();
            gridPen.Dispose();
            starBrush.Dispose();
            starLabelBrush.Dispose();
        }
    }
}
