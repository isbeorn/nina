#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.MGEN.Exceptions;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.ImageAnalysis;
using NINA.Utility.Notification;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINA.Model.MyGuider {

    internal class MGENGuider : BaseINPC, IGuider {
        private MGEN.MGEN mgen;
        private IProfileService profileService;

        public MGENGuider(IProfileService profileService) {
            this.profileService = profileService;
            MGenUpCommand = new AsyncCommand<bool>((object o) => {
                return PressButton(MGEN.MGENButton.UP, default);
            },
                (object o) => Connected == true);
            MGenDownCommand = new AsyncCommand<bool>((object o) => {
                return PressButton(MGEN.MGENButton.DOWN, default);
            },
                (object o) => Connected == true);
            MGenLeftCommand = new AsyncCommand<bool>((object o) => {
                return PressButton(MGEN.MGENButton.LEFT, default);
            },
                (object o) => Connected == true);
            MGenRightCommand = new AsyncCommand<bool>((object o) => {
                return PressButton(MGEN.MGENButton.RIGHT, default);
            },
                (object o) => Connected == true);
            MGenESCCommand = new AsyncCommand<bool>((object o) => {
                return PressButton(MGEN.MGENButton.ESC, default);
            },
                (object o) => Connected == true);
            MGenSetCommand = new AsyncCommand<bool>((object o) => {
                return PressButton(MGEN.MGENButton.SET, default);
            },
                (object o) => Connected == true);
        }

        private bool _connected = false;

        public bool Connected {
            get => _connected;
            set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        public int FocalLength {
            get => profileService.ActiveProfile.GuiderSettings.MGENFocalLength;
            set {
                profileService.ActiveProfile.GuiderSettings.MGENFocalLength = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(PixelScale));
            }
        }

        public double PixelScale {
            get {
                // According to documentation the pixel size reported is normalized to 4.85
                return Astrometry.ArcsecPerPixel(4.85, FocalLength);
            }

            set {
            }
        }

        private string _state;

        public string State {
            get => _state;
            private set {
                _state = value;
                RaisePropertyChanged();
            }
        }

        public string Name {
            get => "Lacerta MGEN Superguider";
        }

        public event EventHandler<IGuideStep> GuideEvent;

        public async Task<bool> AutoSelectGuideStar() {
            if (!await mgen.IsGuidingActive()) {
                var imagingParameter = await mgen.GetImagingParameter();

                Logger.Debug("MGEN - Starting Camera");
                await mgen.StartCamera();

                Logger.Debug($"MGEN - Setting Imaging Parameter - Gain: {imagingParameter.Gain} ExposureTime: {imagingParameter.ExposureTime} Threshold: {imagingParameter.Threshold}");
                await mgen.SetImagingParameter(imagingParameter.Gain, imagingParameter.ExposureTime, imagingParameter.Threshold);

                Logger.Debug($"MGEN - Starting Star Search - Gain: {imagingParameter.Gain} ExposureTime: {imagingParameter.ExposureTime}");
                await mgen.StartStarSearch(imagingParameter.Gain, imagingParameter.ExposureTime);

                // Get brightest star (will be at index 0)
                var starDetail = await mgen.GetStarData(0);
                Logger.Debug($"MGEN - Got Star Detail and setting new guiding position - PosX: {starDetail.PositionX} PosY: {starDetail.PositionY} Brightness: {starDetail.Brightness} Pixels: {starDetail.Pixels}");
                var success = await mgen.SetNewGuidingPosition(starDetail);
                Logger.Debug($"MGEN - Set New Guiding Position: {success.Success}");
                return success.Success;
            }
            return true;
        }

        public async Task<bool> Connect() {
            try {
                refreshCts?.Cancel();
                refreshCts?.Dispose();
                refreshCts = new CancellationTokenSource();

                mgen = new MGEN.MGEN(Path.Combine("FTDI", "ftd2xx.dll"));
                await mgen.DetectAndOpen();
                await RefreshDisplay();
                Connected = true;

                _ = QueryDeviceBackgroundTask();

                RaisePropertyChanged(nameof(PixelScale));
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
                refreshCts?.Cancel();
                return false;
            }
            return true;
        }

        private CancellationTokenSource refreshCts;

        private async Task QueryDeviceBackgroundTask() {
            while (refreshCts?.IsCancellationRequested == false) {
                try {
                    await RefreshDisplay();
                    await RefreshLEDs();
                    await RefreshGuideState();
                    await Utility.Utility.Delay(TimeSpan.FromSeconds(1), refreshCts.Token);
                } catch (OperationCanceledException) {
                    break;
                } catch (Exception) {
                }
            }
        }

        private async Task RefreshLEDs() {
            var ledCommand = await mgen.ReadLEDState(refreshCts.Token);
            if (ledCommand.Success) {
                LEDBlueActive = ledCommand.LEDs.HasFlag(MGEN.LEDS.BLUE);
                LEDGreenActive = ledCommand.LEDs.HasFlag(MGEN.LEDS.GREEN);
                LEDRedUpActive = ledCommand.LEDs.HasFlag(MGEN.LEDS.UP_RED);
                LEDRedDownActive = ledCommand.LEDs.HasFlag(MGEN.LEDS.DOWN_RED);
                LEDRedLeftActive = ledCommand.LEDs.HasFlag(MGEN.LEDS.LEFT_RED);
                LEDRedRightActive = ledCommand.LEDs.HasFlag(MGEN.LEDS.RIGHT_RED);
            }
        }

        private async Task RefreshDisplay() {
            var mediaColor1 = profileService.ActiveProfile.ColorSchemaSettings.ColorSchema.PrimaryColor;
            var primary = System.Drawing.Color.FromArgb(mediaColor1.A, mediaColor1.R, mediaColor1.G, mediaColor1.B);
            var mediaColor2 = profileService.ActiveProfile.ColorSchemaSettings.ColorSchema.SecondaryBackgroundColor;
            var background = System.Drawing.Color.FromArgb(mediaColor2.A, mediaColor2.R, mediaColor2.G, mediaColor2.B);
            var display = await mgen.ReadDisplay(primary, background, refreshCts.Token);
            Display = ImageUtility.ConvertBitmap(display, PixelFormats.Bgra32);
        }

        private MGENGuideStep _lastStep;
        private int _lastStepNumber = 0;

        private async Task RefreshGuideState() {
            if (await mgen.IsGuidingActive(refreshCts.Token)) {
                var state = await mgen.QueryGuideState(refreshCts.Token);
                if (_lastStep?.Frame != state.FrameInfo.FrameIndex) {
                    _lastStep = new MGENGuideStep() {
                        Frame = state.FrameInfo.FrameIndex,
                        Time = _lastStepNumber++,
                        RADistanceRaw = state.FrameInfo.DriftRA,
                        DECDistanceRaw = state.FrameInfo.DriftDec
                    };
                    GuideEvent?.Invoke(this, _lastStep);
                }
            }
        }

        private class MGENGuideStep : IGuideStep {
            public double Frame { get; set; }

            public double Time { get; set; }

            public double RADistanceRaw { get; set; }
            public double DECDistanceRaw { get; set; }

            public double RADuration { get; set; }

            public double DECDuration { get; set; }

            public string Event { get; set; }

            public string TimeStamp { get; set; }

            public string Host { get; set; }

            public int Inst { get; set; }

            public IGuideStep Clone() {
                return (MGENGuideStep)this.MemberwiseClone();
            }
        }

        private async Task<bool> PressButton(MGEN.MGENButton button, CancellationToken ct) {
            var press = await mgen.PressButton(button, ct);
            await RefreshDisplay();
            return press;
        }

        private bool _ledBlueActive;

        public bool LEDBlueActive {
            get => _ledBlueActive;
            set {
                _ledBlueActive = value;
                RaisePropertyChanged();
            }
        }

        private bool _ledGreenActive;

        public bool LEDGreenActive {
            get => _ledGreenActive;
            set {
                _ledGreenActive = value;
                RaisePropertyChanged();
            }
        }

        private bool _ledRedUpActive;

        public bool LEDRedUpActive {
            get => _ledRedUpActive;
            set {
                _ledRedUpActive = value;
                RaisePropertyChanged();
            }
        }

        private bool _ledRedDownActive;

        public bool LEDRedDownActive {
            get => _ledRedDownActive;
            set {
                _ledRedDownActive = value;
                RaisePropertyChanged();
            }
        }

        private bool _ledRedLeftActive;

        public bool LEDRedLeftActive {
            get => _ledRedLeftActive;
            set {
                _ledRedLeftActive = value;
                RaisePropertyChanged();
            }
        }

        private bool _ledRedRightActive;

        public bool LEDRedRightActive {
            get => _ledRedRightActive;
            set {
                _ledRedRightActive = value;
                RaisePropertyChanged();
            }
        }

        private BitmapSource _display;

        public BitmapSource Display {
            get => _display;
            set {
                _display = value;
                RaisePropertyChanged();
            }
        }

        public bool Disconnect() {
            refreshCts?.Cancel();
            mgen.Disconnect();
            Display = null;
            Connected = false;
            return true;
        }

        public async Task<bool> Dither(CancellationToken ct) {
            try {
                if (await mgen.IsGuidingActive(ct)) {
                    Logger.Debug("MGEN - Dithering");
                    await mgen.Dither(ct);
                    return true;
                } else {
                    Notification.ShowError("Guiding is not active. Unable to dither");
                }
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError("Failed to communicate to MGEN during dithering");
            }
            return false;
        }

        public async Task<bool> StartGuiding(CancellationToken ct) {
            try {
                if (!await mgen.IsGuidingActive(ct)) {
                    await StartCalibrationIfRequired(ct);

                    Logger.Debug("MGEN - Starting Guiding");
                    await mgen.StartGuiding(ct);
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
                return false;
            }

            return true;
        }

        private async Task<bool> StartCalibrationIfRequired(CancellationToken ct) {
            using (ct.Register(async () => await mgen.CancelCalibration())) {
                var calibrationStatus = await mgen.QueryCalibration(ct); ;
                if (!calibrationStatus.CalibrationStatus.HasFlag(MGEN.CalibrationStatus.Done) || calibrationStatus.CalibrationStatus.HasFlag(MGEN.CalibrationStatus.Error)) {
                    Logger.Debug("MGEN - Starting Calibraiton");
                    _ = await AutoSelectGuideStar();
                    _ = await mgen.StartCalibration(ct);

                    do {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        calibrationStatus = await mgen.QueryCalibration(ct);
                        State = calibrationStatus.CalibrationStatus.ToString();
                    } while (!calibrationStatus.CalibrationStatus.HasFlag(MGEN.CalibrationStatus.Done) && !calibrationStatus.CalibrationStatus.HasFlag(MGEN.CalibrationStatus.Error));

                    if (calibrationStatus.CalibrationStatus.HasFlag(MGEN.CalibrationStatus.Error)) {
                        Logger.Error(calibrationStatus.Error);
                        Notification.ShowError(calibrationStatus.Error);
                        return false;
                    }
                }
                return true;
            }
        }

        public async Task<bool> StopGuiding(CancellationToken ct) {
            if (await mgen.IsGuidingActive(ct)) {
                Logger.Debug("MGEN - Stopping Guiding");
                await mgen.StopGuiding(ct);
            }
            return true;
        }

        public ICommand MGenUpCommand { get; }
        public ICommand MGenDownCommand { get; }
        public ICommand MGenLeftCommand { get; }
        public ICommand MGenRightCommand { get; }
        public ICommand MGenESCCommand { get; }
        public ICommand MGenSetCommand { get; }

        public string Id {
            get => "Lacerta_MGEN_Superguider";
        }
    }
}