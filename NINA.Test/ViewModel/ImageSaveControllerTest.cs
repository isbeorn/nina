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
using NINA.Core.Model;
using NINA.Image.FileFormat;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using NINA.ViewModel;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Test.ViewModel {

    [TestFixture]
    public class ImageSaveControllerTest {

        [Test]
        public async Task SaveFailure_RaisesImageSaveFailedEventAfterRetries() {
            Mock<IProfileService> profileService = new Mock<IProfileService>();
            Mock<IImageSaveMediator> imageSaveMediator = new Mock<IImageSaveMediator>();
            Mock<IApplicationStatusMediator> applicationStatusMediator = new Mock<IApplicationStatusMediator>();
            Mock<IImageData> imageData = new Mock<IImageData>();
            Mock<IRenderedImage> renderedImage = new Mock<IRenderedImage>();
            ImageMetaData metaData = new ImageMetaData { Image = new ImageParameter { Id = 42, ImageType = "LIGHT" } };
            IOException saveException = new IOException("disk full", unchecked((int)0x80070070));
            TaskCompletionSource<ImageSaveFailedEventArgs> failure = new TaskCompletionSource<ImageSaveFailedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            NINA.Profile.Profile profile = new NINA.Profile.Profile();
            profile.ImageFileSettings.FilePath = TestContext.CurrentContext.TestDirectory;

            profileService.SetupGet(x => x.ActiveProfile).Returns(profile);
            imageData.SetupGet(x => x.MetaData).Returns(metaData);
            imageData
                .Setup(x => x.SaveToDisk(It.IsAny<FileSaveInfo>(), It.IsAny<CancellationToken>(), false, It.IsAny<System.Collections.Generic.IList<ImagePattern>>()))
                .ThrowsAsync(saveException);

            ImageSaveController sut = new ImageSaveController(profileService.Object, imageSaveMediator.Object, applicationStatusMediator.Object);
            IImageSaveController saveController = sut;
            saveController.ImageSaveFailed += (sender, args) => {
                failure.TrySetResult(args);
                return Task.CompletedTask;
            };

            try {
                await sut.Enqueue(imageData.Object, Task.FromResult(renderedImage.Object), default, CancellationToken.None);

                ImageSaveFailedEventArgs args = await failure.Task.WaitAsync(TimeSpan.FromSeconds(10));

                args.Image.Should().BeSameAs(imageData.Object);
                args.MetaData.Should().BeSameAs(metaData);
                args.Exception.Should().BeOfType<AggregateException>()
                    .Which.InnerExceptions.Should().OnlyContain(ex => ex == saveException);
                args.FailureStage.Should().Be(ImageSaveFailureStage.SaveToDisk);
                args.IsDiskFull.Should().BeTrue();
                imageData.Verify(
                    x => x.SaveToDisk(It.IsAny<FileSaveInfo>(), It.IsAny<CancellationToken>(), false, It.IsAny<System.Collections.Generic.IList<ImagePattern>>()),
                    Times.Exactly(3));
            } finally {
                sut.Shutdown();
                profile.Dispose();
            }
        }
    }
}
