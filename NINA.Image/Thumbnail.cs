#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Profile.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NINA.Image.ImageData;
using NINA.Core.Enum;
using NINA.Image.Interfaces;
using NINA.Core.Locale;

namespace NINA.Image {

    public class Thumbnail : BaseINPC {
        protected readonly IImageDataFactory imageDataFactory;

        public Thumbnail(IImageDataFactory imageDataFactory) {
            this.imageDataFactory = imageDataFactory;
            Grade = "";
        }

        public async Task<IImageData> LoadOriginalImage(IProfileService profileService) {
            try {
                var filePath = GetFilePath();
                Logger.Info($"Reloading image from {filePath}");
                if (File.Exists(filePath)) {
                    return await imageDataFactory.CreateFromFile(filePath, (int)profileService.ActiveProfile.CameraSettings.BitDepth, IsBayered, profileService.ActiveProfile.CameraSettings.RawConverter);
                } else {
                    Logger.Info($"Unable to reload image as the file does not exist at {filePath}");
                    Notification.ShowError(String.Format(Loc.Instance["LblFileNotExist"], filePath));
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
            }

            return null;
        }

        public async Task<bool> ChangeGrade(string grade) {
            var oldFileName = GetFilePath();
            if (File.Exists(oldFileName)) {
                int attemptsRemaining = 3;
                Grade = grade;
                while (attemptsRemaining > 0) {
                    try {
                        // This operation may fail because of antivirus. In that case, we'd expect an IOException
                        File.Move(oldFileName, GetFilePath());
                        RaisePropertyChanged(nameof(FilePath));
                        RaisePropertyChanged(nameof(Grade));
                        return true;
                    } catch (IOException ex) {
                        if (--attemptsRemaining > 0) {
                            Logger.Error($"Failed to change grade. Retrying...", ex);
                            await Task.Delay(TimeSpan.FromSeconds(2));
                        } else {
                            Logger.Error("Failed to change grade. Aborting...", ex);
                            Notification.ShowError(ex.Message);
                            return false;
                        }
                    } catch (Exception ex) {
                        Logger.Error("Failed to change grade. Aborting...", ex);
                        Notification.ShowError(ex.Message);
                        return false;
                    }
                }
            }
            return false;
        }

        private string GetFilePath() {
            var fileName = Path.GetFileNameWithoutExtension(ImagePath.LocalPath);
            var extension = Path.GetExtension(ImagePath.LocalPath);

            if (string.IsNullOrEmpty(Grade)) {
                return ImagePath.LocalPath;
            } else {
                return ImagePath.LocalPath.Replace($"{fileName}{extension}", ($"{Grade}_{fileName}{extension}"));
            }
        }

        private bool isSelected;
        public bool IsSelected {
            get => isSelected;
            set {
                isSelected = value;
                RaisePropertyChanged();
            }
        }

        public BitmapSource ThumbnailImage { get; set; }

        public IImageStatistics ImageStatistics { get; set; }

        public double Mean => ImageStatistics?.Mean ?? double.NaN;

        public IStarDetectionAnalysis StarDetectionAnalysis { get; set; }

        public double HFR => StarDetectionAnalysis?.HFR ?? double.NaN;

        public bool IsBayered { get; set; }

        public string FilePath => GetFilePath();

        public Uri ImagePath { get; set; }

        public FileTypeEnum FileType { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        public string Filter { get; set; }

        public double Duration { get; set; }

        public string Grade { get; private set; }
    }
}