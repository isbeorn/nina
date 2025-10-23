#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Profile.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Image;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.ViewModel;
using NINA.Core.Locale;
using NINA.Image.Interfaces;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NINA.ViewModel {

    internal partial class ThumbnailVM : DockableVM, IThumbnailVM {

        public ThumbnailVM(IProfileService profileService, IImagingMediator imagingMediator, IImageSaveMediator imageSaveMediator, IImageDataFactory imageDataFactory) : base(profileService) {
            Title = Loc.Instance["LblImageHistory"];
            ImageGeometry = (GeometryGroup)System.Windows.Application.Current.Resources["HistorySVG"];
            thumbnails = new ObservableLimitedSizedStack<Thumbnail>(50);

            this.imagingMediator = imagingMediator;
            this.imageSaveMediator = imageSaveMediator;
            this.imageDataFactory = imageDataFactory;

            this.imageSaveMediator.ImageSaved += ImageSaveMediator_ImageSaved;
            this.imagingMediator.ImagePrepared += ImagingMediator_ImagePrepared;

            SelectCommand = new AsyncRelayCommand<Thumbnail>(SelectImage);
            GradeImageCommand = new AsyncRelayCommand<Thumbnail>(GradeImage);
        }

        [ObservableProperty]
        private Thumbnail selectedThumbnail;

        [ObservableProperty]
        private ObservableLimitedSizedStack<Thumbnail> thumbnails;

        private void ImagingMediator_ImagePrepared(object sender, ImagePreparedEventArgs e) {
            // When images that aren't saved are taken, they replace the the image shown. This unselects whatever is currently highlighted.
            // If the file is saved, it'll be captured later in the AddThumbnail callback
            SelectedThumbnail = null;
        }

        private Task<bool> GradeImage(Thumbnail arg) {
            return Task.Run(async () => {
                if (arg is Thumbnail) {
                    var selected = arg as Thumbnail;
                    //Order is from "" -> "BAD" -> ""
                    switch (selected.Grade) {
                        case "": {
                                return await selected.ChangeGrade("BAD");
                            }
                        case "BAD": {
                                return await selected.ChangeGrade("");
                            }
                    }
                    return true;
                }
                return false;
            });
        }

        private void ImageSaveMediator_ImageSaved(object sender, ImageSavedEventArgs e) {
            AddThumbnail(e);
        }

        private Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        private Task<bool> AddThumbnail(ImageSavedEventArgs msg) {
            return Task.Run(async () => {
                if (msg.Image != null) {
                    var factor = 100 / msg.Image.Width;

                    var scaledBitmap = CreateResizedImage(msg.Image, (int)(msg.Image.Width * factor), (int)(msg.Image.Height * factor), 0);
                    scaledBitmap.Freeze();

                    await _dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                        try {
                            var thumbnail = new Thumbnail(imageDataFactory) {
                                ThumbnailImage = scaledBitmap,
                                ImagePath = msg.PathToImage,
                                FileType = msg.FileType,
                                Duration = msg.Duration,
                                ImageStatistics = msg.Statistics,
                                StarDetectionAnalysis = msg.StarDetectionAnalysis,
                                Filter = msg.Filter,
                                IsBayered = msg.IsBayered
                            };
                            Thumbnails.Add(thumbnail);
                            SelectedThumbnail = thumbnail;
                        } catch (Exception ex) {
                            Logger.Error(ex);
                        }
                    }));
                }
                return true;
            });
        }

        private static BitmapFrame CreateResizedImage(ImageSource source, int width, int height, int margin) {
            var rect = new System.Windows.Rect(margin, margin, width - margin * 2, height - margin * 2);

            var group = new DrawingGroup();
            RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.HighQuality);
            group.Children.Add(new ImageDrawing(source, rect));

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
                drawingContext.DrawDrawing(group);

            var resizedImage = new RenderTargetBitmap(
                width, height,         // Resized dimensions
                96, 96,                // Default DPI values
                PixelFormats.Default); // Default pixel format
            resizedImage.Render(drawingVisual);

            var frame = BitmapFrame.Create(resizedImage);
            frame.Freeze();
            return frame;
        }

        private IImagingMediator imagingMediator;
        private IImageSaveMediator imageSaveMediator;
        private readonly IImageDataFactory imageDataFactory;

        public ICommand SelectCommand { get; set; }
        public ICommand GradeImageCommand { get; set; }

        private async Task<bool> SelectImage(Thumbnail thumbnail) {
            var iarr = await thumbnail.LoadOriginalImage(profileService);
            if (iarr != null) {
                iarr.SetImageStatistics(thumbnail.ImageStatistics);
                iarr.StarDetectionAnalysis = thumbnail.StarDetectionAnalysis;

                await imagingMediator.PrepareImage(iarr, new PrepareImageParameters(detectStars: false), CancellationToken.None);
                SelectedThumbnail = thumbnail;
                return true;
            } else {
                return false;
            }
        }
    }
}