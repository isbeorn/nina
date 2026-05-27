#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Core.Interfaces;
using NINA.Core.Model;
using NINA.Image.ImageData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NINA.Image.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;

namespace NINA.WPF.Base.Interfaces.Mediator {

    public interface IImageSaveMediator : IMediator<IImageSaveController> {

        Task Enqueue(IImageData imageData, Task<IRenderedImage> prepareTask, IProgress<ApplicationStatus> progress, CancellationToken token);
        event Func<object, BeforeImageSavedEventArgs, Task> BeforeImageSaved;
        
        
        /// <summary>
        /// Called before the image is saved to the disk, but also before the image is processed fully (e.g. stretch and star detection)
        /// It is possible to wait for the image processing by awaiting the BeforeFinalizeImageSavedEventArgs.ImagePrepareTask if necessary
        /// Altering Image Meta Data will be reflected in the file.
        /// </summary>
        /// <param name="e"></param>
        event Func<object, BeforeFinalizeImageSavedEventArgs, Task> BeforeFinalizeImageSaved;
        /// <summary>
        /// Called after the image is saved to the disk but before it is moved to the final destination. Here the image is processed fully (e.g. stretch and star detection) and the saved to a temporary place.
        /// Altering Image Meta Data will NOT be reflected in the written file when altered here!
        /// Image Patterns however can be injected here.
        /// </summary>
        /// <param name="e"></param>

        event EventHandler<ImageSavedEventArgs> ImageSaved;
        event Func<object, ImageSaveFailedEventArgs, Task> ImageSaveFailed;


        void Shutdown();
    }

    public enum ImageSaveFailureStage {
        Unknown,
        BeforeImageSaved,
        PrepareImage,
        BeforeFinalizeImageSaved,
        SaveToDisk
    }

    public class BeforeFinalizeImageSavedEventArgs {
        private List<ImagePattern> patterns = new List<ImagePattern>();
        

        public BeforeFinalizeImageSavedEventArgs(IRenderedImage image) {
            this.Image = image;
        }

        public IRenderedImage Image { get; }

        public ReadOnlyCollection<ImagePattern> Patterns => new ReadOnlyCollection<ImagePattern>(patterns);
        public void AddImagePattern(ImagePattern p) {
            patterns.Add(p);
        }
        
    }

    public class BeforeImageSavedEventArgs : EventArgs {
        public BeforeImageSavedEventArgs(IImageData image, Task<IRenderedImage> prepareTask) {
            this.Image = image;
            this.ImagePrepareTask = prepareTask;
        }

        public IImageData Image { get; }
        public Task<IRenderedImage> ImagePrepareTask { get; }
    }

    public class ImageSavedEventArgs : EventArgs {
        public ImageMetaData MetaData { get; set; }
        public BitmapSource Image { get; set; }
        public IImageStatistics Statistics { get; set; }
        public IStarDetectionAnalysis StarDetectionAnalysis { get; set; }
        public Uri PathToImage { get; set; }
        public FileTypeEnum FileType { get; set; }
        public bool IsBayered { get; set; }
        public double Duration { get; set; }
        public string Filter { get; set; }
    }

    public class ImageSaveFailedEventArgs : EventArgs {
        private const int ErrorDiskFull = 0x70;
        private const int ErrorHandleDiskFull = 0x27;

        public ImageSaveFailedEventArgs(IImageData image, string filePath, string filePattern, ImageSaveFailureStage failureStage, Exception exception) {
            Image = image;
            FilePath = filePath;
            FilePattern = filePattern;
            FailureStage = failureStage;
            Exception = exception;
        }

        public IImageData Image { get; }
        public ImageMetaData MetaData => Image?.MetaData;
        public string FilePath { get; }
        public string FilePattern { get; }
        public ImageSaveFailureStage FailureStage { get; }
        public Exception Exception { get; }
        public bool IsDiskFull => IsDiskFullException(Exception);

        private static bool IsDiskFullException(Exception exception) {
            if (exception == null) {
                return false;
            }

            if (exception is IOException ioException) {
                var win32Error = ioException.HResult & 0xffff;
                if (win32Error == ErrorDiskFull || win32Error == ErrorHandleDiskFull) {
                    return true;
                }
            }

            if (exception is AggregateException aggregateException) {
                return aggregateException.InnerExceptions.Any(IsDiskFullException);
            }

            return IsDiskFullException(exception.InnerException);
        }
    }

    public class ImageSaveFailedException : Exception {

        public ImageSaveFailedException(ImageSaveFailedEventArgs failure)
            : base(CreateMessage(failure), failure?.Exception) {
            Failure = failure;
        }

        public ImageSaveFailedEventArgs Failure { get; }

        private static string CreateMessage(ImageSaveFailedEventArgs failure) {
            if (failure == null) {
                return "Image save failed";
            }

            var imageId = failure.MetaData?.Image?.Id;
            var imageInfo = imageId.HasValue ? $" for image {imageId}" : string.Empty;
            var pathInfo = string.IsNullOrWhiteSpace(failure.FilePath) ? string.Empty : $" at {failure.FilePath}";
            return $"Image save failed{imageInfo}{pathInfo}: {failure.Exception?.Message}";
        }
    }
}
