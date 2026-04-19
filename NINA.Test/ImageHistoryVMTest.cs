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
using NINA.Image.ImageAnalysis;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.ViewModel;
using NINA.ViewModel.ImageHistory;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Utility.AutoFocus;
using NINA.Core.Enum;
using NINA.Profile;
using NINA.WPF.Base.Model;
using NINA.Astrometry;

namespace NINA.Test {

    [TestFixture]
    public class ImageHistoryVMTest {
        private Mock<IProfileService> profileServiceMock = new Mock<IProfileService>();
        private Mock<IImageSaveMediator> imageSaveMediatorMock = new Mock<IImageSaveMediator>();

        [SetUp]
        public void SetUp() {
            profileServiceMock = new Mock<IProfileService>();
            imageSaveMediatorMock = new Mock<IImageSaveMediator>();
        }

        [Test]
        public void ImageHistory_ConcurrentId_Order_Test() {
            var profile = CreateProfile();
            profile.ImageHistorySettings.ImageHistoryLeftSelected = ImageHistoryEnum.HFR;
            profile.ImageHistorySettings.ImageHistoryRightSelected = ImageHistoryEnum.Stars;
            profileServiceMock.SetupGet(x => x.ActiveProfile).Returns(profile);

            var sut = new ImageHistoryVM(profileServiceMock.Object, imageSaveMediatorMock.Object);

            for (int i = 1; i < 101; i++) {
                sut.Add(i, null, "LIGHT");
            }

            for (int i = 0; i < 100; i++) {
                sut.ImageHistory[i].Id.Should().Be(i + 1);
            }
        }

        [Test]
        public void ImageHistory_Value_Test() {
            var profile = CreateProfile();
            profile.ImageHistorySettings.ImageHistoryLeftSelected = ImageHistoryEnum.HFR;
            profile.ImageHistorySettings.ImageHistoryRightSelected = ImageHistoryEnum.Stars;
            profile.CameraSettings.PixelSize = 3.76;
            profile.TelescopeSettings.FocalLength = 952;
            profileServiceMock.SetupGet(x => x.ActiveProfile).Returns(profile);
            var sut = new ImageHistoryVM(profileServiceMock.Object, imageSaveMediatorMock.Object);
            var hfr = 10.1234;
            var fwhm = 4.5678;
            var eccentricity = 0.42;
            var stars = 12323;
            var duration = 300;
            var filter = "Red";

            sut.Add(1, null, "LIGHT");
            sut.AppendImageProperties(new ImageSavedEventArgs() { StarDetectionAnalysis = new StarDetectionAnalysis() { DetectedStars = stars, HFR = hfr, FWHM = fwhm, FWHMUnit = StarMeasurementUnit.Pixels, Eccentricity = eccentricity }, Duration = duration, Filter = filter, MetaData = new ImageMetaData { Image = new ImageParameter { Id = 1 } } });

            sut.ObservableImageHistory.First().HFR.Should().Be(hfr);
            sut.ObservableImageHistory.First().FWHM.Should().Be(fwhm);
            sut.ObservableImageHistory.First().Eccentricity.Should().Be(eccentricity);
            sut.ObservableImageHistory.First().HFRUnit.Should().Be(StarMeasurementUnit.Pixels);
            sut.ObservableImageHistory.First().FWHMUnit.Should().Be(StarMeasurementUnit.Pixels);
            sut.ObservableImageHistory.First().FWHMPixels.Should().Be(fwhm);
            sut.ObservableImageHistory.First().FWHMArcseconds.Should().BeApproximately(fwhm * AstroUtil.ArcsecPerPixel(profile.CameraSettings.PixelSize, profile.TelescopeSettings.FocalLength), 0.0001);
            sut.ObservableImageHistory.First().Stars.Should().Be(stars);
            sut.ImageHistory[0].HFR.Should().Be(hfr);
            sut.ImageHistory[0].FWHM.Should().Be(fwhm);
            sut.ImageHistory[0].Eccentricity.Should().Be(eccentricity);
            sut.ImageHistory[0].Stars.Should().Be(stars);
            sut.ImageHistory[0].Duration.Should().Be(duration);
            sut.ImageHistory[0].Filter.Should().Be(filter);
        }

        [Test]
        public void ImageHistory_LimitedStack_FullConcurrency_Test() {
            var profile = CreateProfile();
            profile.ImageHistorySettings.ImageHistoryLeftSelected = ImageHistoryEnum.HFR;
            profile.ImageHistorySettings.ImageHistoryRightSelected = ImageHistoryEnum.Stars;
            profileServiceMock.SetupGet(x => x.ActiveProfile).Returns(profile);
            var sut = new ImageHistoryVM(profileServiceMock.Object, imageSaveMediatorMock.Object);

            for (int i = 0; i < 300; i++) {
                sut.Add(i + 1, null, "LIGHT");
                sut.AppendImageProperties(new ImageSavedEventArgs() { StarDetectionAnalysis = new StarDetectionAnalysis() { DetectedStars = 1, HFR = 1 }, MetaData = new ImageMetaData { Image = new ImageParameter { Id = i + 1 } } });
                sut.AppendAutoFocusPoint(new AutoFocusReport());
            }

            sut.AutoFocusPoints.Select(x => x.Id).Distinct().ToList().Count.Should().BeLessOrEqualTo(300);
            sut.ObservableImageHistory.Count.Should().Be(300);
            sut.ImageHistory.Count.Should().Be(300);
        }

        [Test]
        public void ImageHistory_ClearPlot_Test() {
            var profile = CreateProfile();
            profile.ImageHistorySettings.ImageHistoryLeftSelected = ImageHistoryEnum.HFR;
            profile.ImageHistorySettings.ImageHistoryRightSelected = ImageHistoryEnum.Stars;
            profileServiceMock.SetupGet(x => x.ActiveProfile).Returns(profile);
            var sut = new ImageHistoryVM(profileServiceMock.Object, imageSaveMediatorMock.Object);

            for (int i = 0; i < 100; i++) {
                sut.Add(i + 1, null, "LIGHT");
                sut.AppendImageProperties(new ImageSavedEventArgs() { StarDetectionAnalysis = new StarDetectionAnalysis() { DetectedStars = 1, HFR = 1 }, MetaData = new ImageMetaData { Image = new ImageParameter { Id = i + 1 } } });
                sut.AppendAutoFocusPoint(new AutoFocusReport());
            }

            sut.PlotClear();

            sut.ObservableImageHistory.Count.Should().Be(0);
            sut.AutoFocusPoints.Count.Should().Be(0);
            sut.ImageHistory.Count.Should().Be(0);
        }

        [Test]
        public void ImageHistory_UnitToggle_UpdatesHistoryKeys() {
            var profile = CreateProfile();
            profile.ImageHistorySettings.ImageHistoryLeftSelected = ImageHistoryEnum.HFR;
            profile.ImageHistorySettings.ImageHistoryRightSelected = ImageHistoryEnum.FWHM;
            profile.CameraSettings.PixelSize = 3.76;
            profile.TelescopeSettings.FocalLength = 952;
            profileServiceMock.SetupGet(x => x.ActiveProfile).Returns(profile);

            var sut = new ImageHistoryVM(profileServiceMock.Object, imageSaveMediatorMock.Object);

            sut.ImageHistoryLeftSelectedKey.Should().Be(nameof(ImageHistoryPoint.HFRPixels));
            sut.ImageHistoryRightSelectedKey.Should().Be(nameof(ImageHistoryPoint.FWHMPixels));

            profile.DockPanelSettings.StarMeasurementsInArcseconds = true;

            sut.ImageHistoryLeftSelectedKey.Should().Be(nameof(ImageHistoryPoint.HFRArcseconds));
            sut.ImageHistoryRightSelectedKey.Should().Be(nameof(ImageHistoryPoint.FWHMArcseconds));
        }

        [Test]
        public void ImageHistory_LegacyAnalysisDefaultsToHocusFocusUnits() {
            var profile = CreateProfile();
            profile.ImageHistorySettings.ImageHistoryLeftSelected = ImageHistoryEnum.FWHM;
            profile.ImageHistorySettings.ImageHistoryRightSelected = ImageHistoryEnum.HFR;
            profile.CameraSettings.PixelSize = 3.76;
            profile.TelescopeSettings.FocalLength = 952;
            profileServiceMock.SetupGet(x => x.ActiveProfile).Returns(profile);
            var sut = new ImageHistoryVM(profileServiceMock.Object, imageSaveMediatorMock.Object);
            var arcsecPerPixel = AstroUtil.ArcsecPerPixel(profile.CameraSettings.PixelSize, profile.TelescopeSettings.FocalLength);

            sut.Add(1, null, "LIGHT");
            sut.AppendImageProperties(new ImageSavedEventArgs() {
                StarDetectionAnalysis = new LegacyStarDetectionAnalysis {
                    DetectedStars = 10,
                    HFR = 2.5,
                    FWHM = 4.0
                },
                MetaData = new ImageMetaData { Image = new ImageParameter { Id = 1 } }
            });

            var point = sut.ObservableImageHistory.First();
            point.HFRUnit.Should().Be(StarMeasurementUnit.Pixels);
            point.FWHMUnit.Should().Be(StarMeasurementUnit.Arcseconds);
            point.HFRArcseconds.Should().BeApproximately(2.5 * arcsecPerPixel, 0.0001);
            point.FWHMArcseconds.Should().Be(4.0);
            point.FWHMPixels.Should().BeApproximately(4.0 / arcsecPerPixel, 0.0001);
        }

        [Test]
        public void StarDetectionAnalysis_DefaultsToHocusFocusUnitsUntilDetectorOverridesThem() {
            var sut = new StarDetectionAnalysis();

            sut.HFRUnit.Should().Be(StarMeasurementUnit.Pixels);
            sut.FWHMUnit.Should().Be(StarMeasurementUnit.Arcseconds);
            sut.HFRStDevUnit.Should().Be(StarMeasurementUnit.Pixels);
        }

        [Test]
        public void NativeStarDetection_UpdateAnalysisOverridesFwhmUnitToPixels() {
            var analysis = new StarDetectionAnalysis();
            var starDetection = new StarDetection();

            starDetection.UpdateAnalysis(analysis, new StarDetectionParams(), new StarDetectionResult());

            analysis.HFRUnit.Should().Be(StarMeasurementUnit.Pixels);
            analysis.FWHMUnit.Should().Be(StarMeasurementUnit.Pixels);
            analysis.HFRStDevUnit.Should().Be(StarMeasurementUnit.Pixels);
        }

        [Test]
        public void ImageHistory_EccentricitySelection_UsesPointPropertyKey() {
            var profile = CreateProfile();
            profile.ImageHistorySettings.ImageHistoryLeftSelected = ImageHistoryEnum.Eccentricity;
            profile.ImageHistorySettings.ImageHistoryRightSelected = ImageHistoryEnum.Stars;
            profileServiceMock.SetupGet(x => x.ActiveProfile).Returns(profile);

            var sut = new ImageHistoryVM(profileServiceMock.Object, imageSaveMediatorMock.Object);

            sut.ImageHistoryLeftSelectedKey.Should().Be(nameof(ImageHistoryPoint.Eccentricity));

            sut.ImageHistoryRightSelected = ImageHistoryEnum.Eccentricity;

            sut.ImageHistoryRightSelectedKey.Should().Be(nameof(ImageHistoryPoint.Eccentricity));
        }

        private static NINA.Profile.Profile CreateProfile() {
            return new NINA.Profile.Profile();
        }

        private class LegacyStarDetectionAnalysis : IStarDetectionAnalysis {
            public double HFR { get; set; }
            public double FWHM { get; set; }
            public double Eccentricity { get; set; }
            public double HFRStDev { get; set; }
            public int DetectedStars { get; set; }
            public List<DetectedStar> StarList { get; set; }
            public event PropertyChangedEventHandler PropertyChanged {
                add { }
                remove { }
            }
        }
    }
}
