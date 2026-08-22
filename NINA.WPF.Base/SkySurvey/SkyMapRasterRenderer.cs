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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DrawingBrush = System.Drawing.SolidBrush;
using DrawingBaseBrush = System.Drawing.Brush;
using DrawingColor = System.Drawing.Color;
using DrawingFont = System.Drawing.Font;
using DrawingFontStyle = System.Drawing.FontStyle;
using DrawingPen = System.Drawing.Pen;
using DrawingPixelFormat = System.Drawing.Imaging.PixelFormat;
using DrawingPoint = System.Drawing.PointF;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaPoint = System.Windows.Point;

namespace NINA.WPF.Base.SkySurvey {

    public enum SkyMapRenderQuality {
        Final,
        InteractionPreview
    }

    public sealed class SkyMapRasterRenderer : IDisposable {
        private const int RasterCacheCapacity = 64;
        private const float PreviewScale = 0.5f;
        private static readonly DrawingBrush BoundaryBrush = new DrawingBrush(DrawingColor.FromArgb(128, DrawingColor.Khaki));
        private static readonly DrawingBrush CardinalDirectionBrush = new DrawingBrush(DrawingColor.Red);
        private static readonly DrawingBrush ConstellationBrush = new DrawingBrush(DrawingColor.FromArgb(128, 255, 255, 153));
        private static readonly DrawingBrush GridBrush = new DrawingBrush(DrawingColor.SteelBlue);
        private static readonly DrawingBaseBrush HorizonMaskBrush = new HatchBrush(
            HatchStyle.ForwardDiagonal,
            DrawingColor.FromArgb(255, 58, 42, 22),
            DrawingColor.FromArgb(255, 8, 8, 8));
        private static readonly DrawingBrush StarBrush = new DrawingBrush(DrawingColor.FromArgb(200, 255, 255, 255));
        private static readonly DrawingBrush StarLabelBrush = new DrawingBrush(DrawingColor.FromArgb(128, 255, 215, 0));
        private static readonly DrawingBrush DsoFillBrush = new DrawingBrush(DrawingColor.FromArgb(10, 255, 255, 255));
        private static readonly DrawingPen BoundaryPen = new DrawingPen(BoundaryBrush, 0.5f);
        private static readonly DrawingPen ConstellationPen = new DrawingPen(DrawingColor.FromArgb(128, 0, 255, 0), 1);
        private static readonly DrawingPen GridPen = new DrawingPen(DrawingColor.FromArgb(127, DrawingColor.SteelBlue), 1);
        private static readonly DrawingPen GridEquatorPen = new DrawingPen(DrawingColor.FromArgb(127, DrawingColor.SteelBlue), 3);
        private static readonly DrawingPen HorizonPen = new DrawingPen(DrawingColor.FromArgb(200, DrawingColor.Orange), 2);
        private static readonly DrawingPen TelescopePen = new DrawingPen(DrawingColor.FromArgb(128, DrawingColor.Yellow), 2);
        private static readonly DrawingFont CardinalDirectionFont = new DrawingFont("Segoe UI", 16, DrawingFontStyle.Bold);
        private static readonly DrawingFont ConstellationFont = new DrawingFont("Segoe UI", 11, DrawingFontStyle.Bold);
        private static readonly DrawingFont DsoFont = new DrawingFont("Segoe UI", 10, DrawingFontStyle.Regular);
        private static readonly DrawingFont GridFont = new DrawingFont("Segoe UI", 7, DrawingFontStyle.Italic);
        private static readonly DrawingFont StarFont = new DrawingFont("Segoe UI", 8, DrawingFontStyle.Italic);
        private static readonly ImageAttributes TileImageAttributes = CreateTileImageAttributes();
        private readonly int height;
        private readonly Dictionary<RasterCacheKey, RasterCacheEntry> rasterCache = [];
        private readonly LinkedList<RasterCacheKey> rasterCacheHistory = [];
        private readonly Bitmap previewBitmap;
        private readonly bool softwareComposition;
        private readonly int width;
        private readonly WriteableBitmap writeableBitmap;

        public SkyMapRasterRenderer(int width, int height) {
            this.width = width;
            this.height = height;
            writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
            softwareComposition = RenderOptions.ProcessRenderMode == System.Windows.Interop.RenderMode.SoftwareOnly;
            previewBitmap = softwareComposition
                ? new Bitmap(
                    Math.Max(1, (int)Math.Ceiling(width * PreviewScale)),
                    Math.Max(1, (int)Math.Ceiling(height * PreviewScale)),
                    DrawingPixelFormat.Format32bppPArgb)
                : null;
        }

        public bool UsesSoftwareComposition => softwareComposition;

        public ImageSource Render(
            SkyMapScene scene,
            IReadOnlyList<SkyMapImagePlacement> images,
            MediaPoint? telescopePosition) {
            return Render(scene, images, telescopePosition, SkyMapRenderQuality.Final);
        }

        public ImageSource Render(
            SkyMapScene scene,
            IReadOnlyList<SkyMapImagePlacement> images,
            MediaPoint? telescopePosition,
            SkyMapRenderQuality quality) {
            writeableBitmap.Dispatcher.VerifyAccess();
            if (softwareComposition) {
                return RenderSoftware(scene, images, telescopePosition, quality);
            }

            writeableBitmap.Lock();
            try {
                using Bitmap bitmap = new Bitmap(
                    width,
                    height,
                    writeableBitmap.BackBufferStride,
                    System.Drawing.Imaging.PixelFormat.Format32bppPArgb,
                    writeableBitmap.BackBuffer);
                using Graphics frameGraphics = Graphics.FromImage(bitmap);
                DrawFrame(frameGraphics, scene, [], telescopePosition);
            } finally {
                writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
                writeableBitmap.Unlock();
            }

            return images.Count == 0 ? writeableBitmap : Composite(images, writeableBitmap);
        }

        public void Dispose() {
            previewBitmap?.Dispose();
            foreach (RasterCacheEntry entry in rasterCache.Values) {
                entry.Bitmap.Dispose();
            }
            rasterCache.Clear();
            rasterCacheHistory.Clear();
        }

        internal static Bitmap CreateRasterImage(BitmapSource source) {
            BitmapSource converted = source.Format == PixelFormats.Pbgra32
                ? source
                : new FormatConvertedBitmap(source, PixelFormats.Pbgra32, null, 0);
            Bitmap bitmap = new Bitmap(converted.PixelWidth, converted.PixelHeight, DrawingPixelFormat.Format32bppPArgb);
            BitmapData data = null;
            try {
                try {
                    data = bitmap.LockBits(
                        new Rectangle(System.Drawing.Point.Empty, bitmap.Size),
                        ImageLockMode.WriteOnly,
                        DrawingPixelFormat.Format32bppPArgb);
                    converted.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
                } finally {
                    if (data is not null) {
                        bitmap.UnlockBits(data);
                    }
                }
                return bitmap;
            } catch {
                bitmap.Dispose();
                throw;
            }
        }

        private ImageSource RenderSoftware(
            SkyMapScene scene,
            IReadOnlyList<SkyMapImagePlacement> images,
            MediaPoint? telescopePosition,
            SkyMapRenderQuality quality) {
            if (quality == SkyMapRenderQuality.InteractionPreview) {
                using (Graphics previewGraphics = Graphics.FromImage(previewBitmap)) {
                    previewGraphics.ScaleTransform(PreviewScale, PreviewScale);
                    DrawFrame(previewGraphics, scene, images, telescopePosition);
                }
            }

            writeableBitmap.Lock();
            try {
                using Bitmap bitmap = new Bitmap(
                    width,
                    height,
                    writeableBitmap.BackBufferStride,
                    DrawingPixelFormat.Format32bppPArgb,
                    writeableBitmap.BackBuffer);
                if (quality == SkyMapRenderQuality.InteractionPreview) {
                    using Graphics frameGraphics = Graphics.FromImage(bitmap);
                    frameGraphics.Clear(DrawingColor.Transparent);
                    frameGraphics.CompositingMode = CompositingMode.SourceCopy;
                    frameGraphics.CompositingQuality = CompositingQuality.HighSpeed;
                    frameGraphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                    frameGraphics.PixelOffsetMode = PixelOffsetMode.Half;
                    frameGraphics.DrawImage(
                        previewBitmap,
                        new Rectangle(0, 0, width, height),
                        new Rectangle(0, 0, previewBitmap.Width, previewBitmap.Height),
                        GraphicsUnit.Pixel);
                } else {
                    using Graphics frameGraphics = Graphics.FromImage(bitmap);
                    DrawFrame(frameGraphics, scene, images, telescopePosition);
                }
            } finally {
                writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
                writeableBitmap.Unlock();
            }
            return writeableBitmap;
        }

        private void DrawFrame(
            Graphics graphics,
            SkyMapScene scene,
            IReadOnlyList<SkyMapImagePlacement> images,
            MediaPoint? telescopePosition) {
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            graphics.Clear(DrawingColor.Transparent);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            DrawImages(graphics, images);
            DrawPaths(graphics, scene.ConstellationBoundaries, BoundaryPen, null);
            DrawPaths(graphics, scene.GridLines, GridPen, GridEquatorPen);
            DrawConstellations(graphics, scene);
            DrawDeepSkyObjects(graphics, scene);
            DrawLabels(graphics, scene.Labels);
            if (telescopePosition is MediaPoint telescope) {
                DrawTelescope(graphics, telescope);
            }
            if (scene.HorizonMaskAreas.Count > 0) {
                graphics.SmoothingMode = SmoothingMode.None;
                DrawFilledPaths(graphics, scene.HorizonMaskAreas, HorizonMaskBrush);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
            }
            DrawLines(graphics, scene.HorizonLines, HorizonPen);
            DrawCardinalDirectionLabels(graphics, scene.Labels);
        }

        private void DrawImages(Graphics graphics, IReadOnlyList<SkyMapImagePlacement> images) {
            foreach (SkyMapImagePlacement image in images) {
                double rotation = image.Rotation * Math.PI / 180;
                double cosine = Math.Cos(rotation);
                double sine = Math.Sin(rotation);
                double halfWidth = image.Width / 2;
                double halfHeight = image.Height / 2;
                double transformedHalfWidth = Math.Abs(cosine) * halfWidth + Math.Abs(sine) * halfHeight;
                double transformedHalfHeight = Math.Abs(sine) * halfWidth + Math.Abs(cosine) * halfHeight;
                if (image.Center.X + transformedHalfWidth <= 0
                    || image.Center.X - transformedHalfWidth >= width
                    || image.Center.Y + transformedHalfHeight <= 0
                    || image.Center.Y - transformedHalfHeight >= height) {
                    continue;
                }

                Bitmap rasterImage = image.FlipHorizontally
                    ? GetRasterImage(image.Image, flipHorizontally: true)
                    : image.RasterImage ?? GetRasterImage(image.Image, flipHorizontally: false);
                GraphicsState state = graphics.Save();
                graphics.TranslateTransform((float)image.Center.X, (float)image.Center.Y);
                graphics.RotateTransform((float)image.Rotation);
                graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                graphics.DrawImage(
                    rasterImage,
                    [
                        new DrawingPoint((float)-halfWidth, (float)-halfHeight),
                        new DrawingPoint((float)halfWidth, (float)-halfHeight),
                        new DrawingPoint((float)-halfWidth, (float)halfHeight)
                    ],
                    new RectangleF(0, 0, rasterImage.Width, rasterImage.Height),
                    GraphicsUnit.Pixel,
                    TileImageAttributes);
                graphics.Restore(state);
            }
        }

        private static ImageAttributes CreateTileImageAttributes() {
            ImageAttributes attributes = new ImageAttributes();
            attributes.SetWrapMode(WrapMode.TileFlipXY);
            return attributes;
        }

        private Bitmap GetRasterImage(BitmapSource source, bool flipHorizontally) {
            RasterCacheKey key = new RasterCacheKey(source, flipHorizontally);
            if (rasterCache.TryGetValue(key, out RasterCacheEntry existing)) {
                rasterCacheHistory.Remove(existing.Node);
                rasterCacheHistory.AddFirst(existing.Node);
                return existing.Bitmap;
            }

            Bitmap bitmap = CreateRasterImage(source);
            if (flipHorizontally) {
                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
            }
            LinkedListNode<RasterCacheKey> node = rasterCacheHistory.AddFirst(key);
            rasterCache.Add(key, new RasterCacheEntry(bitmap, node));
            if (rasterCache.Count > RasterCacheCapacity) {
                LinkedListNode<RasterCacheKey> last = rasterCacheHistory.Last;
                RasterCacheEntry removed = rasterCache[last.Value];
                rasterCacheHistory.RemoveLast();
                rasterCache.Remove(last.Value);
                removed.Bitmap.Dispose();
            }
            return bitmap;
        }

        private static void DrawConstellations(Graphics graphics, SkyMapScene scene) {
            DrawLines(graphics, scene.ConstellationLines, ConstellationPen);
            foreach (SkyMapStar star in scene.Stars) {
                float radius = (float)star.Radius;
                graphics.FillEllipse(StarBrush, (float)star.Center.X - radius, (float)star.Center.Y - radius, radius * 2, radius * 2);
            }
        }

        private static void DrawLines(Graphics graphics, IReadOnlyList<SkyMapLine> lines, DrawingPen pen) {
            foreach (SkyMapLine line in lines) {
                graphics.DrawLine(pen, ToDrawingPoint(line.Start), ToDrawingPoint(line.End));
            }
        }

        private static void DrawDeepSkyObjects(Graphics graphics, SkyMapScene scene) {
            foreach (SkyMapDeepSkyObject dso in scene.DeepSkyObjects) {
                (DrawingPen pen, DrawingBrush brush) = DsoStyle(dso.Type);
                GraphicsState state = graphics.Save();
                graphics.TranslateTransform((float)dso.Center.X, (float)dso.Center.Y);
                graphics.RotateTransform((float)dso.PositionAngle);
                graphics.TranslateTransform(-(float)dso.Center.X, -(float)dso.Center.Y);
                RectangleF bounds = new RectangleF(
                    (float)(dso.Center.X - dso.RadiusX),
                    (float)(dso.Center.Y - dso.RadiusY),
                    (float)(dso.RadiusX * 2),
                    (float)(dso.RadiusY * 2));
                graphics.FillEllipse(DsoFillBrush, bounds);
                graphics.DrawEllipse(pen, bounds);
                graphics.Restore(state);

                if (!string.IsNullOrWhiteSpace(dso.Name)) {
                    SizeF size = graphics.MeasureString(dso.Name, DsoFont);
                    graphics.DrawString(
                        dso.Name,
                        DsoFont,
                        brush,
                        (float)dso.Center.X - size.Width / 2,
                        (float)(dso.Center.Y + dso.RadiusY + 5));
                }
            }
        }

        private static void DrawLabels(Graphics graphics, IReadOnlyList<SkyMapLabel> labels) {
            if (labels.Count == 0) {
                return;
            }
            RectangleF visibleBounds = graphics.VisibleClipBounds;
            foreach (SkyMapLabel label in labels) {
                if (label.Kind != SkyMapLabelKind.CardinalDirection) {
                    DrawLabel(graphics, label, visibleBounds);
                }
            }
        }

        private static void DrawCardinalDirectionLabels(Graphics graphics, IReadOnlyList<SkyMapLabel> labels) {
            RectangleF visibleBounds = graphics.VisibleClipBounds;
            foreach (SkyMapLabel label in labels) {
                if (label.Kind == SkyMapLabelKind.CardinalDirection) {
                    DrawLabel(graphics, label, visibleBounds);
                }
            }
        }

        private static void DrawLabel(Graphics graphics, SkyMapLabel label, RectangleF visibleBounds) {
            (DrawingFont font, DrawingBrush brush) = label.Kind switch {
                SkyMapLabelKind.CardinalDirection => (CardinalDirectionFont, CardinalDirectionBrush),
                SkyMapLabelKind.Constellation => (ConstellationFont, ConstellationBrush),
                SkyMapLabelKind.Star => (StarFont, StarLabelBrush),
                _ => (GridFont, GridBrush)
            };
            float x = (float)label.Position.X;
            float y = (float)label.Position.Y;
            SizeF size = graphics.MeasureString(label.Text, font);
            if (label.Kind == SkyMapLabelKind.CardinalDirection) {
                x -= size.Width / 2;
                y -= size.Height / 2;
                x = Math.Clamp(x, visibleBounds.Left, Math.Max(visibleBounds.Left, visibleBounds.Right - size.Width));
                y = Math.Clamp(y, visibleBounds.Top, Math.Max(visibleBounds.Top, visibleBounds.Bottom - size.Height));
            } else if (label.Kind is SkyMapLabelKind.Constellation or SkyMapLabelKind.Star) {
                x -= size.Width / 2;
            } else {
                x = Math.Clamp(x, visibleBounds.Left, Math.Max(visibleBounds.Left, visibleBounds.Right - size.Width));
                y = Math.Clamp(y, visibleBounds.Top, Math.Max(visibleBounds.Top, visibleBounds.Bottom - size.Height));
            }
            graphics.DrawString(label.Text, font, brush, x, y);
        }

        private static void DrawPaths(
            Graphics graphics,
            IReadOnlyList<SkyMapPath> paths,
            DrawingPen pen,
            DrawingPen thickPen) {
            foreach (SkyMapPath path in paths) {
                if (path.Points.Count < 2) {
                    continue;
                }
                DrawingPoint[] points = new DrawingPoint[path.Points.Count];
                for (int i = 0; i < points.Length; i++) {
                    points[i] = ToDrawingPoint(path.Points[i]);
                }
                DrawingPen selectedPen = path.StrokeThickness > pen.Width && thickPen is not null ? thickPen : pen;
                if (path.Closed) {
                    graphics.DrawPolygon(selectedPen, points);
                } else {
                    graphics.DrawLines(selectedPen, points);
                }
            }
        }

        private static void DrawFilledPaths(
            Graphics graphics,
            IReadOnlyList<SkyMapPath> paths,
            DrawingBaseBrush brush) {
            foreach (SkyMapPath path in paths) {
                if (path.Points.Count < 3) {
                    continue;
                }
                DrawingPoint[] points = new DrawingPoint[path.Points.Count];
                for (int i = 0; i < points.Length; i++) {
                    points[i] = ToDrawingPoint(path.Points[i]);
                }
                graphics.FillPolygon(brush, points);
            }
        }

        private static void DrawTelescope(Graphics graphics, MediaPoint position) {
            graphics.DrawEllipse(TelescopePen, (float)position.X - 15, (float)position.Y - 15, 30, 30);
            graphics.DrawLine(TelescopePen, (float)position.X, (float)position.Y - 15, (float)position.X, (float)position.Y - 5);
            graphics.DrawLine(TelescopePen, (float)position.X, (float)position.Y + 5, (float)position.X, (float)position.Y + 15);
            graphics.DrawLine(TelescopePen, (float)position.X - 15, (float)position.Y, (float)position.X - 5, (float)position.Y);
            graphics.DrawLine(TelescopePen, (float)position.X + 5, (float)position.Y, (float)position.X + 15, (float)position.Y);
        }

        private ImageSource Composite(IReadOnlyList<SkyMapImagePlacement> images, BitmapSource overlay) {
            Rect bounds = new Rect(0, 0, width, height);
            DrawingGroup drawing = new DrawingGroup { ClipGeometry = new RectangleGeometry(bounds) };
            using (DrawingContext context = drawing.Open()) {
                context.DrawRectangle(MediaBrushes.Transparent, null, bounds);
                foreach (SkyMapImagePlacement image in images) {
                    double rotation = image.Rotation * Math.PI / 180;
                    double cosine = Math.Cos(rotation);
                    double sine = Math.Sin(rotation);
                    double halfWidth = image.Width / 2;
                    double halfHeight = image.Height / 2;
                    double transformedHalfWidth = Math.Abs(cosine) * halfWidth + Math.Abs(sine) * halfHeight;
                    double transformedHalfHeight = Math.Abs(sine) * halfWidth + Math.Abs(cosine) * halfHeight;
                    if (image.Center.X + transformedHalfWidth <= 0
                        || image.Center.X - transformedHalfWidth >= width
                        || image.Center.Y + transformedHalfHeight <= 0
                        || image.Center.Y - transformedHalfHeight >= height) {
                        continue;
                    }

                    double horizontalScale = image.FlipHorizontally ? -1 : 1;
                    System.Windows.Media.Matrix matrix = new System.Windows.Media.Matrix(
                        horizontalScale * cosine,
                        horizontalScale * sine,
                        -sine,
                        cosine,
                        0,
                        0);
                    matrix.OffsetX = image.Center.X - image.Center.X * matrix.M11 - image.Center.Y * matrix.M21;
                    matrix.OffsetY = image.Center.Y - image.Center.X * matrix.M12 - image.Center.Y * matrix.M22;
                    context.PushTransform(new MatrixTransform(matrix));
                    context.DrawImage(image.Image, new Rect(
                        image.Center.X - halfWidth,
                        image.Center.Y - halfHeight,
                        image.Width,
                        image.Height));
                    context.Pop();
                }
                context.DrawImage(overlay, bounds);
            }
            return new DrawingImage(drawing);
        }

        private static DrawingPoint ToDrawingPoint(MediaPoint point) {
            return new DrawingPoint((float)point.X, (float)point.Y);
        }

        private static (DrawingPen Pen, DrawingBrush Brush) DsoStyle(string type) {
            return type switch {
                "GALXY" or "GALCL" => (DsoResources.GalaxyPen, DsoResources.GalaxyBrush),
                "PLNNB" => (DsoResources.PlanetaryNebulaPen, DsoResources.PlanetaryNebulaBrush),
                "BRTNB" or "CL+NB" => (DsoResources.NebulaPen, DsoResources.NebulaBrush),
                "GLOCL" => (DsoResources.GlobularClusterPen, DsoResources.GlobularClusterBrush),
                _ => (DsoResources.DefaultPen, DsoResources.DefaultBrush)
            };
        }

        private static class DsoResources {
            public static readonly DrawingBrush GalaxyBrush = new DrawingBrush(DrawingColor.BurlyWood);
            public static readonly DrawingBrush PlanetaryNebulaBrush = new DrawingBrush(DrawingColor.Cyan);
            public static readonly DrawingBrush NebulaBrush = new DrawingBrush(DrawingColor.Violet);
            public static readonly DrawingBrush GlobularClusterBrush = new DrawingBrush(DrawingColor.Yellow);
            public static readonly DrawingBrush DefaultBrush = new DrawingBrush(DrawingColor.White);
            public static readonly DrawingPen GalaxyPen = new DrawingPen(DrawingColor.FromArgb(128, DrawingColor.BurlyWood));
            public static readonly DrawingPen PlanetaryNebulaPen = new DrawingPen(DrawingColor.FromArgb(128, DrawingColor.Cyan));
            public static readonly DrawingPen NebulaPen = new DrawingPen(DrawingColor.FromArgb(128, DrawingColor.Violet));
            public static readonly DrawingPen GlobularClusterPen = new DrawingPen(DrawingColor.FromArgb(128, DrawingColor.Yellow));
            public static readonly DrawingPen DefaultPen = new DrawingPen(DrawingColor.FromArgb(127, 255, 255, 255));
        }

        private readonly record struct RasterCacheKey(BitmapSource Image, bool FlipHorizontally);

        private sealed record RasterCacheEntry(Bitmap Bitmap, LinkedListNode<RasterCacheKey> Node);
    }
}
