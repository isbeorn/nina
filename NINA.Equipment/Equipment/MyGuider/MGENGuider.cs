#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.MGEN;
using NINA.Exceptions;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Astrometry;
using NINA.Core.Utility.Notification;
using NINA.Core.Locale;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using NINA.Core.Interfaces;
using NINA.Image.ImageAnalysis;
using NINA.Equipment.Interfaces;
using NINA.Core.Model;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Enum;
using System.Collections.Generic;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Equipment.MyGuider.PHD2;

namespace NINA.Equipment.Equipment.MyGuider {

    public class MGENGuider : BaseINPC, IGuider {
        public readonly IMGEN MGen;
        private IProfileService profileService;
        private readonly IWindowServiceFactory windowServiceFactory;
        private ITelescopeMediator telescopeMediator;
        private Coordinates lastCalibratedCoords = null;
        private PierSide lastCalibratedSideOfPier;
        private Coordinates lastKnownGuidingPosition = null;

        public MGENGuider(IMGEN mgen, string name, string id, IProfileService profileService, ITelescopeMediator telescopeMediator, IWindowServiceFactory windowServiceFactory) {
            this.windowServiceFactory = windowServiceFactory;
            this.MGen = mgen;
            this.Name = name;
            this.Id = id;
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
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

        private bool needsCalibration = false;

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

        public int PixelMargin {
            get => profileService.ActiveProfile.GuiderSettings.MGENPixelMargin;
            set {
                profileService.ActiveProfile.GuiderSettings.MGENPixelMargin = value;
                RaisePropertyChanged();
            }
        }

        public double PixelScale {
            get => AstroUtil.ArcsecPerPixel(this.MGen.PixelSize, FocalLength);

            set {
            }
        }

        public int DitherSettlingTime {
            get => profileService.ActiveProfile.GuiderSettings.SettleTime;

            set {
                profileService.ActiveProfile.GuiderSettings.SettleTime = value;
                RaisePropertyChanged();
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

        public string Name { get; }
        public string DisplayName => Name;

        public event EventHandler<IGuideStep> GuideEvent;

        public async Task<bool> AutoSelectGuideStar() {
            if (await MGen.IsGuidingActive()) {
                Logger.Debug("MGEN - Stopping guiding to select new guide star");
                await MGen.StopGuiding();
            }
            var imagingParameter = await MGen.GetImagingParameter();
            var ditherAmplitude = await MGen.GetDitherAmplitude();
            Logger.Debug($"MGEN - Dither amplitude {ditherAmplitude.Amplitude} pixels");
            Logger.Debug($"MGEN - Pixel margin {PixelMargin} pixels");
            Logger.Debug("MGEN - Starting Camera");
            await MGen.StartCamera();
            Logger.Debug($"MGEN - Starting Star Search - Gain: {imagingParameter.Gain} ExposureTime: {imagingParameter.ExposureTime}");
            var numberOfStars = await MGen.StartStarSearch(imagingParameter.Gain, imagingParameter.ExposureTime);
            Logger.Debug($"MGEN - Star Search Done - {numberOfStars} stars found");
            if (numberOfStars > 0) {
                if (MGen is MGEN2.MGEN) {
                    bool starSearchSuccess = false;
                    for (byte starIndex = 0; starIndex < numberOfStars; starIndex++) {
                        var starDetail = await MGen.GetStarData(starIndex);
                        if (starDetail.PositionX > Math.Ceiling(Math.Max(PixelMargin, ditherAmplitude.Amplitude)) &&
                            starDetail.PositionX < MGen.SensorSizeX - Math.Ceiling(Math.Max(PixelMargin, ditherAmplitude.Amplitude)) &&
                            starDetail.PositionY > Math.Ceiling(Math.Max(PixelMargin, ditherAmplitude.Amplitude)) &&
                            starDetail.PositionY < MGen.SensorSizeY - Math.Ceiling(Math.Max(PixelMargin, ditherAmplitude.Amplitude)) &&
                            starDetail.Pixels < 60) {
                            Logger.Info($"MGEN - Got Star Detail and setting new guiding position - PosX: {starDetail.PositionX} PosY: {starDetail.PositionY} Brightness: {starDetail.Brightness} Pixels: {starDetail.Pixels}");
                            starSearchSuccess = await MGen.SetNewGuidingPosition(starDetail);
                            Logger.Debug($"MGEN - Set New Guiding Position: {starSearchSuccess}");
                            Logger.Debug($"MGEN - Setting Imaging Parameter - Gain: {imagingParameter.Gain} ExposureTime: {imagingParameter.ExposureTime} Threshold: {imagingParameter.Threshold}");
                            await MGen.SetImagingParameter(imagingParameter.Gain, imagingParameter.ExposureTime, imagingParameter.Threshold);
                            break;
                        } else {
                            Logger.Info($"MGEN - Got Star Detail but skipping star because too close to edge or too big - PosX: {starDetail.PositionX} PosY: {starDetail.PositionY} Brightness: {starDetail.Brightness} Pixels: {starDetail.Pixels}");
                        }
                    }
                    if (!starSearchSuccess) {
                        Logger.Error($"MGEN - No guide star found!");
                    }
                    return starSearchSuccess;
                } 
            }
            return numberOfStars > 0;
        }

        public Task<bool> Connect() {
            return Connect(default);
        }

        private CancellationTokenSource refreshCts;

        private async Task QueryDeviceBackgroundTask() {
            while (refreshCts?.IsCancellationRequested == false) {
                try {
                    await RefreshDisplay();
                    await RefreshLEDs();
                    await RefreshGuideState();
                    await CoreUtil.Delay(TimeSpan.FromSeconds(1), refreshCts.Token);
                } catch (OperationCanceledException) {
                    break;
                } catch (Exception) {
                }
            }
        }

        private async Task RefreshLEDs() {
            LEDState = await MGen.ReadLEDState(refreshCts.Token);
        }

        private async Task RefreshDisplay() {
            var mediaColor1 = profileService.ActiveProfile.ColorSchemaSettings.ColorSchema.PrimaryColor;
            var primary = System.Drawing.Color.FromArgb(mediaColor1.A, mediaColor1.R, mediaColor1.G, mediaColor1.B);
            var mediaColor2 = profileService.ActiveProfile.ColorSchemaSettings.ColorSchema.SecondaryBackgroundColor;
            var background = System.Drawing.Color.FromArgb(mediaColor2.A, mediaColor2.R, mediaColor2.G, mediaColor2.B);
            var display = await MGen.ReadDisplay(primary, background, refreshCts.Token);
            Display = ImageUtility.ConvertBitmap(display);
        }

        private MGENGuideStep _lastStep;
        private int _lastStepNumber = 0;

        private async Task RefreshGuideState() {
            if (await MGen.IsGuidingActive(refreshCts.Token)) {
                var state = await MGen.QueryGuideState(refreshCts.Token);
                if (_lastStep?.Frame != state.FrameInfo.FrameIndex) {
                    _lastStep = new MGENGuideStep() {
                        Frame = state.FrameInfo.FrameIndex,
                        Time = _lastStepNumber++,
                        RADistanceRaw = state.FrameInfo.DriftRA / 256.0,
                        DECDistanceRaw = state.FrameInfo.DriftDec / 256.0
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
            var press = await MGen.PressButton(button, ct);
            await RefreshDisplay();
            return press;
        }

        private LEDState _ledState;

        public LEDState LEDState {
            get => _ledState;
            set {
                _ledState = value;
                RaisePropertyChanged();
            }
        }

        private BitmapSource _display;

        public BitmapSource Display {
            get => _display;
            set {
                _display = value;
                _display?.Freeze();
                RaisePropertyChanged();
            }
        }

        public void Disconnect() {
            try { this.refreshCts?.Cancel(); } catch { }
            MGen.Disconnect();
            Display = null;
            Connected = false;
        }

        public async Task<bool> Dither(IProgress<ApplicationStatus> progress, CancellationToken ct) {
            try {
                if (await MGen.IsGuidingActive(ct)) {
                    Logger.Debug("MGEN - Dithering");
                    var task = await Task.Run<bool>(async () => {
                        bool dithered = await MGen.Dither(ct);
                        if (dithered) {
                            await WaitForSettling(DitherSettlingTime, progress, ct);
                            Logger.Debug($"MGEN - Storing last known guiding position: {lastKnownGuidingPosition}");
                            lastKnownGuidingPosition = telescopeMediator.GetInfo().Connected ? telescopeMediator.GetCurrentPosition() : null;
                        }
                        return dithered;
                    });
                } else {
                    Logger.Error("Guiding is not active. Unable to dither");
                    Notification.ShowError("Guiding is not active. Unable to dither");
                }
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError("Failed to communicate to MGEN during dithering");
            } finally {
                progress.Report(new ApplicationStatus { Status = string.Empty });
            }
            return false;
        }

        public async Task<bool> StartGuiding(bool forceCalibration, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            try {
                var wasActivelyGuiding = await MGen.IsActivelyGuiding(ct);
                bool telescopeMoved = false;
                if (telescopeMediator.GetInfo().Connected) {
                    var ditherAmplitude = await MGen.GetDitherAmplitude();
                    var ditherAmplitudeArcmins = ditherAmplitude.Amplitude * PixelScale / 60.0;
                    Logger.Debug($"MGEN - Last known guiding position: {lastKnownGuidingPosition}");
                    Logger.Debug($"MGEN - Current telescope position: {telescopeMediator.GetCurrentPosition()}");
                    Logger.Debug($"MGEN - Last known (calibrated) side of pier: {lastCalibratedSideOfPier}");
                    Logger.Debug($"MGEN - Current side of pier: {telescopeMediator.GetInfo().SideOfPier}");
                    telescopeMoved = (lastKnownGuidingPosition == null || lastCalibratedSideOfPier != telescopeMediator.GetInfo().SideOfPier ||
                        (telescopeMediator.GetCurrentPosition() - lastKnownGuidingPosition).Distance.ArcMinutes >
                        Math.Max(1.2 * ditherAmplitudeArcmins, profileService.ActiveProfile.PlateSolveSettings.Threshold));
                    Logger.Debug($"Telescope moved: {telescopeMoved}");
                }
                if (!wasActivelyGuiding && !telescopeMoved) {
                    try {
                        Logger.Debug("MGEN - Not actively guiding, attempting to start guiding");
                        await MGen.StartGuiding(ct);
                        Logger.Debug("MGEN - Guiding successfully resumed");
                    } catch (NoStarSeenException) {
                        Logger.Debug("MGEN - Guiding didn't start, selecting new guide star");
                        await AutoSelectGuideStar();
                    }
                } else if (telescopeMoved) {
                    Logger.Debug("MGEN - Telescope moved from last known guiding position, selecting new guide star");
                    await AutoSelectGuideStar();
                    if (telescopeMediator.GetInfo().Connected) {
                        lastKnownGuidingPosition = telescopeMediator.GetCurrentPosition();
                    } else {
                        lastKnownGuidingPosition = null;
                    }
                }
                var calibrated = await StartCalibrationIfRequired(forceCalibration, ct);
                var guidingStopped = !await MGen.IsGuidingActive(ct);
                if (calibrated || !wasActivelyGuiding || guidingStopped) {
                    Logger.Info("MGEN - Starting Guiding");
                    await MGen.StartGuiding(ct);
                    if (!await MGen.IsGuidingActive(ct)) {
                        Logger.Error("MGEN - Failed to start guiding");
                        return false;
                    }
                    await WaitForSettling(DitherSettlingTime, progress, ct);
                } else {
                    return await MGen.IsActivelyGuiding(ct);
                }
                
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
                return false;
            } finally {
                progress.Report(new ApplicationStatus { Status = string.Empty });
            }
            return true;
        }

        /// <summary>
        /// Starts a calibration if the forceCalibration or needsCalibration flag is true, the calibration status was not done or the last calibration status was in error state        /// 
        /// </summary>
        /// <param name="forceCalibration"></param>
        /// <param name="ct"></param>
        /// <returns>
        ///     true: A calibration was done successfully
        ///     false: No calibration was required
        ///     throws Exception: The calibration failed
        /// </returns>
        private async Task<bool> StartCalibrationIfRequired(bool forceCalibration, CancellationToken ct) {
            using (ct.Register(async () => await MGen.CancelCalibration())) {
                var calibrationStatus = await MGen.QueryCalibration(ct);
                if (lastCalibratedCoords != null) {
                    Logger.Debug($"MGEN - Last calibration declination: {lastCalibratedCoords.Dec}");
                    Logger.Debug($"MGEN - Last calibration side of pier: {lastCalibratedSideOfPier}");
                } else {
                    Logger.Debug($"MGEN - Last calibration location: not set yet");
                }
                bool movedTooFar = false;
                if (telescopeMediator.GetInfo().Connected) {
                    Logger.Debug($"MGEN - Current declination: {telescopeMediator.GetCurrentPosition().Dec}");
                    Logger.Debug($"MGEN - Current side of pier: {telescopeMediator.GetInfo().SideOfPier}");
                    movedTooFar = (lastCalibratedCoords == null ? true :
                        (lastCalibratedSideOfPier != telescopeMediator.GetInfo().SideOfPier ||
                        Math.Max(Math.Cos(lastCalibratedCoords.Dec * Math.PI / 180), Math.Cos(telescopeMediator.GetCurrentPosition().Dec * Math.PI / 180)) /
                        Math.Min(Math.Cos(lastCalibratedCoords.Dec * Math.PI / 180), Math.Cos(telescopeMediator.GetCurrentPosition().Dec * Math.PI / 180)) > 1.2));
                } else {
                    Logger.Debug("MGEN - Telescope not connected, impossible to determine if calibration is still good");
                }
                Logger.Debug($"MGEN - Moved too far? {movedTooFar}");
                if (movedTooFar || forceCalibration || needsCalibration || !calibrationStatus.CalibrationStatus.HasFlag(MGEN.CalibrationStatus.Done) || calibrationStatus.CalibrationStatus.HasFlag(MGEN.CalibrationStatus.Error)) {
                    if (await MGen.IsGuidingActive()) {
                        Logger.Debug("MGEN - Stopping guiding to start new calibration");
                        await MGen.StopGuiding();
                    }
                    Logger.Info("MGEN - Starting Calibration");
                    if (MGen is MGEN3.MGEN3 mg3) {
                        Logger.Info("MGEN - Clearing Calibration data");
                        await mg3.ClearCalibration(ct);
                    }

                    _ = await MGen.StartCalibration(ct);

                    if (MGen is MGEN3.MGEN3) {
                        calibrationStatus = await MGen.QueryCalibration(ct);
                        if (!calibrationStatus.CalibrationStatus.HasFlag(MGEN.CalibrationStatus.Done)) {
                            Logger.Error($"MGEN3 calibration was not succcessful: {calibrationStatus.Error}");
                            throw new Exception(calibrationStatus.Error);
                        }
                    } else {
                        do {
                            await Task.Delay(TimeSpan.FromSeconds(1), ct);
                            calibrationStatus = await MGen.QueryCalibration(ct);
                            State = calibrationStatus.CalibrationStatus.ToString();
                        } while (!calibrationStatus.CalibrationStatus.HasFlag(MGEN.CalibrationStatus.Done) && !calibrationStatus.CalibrationStatus.HasFlag(MGEN.CalibrationStatus.Error));
                    }

                    if (calibrationStatus.CalibrationStatus.HasFlag(MGEN.CalibrationStatus.Error)) {
                        Logger.Error($"MGEN2 calibration was not succcessful: {calibrationStatus.Error}");
                        throw new Exception(calibrationStatus.Error);
                    } else {
                        if (telescopeMediator.GetInfo().Connected) {
                            lastCalibratedCoords = telescopeMediator.GetCurrentPosition();
                            lastCalibratedSideOfPier = telescopeMediator.GetInfo().SideOfPier;
                            Logger.Debug($"MGEN - Stored calibration declination: {lastCalibratedCoords}");
                            Logger.Debug($"MGEN - Stored calibration side of pier: {lastCalibratedSideOfPier}");
                        }
                        needsCalibration = false;
                        return true;
                    }
                }
                // No calibration was required - return false;
                return false;
            }
        }

        public bool CanClearCalibration => true;

        public bool CanSetShiftRate => false;
        public bool ShiftEnabled => false;
        public bool CanGetLockPosition => false;

        public SiderealShiftTrackingRate ShiftRate => SiderealShiftTrackingRate.Disabled;

        public Task<bool> ClearCalibration(CancellationToken ct) {
            return Task.FromResult(true);
        }

        public async Task<bool> StopGuiding(CancellationToken ct) {
            if (await MGen.IsGuidingActive(ct)) {
                Logger.Debug("MGEN - Stopping Guiding");
                if (telescopeMediator.GetInfo().Connected) {
                    lastKnownGuidingPosition = telescopeMediator.GetCurrentPosition();
                    Logger.Debug($"MGEN - Storing last known guiding position: {lastKnownGuidingPosition}");
                } else {
                    lastKnownGuidingPosition = null;
                }
                return await MGen.StopGuiding(ct);
            } else {
                return false;
            }
        }

        private async Task WaitForSettling(int secondsDelay, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            await CoreUtil.Wait(TimeSpan.FromSeconds(secondsDelay), ct, progress, Loc.Instance["LblSettle"]);
        }

        public async Task<bool> Connect(CancellationToken token) {
            try {
                try { 
                    refreshCts?.Cancel();
                    refreshCts?.Dispose();
                } catch { }
                refreshCts = new CancellationTokenSource();

                await MGen.DetectAndOpen();
                await RefreshDisplay();
                Connected = true;

                _ = QueryDeviceBackgroundTask();

                RaisePropertyChanged(nameof(PixelScale));
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
                try { refreshCts?.Cancel(); } catch { }
                return false;
            }
            return true;
        }

        public void SetupDialog() {
            var windowService = windowServiceFactory.Create();
            windowService.ShowDialog(this, Loc.Instance["LblMGENSetup"], System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.SingleBorderWindow);
        }

        public ICommand MGenUpCommand { get; }
        public ICommand MGenDownCommand { get; }
        public ICommand MGenLeftCommand { get; }
        public ICommand MGenRightCommand { get; }
        public ICommand MGenESCCommand { get; }
        public ICommand MGenSetCommand { get; }

        public string Id { get; }

        public bool HasSetupDialog => !Connected;

        public string Category => "Lacerta";

        public string Description => "";

        public string DriverInfo => "";

        public string DriverVersion => "";

        public Task<bool> SetShiftRate(SiderealShiftTrackingRate shiftTrackingRate, CancellationToken ct) {
            return Task.FromResult(false);
        }

        public Task<bool> StopShifting(CancellationToken ct) {
            return Task.FromResult(true);
        }

        public IList<string> SupportedActions => new List<string>();

        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }

        public string SendCommandString(string command, bool raw) {
            throw new NotImplementedException();
        }

        public bool SendCommandBool(string command, bool raw) {
            throw new NotImplementedException();
        }

        public void SendCommandBlind(string command, bool raw) {
            throw new NotImplementedException();
        }

        public Task<LockPosition> GetLockPosition() {
            throw new NotImplementedException();
        }

    }

    public class MGenLogger : NINA.MGEN.ILogger {

        public void Debug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "") {
            Logger.Debug(message, memberName, sourceFilePath);
        }

        public void Error(Exception ex, [CallerMemberName] string memberName = "", string sourceFilePath = "") {
            Logger.Error(ex, memberName, sourceFilePath);
        }

        public void Error(string customMsg, Exception ex, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "") {
            Logger.Error(customMsg, ex, memberName, sourceFilePath);
        }

        public void Error(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "") {
            Logger.Error(message, memberName, sourceFilePath);
        }

        public void Info(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "") {
            Logger.Info(message, memberName, sourceFilePath);
        }

        public void Trace(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "") {
            Logger.Trace(message, memberName, sourceFilePath);
        }

        public void Warning(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "") {
            Logger.Warning(message, memberName, sourceFilePath);
        }
    }
}