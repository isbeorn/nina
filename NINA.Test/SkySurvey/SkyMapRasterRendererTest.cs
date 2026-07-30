#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.WPF.Base.SkySurvey;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfImage = System.Windows.Controls.Image;

namespace NINA.Test.SkySurvey {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public class SkyMapRasterRendererTest {
        [Test]
        public void RasterRenderer_ReusesWritableViewportSurface() {
            SkyMapScene scene = new SkyMapScene(
                [new SkyMapStar(new Point(100, 100), 3)],
                [new SkyMapLine(new Point(100, 100), new Point(200, 200))],
                [new SkyMapDeepSkyObject("M42", "BRTNB", new Point(300, 300), 20, 15, 30)],
                [new SkyMapPath([new Point(10, 10), new Point(20, 20), new Point(30, 10)])],
                [new SkyMapPath([new Point(0, 400), new Point(1200, 400)])]);
            SkyMapRasterRenderer sut = new SkyMapRasterRenderer(1200, 800);

            ImageSource first = sut.Render(scene, [], null);
            ImageSource second = sut.Render(scene, [], null);

            first.IsFrozen.Should().BeFalse();
            first.Width.Should().Be(1200);
            first.Height.Should().Be(800);
            second.Should().BeSameAs(first);
        }

        [Test]
        public void RasterRenderer_GridLabelAtViewportEdge_RemainsVisible() {
            SkyMapScene scene = new SkyMapScene(
                [],
                [],
                [],
                [],
                [],
                [new SkyMapLabel("05:40h", new Point(99, 99), SkyMapLabelKind.Grid)]);
            SkyMapRasterRenderer sut = new SkyMapRasterRenderer(100, 100);

            BitmapSource result = sut.Render(scene, [], null).Should().BeAssignableTo<BitmapSource>().Subject;
            byte[] pixels = new byte[100 * 100 * 4];
            result.CopyPixels(pixels, 100 * 4, 0);

            pixels.Where((_, index) => index % 4 == 3).Should().Contain(x => x > 0);
        }

        [Test]
        public void RasterRenderer_WithCachedImage_ReturnsFreshCompositeForBindingRefresh() {
            BitmapSource cachedImage = BitmapSource.Create(
                1,
                1,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                new byte[] { 255, 255, 255, 255 },
                4);
            cachedImage.Freeze();
            SkyMapScene scene = new SkyMapScene([], [], [], [], []);
            SkyMapRasterRenderer sut = new SkyMapRasterRenderer(1200, 800);

            SkyMapImagePlacement placement = new SkyMapImagePlacement(cachedImage, new Point(600, 400), 200, 100, 15);
            ImageSource first = sut.Render(
                scene,
                [placement],
                null);
            ImageSource second = sut.Render(
                scene,
                [placement],
                null);

            first.Should().BeOfType<DrawingImage>();
            first.Width.Should().Be(1200);
            first.Height.Should().Be(800);
            second.Should().NotBeSameAs(first);
        }

        [Test]
        public void RasterRenderer_WithHorizontallyFlippedCachedImage_MirrorsImagePixels() {
            BitmapSource asymmetric = BitmapSource.Create(
                2,
                1,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                new byte[] {
                    0, 0, 255, 255,
                    255, 0, 0, 255
                },
                8);
            asymmetric.Freeze();
            SkyMapScene scene = new SkyMapScene([], [], [], [], []);
            SkyMapRasterRenderer renderer = new SkyMapRasterRenderer(100, 100);
            using SkyMapAnnotator annotator = new SkyMapAnnotator();
            WpfImage image = new WpfImage { Width = 100, Height = 100 };
            BindingOperations.SetBinding(
                image,
                WpfImage.SourceProperty,
                new Binding(nameof(SkyMapAnnotator.SkyMapOverlay)) { Source = annotator });

            annotator.SkyMapOverlay = renderer.Render(
                scene,
                [new SkyMapImagePlacement(asymmetric, new Point(50, 50), 40, 20, 0, FlipHorizontally: true)],
                null);
            byte[] frame = Render(image);

            byte[] left = PixelAt(frame, 100, 40, 50);
            byte[] right = PixelAt(frame, 100, 60, 50);
            left[0].Should().BeGreaterThan(left[2]);
            right[2].Should().BeGreaterThan(right[0]);
            left[3].Should().Be(255);
            right[3].Should().Be(255);
        }

        [Test]
        public void RasterRenderer_HorizonMask_HidesCachedImagesBelowHorizon() {
            BitmapSource red = CreatePixel(0, 0, 255);
            SkyMapPath hiddenHalf = new SkyMapPath(
                [new Point(0, 50), new Point(100, 50), new Point(100, 100), new Point(0, 100)],
                closed: true);
            SkyMapScene scene = new SkyMapScene(
                [],
                [],
                [new SkyMapDeepSkyObject("D", "BRTNB", new Point(50, 45), 20, 20, 0)],
                [],
                [],
                [],
                [],
                [hiddenHalf]);
            SkyMapRasterRenderer renderer = new SkyMapRasterRenderer(100, 100);
            using SkyMapAnnotator annotator = new SkyMapAnnotator();
            WpfImage image = new WpfImage { Width = 100, Height = 100 };
            BindingOperations.SetBinding(
                image,
                WpfImage.SourceProperty,
                new Binding(nameof(SkyMapAnnotator.SkyMapOverlay)) { Source = annotator });

            annotator.SkyMapOverlay = renderer.Render(
                scene,
                [new SkyMapImagePlacement(red, new Point(50, 50), 100, 100, 0)],
                null);
            byte[] frame = Render(image);

            PixelAt(frame, 100, 10, 25).Should().Equal(0, 0, 255, 255);
            AssertOpaqueMaskPixel(frame, 100, 10, 75);
            AssertOpaqueMaskPixel(frame, 100, 50, 55);
            AssertOpaqueMaskPixel(frame, 100, 50, 75);
            AssertOpaqueMaskPixel(frame, 100, 90, 75);
        }

        [Test]
        public void RasterRenderer_CardinalDirection_RendersLargeBoldRedLabel() {
            SkyMapScene scene = new SkyMapScene(
                [],
                [],
                [],
                [],
                [],
                [new SkyMapLabel("N", new Point(50, 50), SkyMapLabelKind.CardinalDirection)]);
            SkyMapRasterRenderer renderer = new SkyMapRasterRenderer(100, 100);

            BitmapSource result = renderer.Render(scene, [], null).Should().BeAssignableTo<BitmapSource>().Subject;
            CountRedPixels(result).Should().BeGreaterThan(40);
        }

        [Test]
        public void RasterRenderer_CustomHorizon_KeepsCardinalDirectionVisible() {
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            SkyMapObserverSnapshot observer = new SkyMapObserverSnapshot(50, at, 10, _ => 30);
            Coordinates center = observer.ToCelestial(new SkyMapHorizontalCoordinates(10, 90));
            ViewportFoV viewport = new ViewportFoV(center, 40, 100, 100, 0);
            SkyMapViewportProjection projection = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, observer);
            SkyMapSceneBuilder builder = new SkyMapSceneBuilder([], [], []);
            SkyMapScene scene = builder.Build(
                projection,
                SkyMapRenderOptions.HorizontalGrid | SkyMapRenderOptions.Horizon);
            SkyMapRasterRenderer renderer = new SkyMapRasterRenderer(100, 100);

            BitmapSource result = renderer.Render(scene, [], null).Should().BeAssignableTo<BitmapSource>().Subject;
            CountRedPixels(result).Should().BeGreaterThan(40);
        }

        [Test]
        public void RasterRenderer_FullyBlockedHorizon_UsesOpaquePattern() {
            BitmapSource red = CreatePixel(0, 0, 255);
            SkyMapPath fullMask = new SkyMapPath(
                [new Point(0, 0), new Point(100, 0), new Point(100, 100), new Point(0, 100)],
                closed: true);
            SkyMapScene scene = new SkyMapScene([], [], [], [], [], horizonMaskAreas: [fullMask]);
            SkyMapRasterRenderer renderer = new SkyMapRasterRenderer(100, 100);
            using SkyMapAnnotator annotator = new SkyMapAnnotator();
            WpfImage image = new WpfImage { Width = 100, Height = 100 };
            BindingOperations.SetBinding(
                image,
                WpfImage.SourceProperty,
                new Binding(nameof(SkyMapAnnotator.SkyMapOverlay)) { Source = annotator });

            annotator.SkyMapOverlay = renderer.Render(
                scene,
                [new SkyMapImagePlacement(red, new Point(50, 50), 100, 100, 0)],
                null);
            byte[] frame = Render(image);
            uint[] colors = Enumerable.Range(0, frame.Length / 4)
                .Select(index => BitConverter.ToUInt32(frame, index * 4))
                .Distinct()
                .ToArray();

            colors.Should().HaveCountGreaterThan(1);
            colors.Should().OnlyContain(color => (color & 0xff000000) == 0xff000000);
            colors.Should().NotContain(0xffff0000);
        }

        [Test]
        public void RasterRenderer_ConsecutiveCachedFrames_RenderThroughWpfBinding() {
            BitmapSource red = CreatePixel(0, 0, 255);
            BitmapSource blue = CreatePixel(255, 0, 0);
            SkyMapScene scene = new SkyMapScene([], [], [], [], []);
            SkyMapRasterRenderer renderer = new SkyMapRasterRenderer(100, 100);
            using SkyMapAnnotator annotator = new SkyMapAnnotator();
            WpfImage image = new WpfImage { Width = 100, Height = 100 };
            BindingOperations.SetBinding(
                image,
                WpfImage.SourceProperty,
                new Binding(nameof(SkyMapAnnotator.SkyMapOverlay)) { Source = annotator });

            annotator.SkyMapOverlay = renderer.Render(
                scene,
                [new SkyMapImagePlacement(red, new Point(25, 50), 20, 20, 0)],
                null);
            byte[] firstFrame = Render(image);
            PixelAt(firstFrame, 100, 25, 50).Should().Equal(0, 0, 255, 255);

            ImageSource firstSource = image.Source;
            annotator.SkyMapOverlay = renderer.Render(
                scene,
                [new SkyMapImagePlacement(blue, new Point(75, 50), 20, 20, 0)],
                null);
            byte[] secondFrame = Render(image);

            image.Source.Should().BeSameAs(annotator.SkyMapOverlay);
            image.Source.Should().NotBeSameAs(firstSource);
            PixelAt(secondFrame, 100, 25, 50)[3].Should().Be(0);
            PixelAt(secondFrame, 100, 75, 50).Should().Equal(255, 0, 0, 255);
        }

        [Test]
        public void HorizonPipeline_HidesCachedSkyBelowLocalHorizon() {
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            SkyMapObserverSnapshot observer = new SkyMapObserverSnapshot(50, 10, at);
            Coordinates center = observer.ToCelestial(new SkyMapHorizontalCoordinates(0, 180));
            ViewportFoV viewport = new ViewportFoV(center, 20, 100, 100, 0);
            SkyMapSceneBuilder builder = new SkyMapSceneBuilder([], [], []);
            SkyMapViewportProjection projection = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, observer);
            SkyMapScene scene = builder.Build(projection, SkyMapRenderOptions.Horizon);
            BitmapSource red = CreatePixel(0, 0, 255);
            SkyMapRasterRenderer renderer = new SkyMapRasterRenderer(100, 100);
            using SkyMapAnnotator annotator = new SkyMapAnnotator();
            WpfImage image = new WpfImage { Width = 100, Height = 100 };
            BindingOperations.SetBinding(
                image,
                WpfImage.SourceProperty,
                new Binding(nameof(SkyMapAnnotator.SkyMapOverlay)) { Source = annotator });

            annotator.SkyMapOverlay = renderer.Render(
                scene,
                [new SkyMapImagePlacement(red, new Point(50, 50), 100, 100, 0)],
                null);
            byte[] frame = Render(image);

            PixelAt(frame, 100, 50, 25).Should().Equal(0, 0, 255, 255);
            AssertOpaqueMaskPixel(frame, 100, 10, 75);
            AssertOpaqueMaskPixel(frame, 100, 50, 75);
            AssertOpaqueMaskPixel(frame, 100, 90, 75);
        }

        [TestCase(30, 0, 0)]
        [TestCase(-30, 0, 0)]
        [TestCase(30, 37, 0)]
        [TestCase(-30, 37, 0)]
        [TestCase(30, 37, 15)]
        public void HorizonPipeline_WhenZoomedOut_MasksOnlyCoordinatesBelowHorizon(
            double centerAltitude,
            double rotation,
            double customHorizonAmplitude) {
            DateTime at = new DateTime(2026, 7, 27, 22, 0, 0, DateTimeKind.Utc);
            Func<double, double> customHorizon = customHorizonAmplitude == 0
                ? null
                : azimuth => customHorizonAmplitude * Math.Sin(AstroUtil.ToRadians(azimuth * 2));
            SkyMapObserverSnapshot observer = new SkyMapObserverSnapshot(50, at, 16.5, customHorizon);
            Coordinates center = observer.ToCelestial(new SkyMapHorizontalCoordinates(centerAltitude, 180));
            ViewportFoV viewport = new ViewportFoV(center, 140, 100, 100, rotation);
            SkyMapViewportProjection projection = new SkyMapViewportProjection(viewport, SkyMapProjectionMode.AltAz, observer);
            SkyMapSceneBuilder builder = new SkyMapSceneBuilder([], [], []);
            SkyMapScene scene = builder.Build(projection, SkyMapRenderOptions.Horizon);
            BitmapSource red = CreatePixel(0, 0, 255);
            SkyMapRasterRenderer renderer = new SkyMapRasterRenderer(100, 100);
            using SkyMapAnnotator annotator = new SkyMapAnnotator();
            WpfImage image = new WpfImage { Width = 100, Height = 100 };
            BindingOperations.SetBinding(
                image,
                WpfImage.SourceProperty,
                new Binding(nameof(SkyMapAnnotator.SkyMapOverlay)) { Source = annotator });

            annotator.SkyMapOverlay = renderer.Render(
                scene,
                [new SkyMapImagePlacement(red, new Point(50, 50), 100, 100, 0)],
                null);
            byte[] frame = Render(image);
            int visibleSamples = 0;
            int hiddenSamples = 0;

            foreach (double altitude in new[] { -60d, -30d, -10d, 10d, 30d, 60d }) {
                for (double azimuth = 0; azimuth < 360; azimuth += 15) {
                    Coordinates coordinates = observer.ToCelestial(new SkyMapHorizontalCoordinates(altitude, azimuth));
                    if (!projection.Contains(coordinates)) {
                        continue;
                    }
                    Point projected = projection.Project(coordinates);
                    int x = (int)Math.Round(projected.X);
                    int y = (int)Math.Round(projected.Y);
                    if (x < 2 || x >= 98 || y < 2 || y >= 98) {
                        continue;
                    }

                    double clearance = observer.HorizonClearance(coordinates);
                    if (Math.Abs(clearance) < 8) {
                        continue;
                    }
                    if (clearance >= 0) {
                        PixelAt(frame, 100, x, y).Should().Equal(
                            new byte[] { 0, 0, 255, 255 },
                            $"altitude {altitude}°, azimuth {azimuth}° is above the configured horizon");
                        visibleSamples++;
                    } else {
                        AssertOpaqueMaskPixel(frame, 100, x, y);
                        hiddenSamples++;
                    }
                }
            }

            visibleSamples.Should().BeGreaterThan(0);
            hiddenSamples.Should().BeGreaterThan(0);
        }

        private static BitmapSource CreatePixel(byte blue, byte green, byte red) {
            BitmapSource source = BitmapSource.Create(
                1,
                1,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                new byte[] { blue, green, red, 255 },
                4);
            source.Freeze();
            return source;
        }

        private static byte[] Render(WpfImage image) {
            image.Measure(new Size(100, 100));
            image.Arrange(new Rect(0, 0, 100, 100));
            image.UpdateLayout();
            RenderTargetBitmap bitmap = new RenderTargetBitmap(100, 100, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(image);
            byte[] pixels = new byte[100 * 100 * 4];
            bitmap.CopyPixels(pixels, 100 * 4, 0);
            return pixels;
        }

        private static byte[] PixelAt(byte[] pixels, int width, int x, int y) {
            int offset = (y * width + x) * 4;
            return pixels.Skip(offset).Take(4).ToArray();
        }

        private static int CountRedPixels(BitmapSource source) {
            int stride = source.PixelWidth * 4;
            byte[] pixels = new byte[stride * source.PixelHeight];
            source.CopyPixels(pixels, stride, 0);
            return Enumerable.Range(0, pixels.Length / 4)
                .Count(index => pixels[index * 4 + 2] > 128
                    && pixels[index * 4 + 1] < 64
                    && pixels[index * 4] < 64);
        }

        private static void AssertOpaqueMaskPixel(byte[] pixels, int width, int x, int y) {
            byte[] pixel = PixelAt(pixels, width, x, y);
            pixel[3].Should().Be(255);
            BitConverter.ToUInt32(pixel, 0).Should().NotBe(0xffff0000);
        }


        private static Coordinates CelestialCoordinates(double rightAscension, double declination) {
            return new Coordinates(rightAscension, declination, Epoch.J2000, Coordinates.RAType.Degrees);
        }
    }
}


