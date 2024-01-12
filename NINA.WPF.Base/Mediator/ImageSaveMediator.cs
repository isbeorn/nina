#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Model;
using NINA.Image.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.Core.Locale;
using NINA.Image.FileFormat;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Core.Utility.Extensions;

namespace NINA.WPF.Base.Mediator {

    public class ImageSaveMediator : IImageSaveMediator {
        protected IImageSaveController handler;

        public Task Enqueue(IImageData imageData, Task<IRenderedImage> prepareTask, IProgress<ApplicationStatus> progress, CancellationToken token) {
            return handler.Enqueue(imageData, prepareTask, progress, token);
        }

        public void RegisterHandler(IImageSaveController handler) {
            if (this.handler != null) {
                throw new Exception("Handler already registered!");
            }
            this.handler = handler;
        }

        public event Func<object, BeforeImageSavedEventArgs, Task> BeforeImageSaved {
            add { this.handler.BeforeImageSaved += value; }
            remove { this.handler.BeforeImageSaved -= value; }
        }

        public event Func<object, BeforeFinalizeImageSavedEventArgs, Task> BeforeFinalizeImageSaved {
            add { this.handler.BeforeFinalizeImageSaved += value; }
            remove { this.handler.BeforeFinalizeImageSaved -= value; }
        }

        public event EventHandler<ImageSavedEventArgs> ImageSaved {
            add { this.handler.ImageSaved += value; }
            remove { this.handler.ImageSaved -= value; }
        }

        public void Shutdown() {
            this.handler?.Shutdown();
        }
    }
    

    public class ImageSaveMediatorX86 : IImageSaveMediator {
        private IProfileService profileService;

        public ImageSaveMediatorX86(IProfileService profileService) {
            this.profileService = profileService;
        }

        public Task Enqueue(IImageData imageData, Task<IRenderedImage> prepareTask, IProgress<ApplicationStatus> progress, CancellationToken token) {
            return SequentialSaveAndPostProcessingForX86(imageData, prepareTask, progress, token);
        }

        public void RegisterHandler(IImageSaveController handler) {
        }

        public event Func<object, BeforeImageSavedEventArgs, Task> BeforeImageSaved;

        public async Task OnBeforeImageSaved(BeforeImageSavedEventArgs e) {
            await (BeforeImageSaved?.InvokeAsync(this, e) ?? Task.CompletedTask);
        }

        public event Func<object, BeforeFinalizeImageSavedEventArgs, Task> BeforeFinalizeImageSaved;

        public async Task OnBeforeFinalizeImageSaved(BeforeFinalizeImageSavedEventArgs e) {
            await (BeforeFinalizeImageSaved?.InvokeAsync(this, e) ?? Task.CompletedTask);
        }

        public event EventHandler<ImageSavedEventArgs> ImageSaved;

        public void OnImageSaved(ImageSavedEventArgs e) {
            ImageSaved?.Invoke(this, e);
        }

        public void Shutdown() {
        }

        /// <summary>
        /// Saves the image data and waits until finished before proceeding
        /// This method is used in the flow when the application runs in x86 mode. There the process is restricted with memory and heavy concurrent calls are problematic.
        /// </summary>
        /// <param name="imageData"></param>
        /// <param name="prepareTask"></param>
        /// <param name="progress"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task SequentialSaveAndPostProcessingForX86(IImageData imageData, Task<IRenderedImage> prepareTask, IProgress<ApplicationStatus> progress, CancellationToken token) {
            try {
                
                var preparedData = await prepareTask;
                var stats = await preparedData.RawImageData.Statistics;
                
                await OnBeforeImageSaved(new BeforeImageSavedEventArgs(imageData, prepareTask));
                await OnBeforeFinalizeImageSaved(new BeforeFinalizeImageSavedEventArgs(preparedData));

                progress?.Report(new ApplicationStatus() { Source = Loc.Instance["LblSave"], Status = Loc.Instance["LblSavingImage"] });
                var path = await imageData.SaveToDisk(new FileSaveInfo(profileService), token);

                OnImageSaved(
                    new ImageSavedEventArgs() {
                        MetaData = preparedData.RawImageData.MetaData,
                        PathToImage = new Uri(path),
                        Image = preparedData.Image,
                        FileType = profileService.ActiveProfile.ImageFileSettings.FileType,
                        Statistics = stats,
                        StarDetectionAnalysis = preparedData.RawImageData.StarDetectionAnalysis,
                        Duration = preparedData.RawImageData.MetaData.Image.ExposureTime,
                        IsBayered = preparedData.RawImageData.Properties.IsBayered,
                        Filter = preparedData.RawImageData.MetaData.FilterWheel.Filter
                    }
                );
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
            } finally {
                progress?.Report(new ApplicationStatus() { Status = string.Empty });
            }
        }
    }
}