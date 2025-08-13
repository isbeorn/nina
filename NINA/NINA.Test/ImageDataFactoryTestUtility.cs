#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using Moq;
using NINA.Core.Interfaces;
using NINA.Image.ImageAnalysis;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Image.RawConverter;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Test {
    public class ImageDataFactoryTestUtility {

        public ImageDataFactoryTestUtility() {
            this.ProfileServiceMock = new Mock<IProfileService>();
            this.StarAnnotatorMock = new Mock<IStarAnnotator>();
            this.StarAnnotatorSelectorMock = new Mock<IPluggableBehaviorSelector<IStarAnnotator>>();
            this.StarAnnotatorSelectorMock.Setup(x => x.GetBehavior()).Returns(this.StarAnnotatorMock.Object);
            this.StarDetectionMock = new Mock<IStarDetection>();
            this.StarDetectionMock.Setup(s => s.CreateAnalysis()).Returns(() => new StarDetectionAnalysis());
            this.StarDetectionSelectorMock = new Mock<IPluggableBehaviorSelector<IStarDetection>>();
            this.StarDetectionSelectorMock.Setup(x => x.GetBehavior()).Returns(this.StarDetectionMock.Object);
            this.ImageDataFactory = new ImageDataFactory(this.ProfileServiceMock.Object, this.StarDetectionSelectorMock.Object, this.StarAnnotatorSelectorMock.Object);
            this.ExposureDataFactory = new ExposureDataFactory(this.ImageDataFactory, this.ProfileServiceMock.Object, this.StarDetectionSelectorMock.Object, this.StarAnnotatorSelectorMock.Object);
        }

        public IExposureDataFactory ExposureDataFactory { get; set; }
        public IImageDataFactory ImageDataFactory { get; set; }
        public Mock<IStarAnnotator> StarAnnotatorMock { get; set; }
        public Mock<IStarDetection> StarDetectionMock { get; set; }
        public Mock<IPluggableBehaviorSelector<IStarAnnotator>> StarAnnotatorSelectorMock { get; set; }
        public Mock<IPluggableBehaviorSelector<IStarDetection>> StarDetectionSelectorMock { get; set; }
        public Mock<IProfileService> ProfileServiceMock { get; set; }
    }
}
