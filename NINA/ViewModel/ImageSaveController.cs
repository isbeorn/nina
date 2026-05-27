#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.Extensions;
using NINA.Core.Utility.Notification;
using NINA.Image.FileFormat;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using NINA.ViewModel.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.ViewModel;
using Nito.AsyncEx;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.ViewModel {

    public class ImageSaveController : BaseVM, IImageSaveController {
        private IImageSaveMediator imageSaveMediator;
        private static readonly TimeSpan DiskFullNotificationThrottle = TimeSpan.FromMinutes(5);
        private DateTime lastDiskFullNotificationUtc = DateTime.MinValue;
        private Task worker;
        private CancellationTokenSource workerCTS;

        public ImageSaveController(IProfileService profileService, IImageSaveMediator imageSaveMediator, IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            this.imageSaveMediator = imageSaveMediator;
            this.imageSaveMediator.RegisterHandler(this);
            this.applicationStatusMediator = applicationStatusMediator;

            // This queue size could be adjustable for systems with lots of memory available
            queue = new AsyncProducerConsumerQueue<PrepareSaveItem>(Properties.Settings.Default.SaveQueueSize);
            workerCTS = new CancellationTokenSource();
            worker = Task.Run(DoWork);
        }

        private IApplicationStatusMediator applicationStatusMediator;
        private AsyncProducerConsumerQueue<PrepareSaveItem> queue;

        public event Func<object, BeforeImageSavedEventArgs, Task> BeforeImageSaved;
        public event Func<object, BeforeFinalizeImageSavedEventArgs, Task> BeforeFinalizeImageSaved;
        public event EventHandler<ImageSavedEventArgs> ImageSaved;

        public Task Enqueue(IImageData imageData, Task<IRenderedImage> prepareTask, IProgress<ApplicationStatus> progress, CancellationToken token) {
            var mergedCts = CancellationTokenSource.CreateLinkedTokenSource(token, workerCTS.Token);
            Logger.Debug($"Enqueuing image to be saved with id {imageData.MetaData.Image.Id}");
            return queue.EnqueueAsync(new PrepareSaveItem(imageData, prepareTask), mergedCts.Token);
        }

        private async Task DoWork() {
            while (!workerCTS.IsCancellationRequested) {
                CancellationTokenSource writeTimeoutCts = null;
                PrepareSaveItem item = null;
                string stage = "dequeue";
                string filePath = null;
                try {
                    item = await queue.DequeueAsync(workerCTS.Token);
                    var swTotal = Stopwatch.StartNew();
                    var sw = Stopwatch.StartNew();

                    Logger.Debug($"Dequeuing image to be saved with id {item.Data.MetaData.Image.Id}");
                    workerCTS.Token.ThrowIfCancellationRequested();

                    // NOTE: Consider whether this should be configurable. 5 minutes for writing files should be exceptionally conservative
                    writeTimeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                    applicationStatusMediator.StatusUpdate(new ApplicationStatus() { Source = Loc.Instance["LblSave"], Status = Loc.Instance["LblSavingImage"] });

                    filePath = profileService.ActiveProfile.ImageFileSettings.FilePath;
                    stage = "BeforeImageSaved";
                    await (BeforeImageSaved?.InvokeAsync(this, new BeforeImageSavedEventArgs(item.Data, item.PrepareTask)) ?? Task.CompletedTask);

                    var beforeSaveTime = sw.Elapsed;
                    
                    sw = Stopwatch.StartNew();

                    writeTimeoutCts.Token.ThrowIfCancellationRequested();
                    stage = "prepare image";
                    var preparedData = await item.PrepareTask;
                    var beforeFinalizeArgs = new BeforeFinalizeImageSavedEventArgs(preparedData);                    
                    stage = "BeforeFinalizeImageSaved";
                    await (BeforeFinalizeImageSaved?.InvokeAsync(this, beforeFinalizeArgs) ?? Task.CompletedTask);

                    var beforeFinalizeImageSaveTime = sw.Elapsed;
                    sw = Stopwatch.StartNew();

                    var customPatterns = beforeFinalizeArgs.Patterns;
                    var patternTemplate = profileService.ActiveProfile.ImageFileSettings.GetFilePattern(item.Data.MetaData.Image.ImageType);

                    stage = "save to disk";
                    string path = await Retry.Do(() => item.Data.SaveToDisk(new FileSaveInfo(profileService) { FilePattern = patternTemplate }, writeTimeoutCts.Token, false, customPatterns), TimeSpan.FromSeconds(1), 3);

                    var finalizeSaveTime = sw.Elapsed;
                    swTotal.Stop();
                    Logger.Info($"Successfully saved file at {path}. Duration Total: {swTotal.Elapsed}; BeforeSave: {beforeSaveTime}; BeforeFinalizeImageSaved: {beforeFinalizeImageSaveTime}; FinalizeSaveTime: {finalizeSaveTime}");


                    // Run this in a separate task to limit risk of deadlocks
                    _ = Task.Run(async () => {
                        try {
                            var stats = await item.Data.Statistics;
                            ImageSaved?.Invoke(this,
                                  new ImageSavedEventArgs() {
                                      MetaData = item.Data.MetaData,
                                      PathToImage = new Uri(path),
                                      Image = preparedData?.Image,
                                      FileType = profileService.ActiveProfile.ImageFileSettings.FileType,
                                      Statistics = stats,
                                      StarDetectionAnalysis = item.Data.StarDetectionAnalysis,
                                      Duration = item.Data.MetaData.Image.ExposureTime,
                                      IsBayered = item.Data.Properties.IsBayered,
                                      Filter = item.Data.MetaData.FilterWheel.Filter
                                  }
                          );
                        } catch(OperationCanceledException) {
                        } catch(Exception ex) {
                            Logger.Error("ImageSaved event ran into an error", ex);

                        }
                    }, workerCTS.Token).ContinueWith(t => {
                        if (t.IsFaulted) {
                            Logger.Error("ImageSaved event ran into an error", t.Exception);
                        }
                    });
                } catch (Exception ex) when (writeTimeoutCts?.IsCancellationRequested == true && !workerCTS.IsCancellationRequested) {
                    // Retry aggregates canceled attempts, so the timeout can also arrive wrapped.
                    Logger.Error($"Saving image {item?.Data.MetaData.Image.Id} to {filePath} timed out during {stage}", ex);
                    Notification.ShowError(Loc.Instance["LblSaveFileFailed"] + Environment.NewLine + filePath);
                } catch (OperationCanceledException) {
                } catch (Exception ex) {
                    Logger.Error($"Saving image {item?.Data.MetaData.Image.Id} to {filePath} failed during {stage}", ex);
                    if (ShouldShowFailureNotification(ex)) {
                        var message = Loc.Instance[IsDiskFullException(ex) ? "LblSaveFileFailedDiskFull" : "LblSaveFileFailed"];
                        Notification.ShowError(message + Environment.NewLine + filePath + Environment.NewLine + ex.Message);
                    }
                } finally {
                    writeTimeoutCts?.Dispose();
                    applicationStatusMediator.StatusUpdate(new ApplicationStatus() { Source = Loc.Instance["LblSave"], Status = string.Empty });
                }
            }
        }

        private bool ShouldShowFailureNotification(Exception exception) {
            if (!IsDiskFullException(exception)) {
                return true;
            }
            var now = DateTime.UtcNow;
            if (now - lastDiskFullNotificationUtc < DiskFullNotificationThrottle) {
                return false;
            }
            lastDiskFullNotificationUtc = now;
            return true;
        }

        private static bool IsDiskFullException(Exception exception) {
            if (exception == null) {
                return false;
            }
            if (exception is IOException ioException) {
                var error = ioException.HResult & 0xffff;
                if (error == 0x70 || error == 0x27) {
                    return true;
                }
            }
            if (exception is AggregateException aggregate) {
                return aggregate.InnerExceptions.Any(IsDiskFullException);
            }
            return IsDiskFullException(exception.InnerException);
        }

        public void Shutdown() {
            try { workerCTS?.Cancel(); } catch { }
            // Give the worker at most 1 minute to shutdown cleanly, writing any files remaining in the queue
            if (!worker.Wait(TimeSpan.FromMinutes(1))) {
                Logger.Error("Image save worker failed to cleanly shutdown");
            }
        }

        private class PrepareSaveItem {

            public PrepareSaveItem(IImageData data, Task<IRenderedImage> prepareTask) {
                Data = data;
                PrepareTask = prepareTask;
            }

            public IImageData Data { get; }
            public Task<IRenderedImage> PrepareTask { get; }
        }
    }
}
