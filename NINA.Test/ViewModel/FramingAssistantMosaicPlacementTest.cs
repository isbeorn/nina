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
using Moq;
using NINA.Astrometry;
using NINA.ViewModel.FramingAssistant;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.SkySurvey;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace NINA.Test.ViewModel {

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public class FramingAssistantMosaicPlacementTest {

        [Test]
        public void NonOfflineThreeByOneMosaic_RotatesPanelCentersAroundMosaicCenter() {
            FramingAssistantVM sut = CreateUninitializedViewModel();
            sut.Rectangle = new FramingRectangle(0, 120, 200, 560, 200) { Rotation = 90 };
            sut.CameraRectangles.Add(new FramingRectangle(0, 0, 0, 200, 200) { Id = 1 });
            sut.CameraRectangles.Add(new FramingRectangle(0, 180, 0, 200, 200) { Id = 2 });
            sut.CameraRectangles.Add(new FramingRectangle(0, 360, 0, 200, 200) { Id = 3 });

            UpdateCameraRectanglePlacements(sut);

            AssertPlacement(sut.ProjectedCameraRectangles[0], sut.CameraRectangles[0], 400, 120, 90);
            AssertPlacement(sut.ProjectedCameraRectangles[1], sut.CameraRectangles[1], 400, 300, 90);
            AssertPlacement(sut.ProjectedCameraRectangles[2], sut.CameraRectangles[2], 400, 480, 90);
        }

        [TestCase(0, true, 0, 0)]
        [TestCase(37, true, 75, -45)]
        [TestCase(90, true, -260, 110)]
        [TestCase(180, true, 40, 60)]
        [TestCase(270, true, -75, -125)]
        [TestCase(323, true, 115, 30)]
        [TestCase(359, true, -1, 1)]
        [TestCase(0, false, 0, 0)]
        [TestCase(37, false, 75, -45)]
        [TestCase(90, false, -260, 110)]
        [TestCase(180, false, 40, 60)]
        [TestCase(270, false, -75, -125)]
        [TestCase(323, false, 115, 30)]
        [TestCase(359, false, -1, 1)]
        public void NonOfflineThreePanelMosaic_PreservesRotatedPanelLayout(
            double rotation,
            bool horizontal,
            double translationX,
            double translationY) {
            double rectangleX = (horizontal ? 120 : 300) + translationX;
            double rectangleY = (horizontal ? 200 : 20) + translationY;
            double rectangleWidth = horizontal ? 560 : 200;
            double rectangleHeight = horizontal ? 200 : 560;
            FramingAssistantVM sut = CreateUninitializedViewModel();
            sut.Rectangle = new FramingRectangle(
                0,
                rectangleX,
                rectangleY,
                rectangleWidth,
                rectangleHeight) {
                Rotation = rotation
            };
            IReadOnlyList<FramingRectangle> panels = CreatePanels(horizontal);
            foreach (FramingRectangle panel in panels) {
                sut.CameraRectangles.Add(panel);
            }

            UpdateCameraRectanglePlacements(sut);

            for (int i = 0; i < panels.Count; i++) {
                FramingRectangle panel = panels[i];
                (double centerX, double centerY) = RotatePanelCenter(
                    sut.Rectangle,
                    panel);
                AssertPlacement(
                    sut.ProjectedCameraRectangles[i],
                    panel,
                    centerX,
                    centerY,
                    sut.Rectangle.Rotation + panel.Rotation);
            }
        }

        [TestCase(3, 3, 37, 115, -45)]
        [TestCase(2, 3, 90, -260, 110)]
        [TestCase(2, 2, 323, -75, -125)]
        public void NonOfflineMultiRowMosaic_ProjectsEveryPanelAtCorrectCoordinates(
            int horizontalPanels,
            int verticalPanels,
            double rotation,
            double translationX,
            double translationY) {
            const double panelWidth = 200;
            const double panelHeight = 120;
            const double horizontalStride = 180;
            const double verticalStride = 105;
            double rectangleWidth = panelWidth + (horizontalPanels - 1) * horizontalStride;
            double rectangleHeight = panelHeight + (verticalPanels - 1) * verticalStride;
            FramingAssistantVM sut = CreateUninitializedViewModel();
            sut.Rectangle = new FramingRectangle(
                0,
                300 + translationX,
                200 + translationY,
                rectangleWidth,
                rectangleHeight) {
                Rotation = rotation
            };
            IReadOnlyList<FramingRectangle> panels = CreatePanels(
                horizontalPanels,
                verticalPanels,
                panelWidth,
                panelHeight,
                horizontalStride,
                verticalStride);
            foreach (FramingRectangle panel in panels) {
                sut.CameraRectangles.Add(panel);
            }

            UpdateCameraRectanglePlacements(sut);

            sut.ProjectedCameraRectangles.Should().HaveCount(panels.Count);
            for (int i = 0; i < panels.Count; i++) {
                FramingRectangle panel = panels[i];
                (double centerX, double centerY) = RotatePanelCenter(sut.Rectangle, panel);
                AssertPlacement(
                    sut.ProjectedCameraRectangles[i],
                    panel,
                    centerX,
                    centerY,
                    sut.Rectangle.Rotation + panel.Rotation);
            }
        }

        private static IReadOnlyList<FramingRectangle> CreatePanels(bool horizontal) {
            return [
                new FramingRectangle(0, 0, 0, 200, 200) { Id = 1, Rotation = 2 },
                new FramingRectangle(0, horizontal ? 180 : 0, horizontal ? 0 : 180, 200, 200) { Id = 2 },
                new FramingRectangle(0, horizontal ? 360 : 0, horizontal ? 0 : 360, 200, 200) { Id = 3, Rotation = 358 }
            ];
        }

        private static IReadOnlyList<FramingRectangle> CreatePanels(
            int horizontalPanels,
            int verticalPanels,
            double panelWidth,
            double panelHeight,
            double horizontalStride,
            double verticalStride) {
            List<FramingRectangle> panels = [];
            int id = 1;
            for (int row = 0; row < verticalPanels; row++) {
                for (int column = 0; column < horizontalPanels; column++) {
                    panels.Add(new FramingRectangle(
                        0,
                        column * horizontalStride,
                        row * verticalStride,
                        panelWidth,
                        panelHeight) {
                        Id = id,
                        Rotation = id * 2
                    });
                    id++;
                }
            }
            return panels;
        }

        private static (double X, double Y) RotatePanelCenter(
            FramingRectangle rectangle,
            FramingRectangle panel) {
            double radians = AstroUtil.ToRadians(rectangle.Rotation);
            double cosine = Math.Cos(radians);
            double sine = Math.Sin(radians);
            double mosaicCenterX = rectangle.Width / 2;
            double mosaicCenterY = rectangle.Height / 2;
            double deltaX = panel.X + panel.Width / 2 - mosaicCenterX;
            double deltaY = panel.Y + panel.Height / 2 - mosaicCenterY;
            return (
                rectangle.X + mosaicCenterX + deltaX * cosine - deltaY * sine,
                rectangle.Y + mosaicCenterY + deltaX * sine + deltaY * cosine);
        }

        private static FramingAssistantVM CreateUninitializedViewModel() {
            FramingAssistantVM sut = (FramingAssistantVM)RuntimeHelpers.GetUninitializedObject(typeof(FramingAssistantVM));
            FieldInfo skyMapAnnotator = typeof(FramingAssistantVM).GetField(
                "skyMapAnnotator",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            skyMapAnnotator.SetValue(sut, Mock.Of<ISkyMapAnnotator>());
            return sut;
        }

        private static void UpdateCameraRectanglePlacements(FramingAssistantVM sut) {
            MethodInfo updatePlacements = typeof(FramingAssistantVM).GetMethod(
                "UpdateCameraRectanglePlacements",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            updatePlacements.Invoke(sut, null);
        }

        private static void AssertPlacement(
            SkyMapCameraRectanglePlacement placement,
            FramingRectangle panel,
            double expectedCenterX,
            double expectedCenterY,
            double expectedRotation) {
            placement.Id.Should().Be(panel.Id);
            placement.Width.Should().BeApproximately(panel.Width, 1E-9);
            placement.Height.Should().BeApproximately(panel.Height, 1E-9);
            placement.X.Should().BeApproximately(expectedCenterX - panel.Width / 2, 1E-9);
            placement.Y.Should().BeApproximately(expectedCenterY - panel.Height / 2, 1E-9);
            placement.Rotation.Should().BeApproximately(expectedRotation, 1E-9);
        }
    }
}
