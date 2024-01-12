#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using NINA.Core.Interfaces;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Equipment.MyGuider.MetaGuide;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyGuider {

    public class MetaGuideGuider : BaseINPC, IGuider {

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(int hWnd, uint Msg, int wParam, int lParam);

        private const int HWND_BROADCAST = 0xffff;
        private static readonly uint remoteLockMsg = RegisterWindowMessage("MG_RemoteLock");
        private static readonly uint remoteUnlockMsg = RegisterWindowMessage("MG_RemoteUnLock");
        private static readonly uint remoteGuideMsg = RegisterWindowMessage("MG_RemoteGuide");
        private static readonly uint remoteUnguideMsg = RegisterWindowMessage("MG_RemoteUnGuide");
        private static readonly uint remoteDitherMsg = RegisterWindowMessage("MG_RemoteDither");
        private static readonly uint remoteDitherRadiusMsg = RegisterWindowMessage("MG_RemoteDitherRadius");
        private static readonly uint remoteShift = RegisterWindowMessage("MG_RemoteShift");
        private static readonly uint remoteUnShift = RegisterWindowMessage("MG_RemoteUnShift");
        private static readonly uint remoteSetShift = RegisterWindowMessage("MG_RemoteSetShift");

        private const int METAGUIDE_CONNECT_TIMEOUT_MS = 5000;
        private const int METAGUIDE_QUEUE_FLUSH_TIMEOUT_MS = 2000;

        private static readonly Version MINIMUM_MG_VERSION = Version.Parse("5.4.9");

        private readonly object lockobj = new object();
        private readonly AsyncAutoResetEvent metaGuideMessageReceivedEvent = new AsyncAutoResetEvent(false);

        private readonly IProfileService profileService;
        private readonly IWindowServiceFactory windowServiceFactory;
        private CancellationTokenSource clientCTS = null;
        private MetaGuideListener listener = null;
        private Task listenerTask = null;
        private bool connecting = false;
        private MetaGuideGuideMsg latestUnpublishedGuide = null;
        private volatile MetaGuideStatusMsg latestStatus = null;

        public MetaGuideGuider(IProfileService profileService, IWindowServiceFactory windowServiceFactory) {
            this.profileService = profileService;
            this.windowServiceFactory = windowServiceFactory;
        }

        public string Name => "MetaGuide";
        public string Id => "MetaGuide";

        public event EventHandler<IGuideStep> GuideEvent;

        private bool connected;

        public bool Connected {
            get => connected;
            private set {
                if (connected != value) {
                    connected = value;
                    UpdateState();
                    RaisePropertyChanged();
                }
            }
        }

        private double pixelScale;

        public double PixelScale {
            get => pixelScale;
            set {
                if (pixelScale != value) {
                    pixelScale = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string state = "Not Ready";

        public string State {
            get => state;
            set {
                if (state != value) {
                    state = value;
                    RaisePropertyChanged();
                }
            }
        }

        private volatile bool isLocked = false;

        public bool IsLocked {
            get => this.isLocked;
            private set {
                if (this.isLocked != value) {
                    this.isLocked = value;
                    UpdateState();
                    RaisePropertyChanged();
                }
            }
        }

        private volatile bool isGuiding = false;

        public bool IsGuiding {
            get => this.isGuiding;
            private set {
                if (this.isGuiding != value) {
                    this.isGuiding = value;
                    UpdateState();
                    RaisePropertyChanged();
                }
            }
        }

        private double intensity;

        public double Intensity {
            get => intensity;
            set {
                if (intensity != value) {
                    intensity = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double fwhm;

        public double FWHM {
            get => fwhm;
            set {
                if (fwhm != value) {
                    fwhm = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double seeing;

        public double Seeing {
            get => seeing;
            set {
                if (seeing != value) {
                    seeing = value;
                    RaisePropertyChanged();
                }
            }
        }

        private CalibrationState calibrationState;

        public CalibrationState CalibrationState {
            get => calibrationState;
            set {
                if (calibrationState != value) {
                    calibrationState = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double focalLength;

        public double FocalLength {
            get => focalLength;
            set {
                if (focalLength != value) {
                    focalLength = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double pixelSize;

        public double PixelSize {
            get => pixelSize;
            set {
                if (pixelSize != value) {
                    pixelSize = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool HasSetupDialog => !Connected;

        public string Category => "Guiders";

        public string Description => "MetaGuide";

        public string DriverInfo => "MetaGuide";

        public string DriverVersion => "1.0";

        public Task<bool> AutoSelectGuideStar() {
            if (!this.profileService.ActiveProfile.GuiderSettings.MetaGuideLockWhenGuiding) {
                return Task.FromResult(true);
            }

            // Fire and forget
            return Task.FromResult(PostAndCheckMessage("LockOn", remoteLockMsg, 0, 0));
        }

        public async Task<bool> Connect(CancellationToken token) {
            if (Connected) {
                return true;
            }

            lock (this.lockobj) {
                if (this.connecting) {
                    return false;
                }
                this.connecting = true;
            }

            bool useIpAddressAny = this.profileService.ActiveProfile.GuiderSettings.MetaGuideUseIpAddressAny;
            int port = this.profileService.ActiveProfile.GuiderSettings.MetaGuidePort;
            this.clientCTS = new CancellationTokenSource();
            this.listener = new MetaGuideListener();
            this.listener.OnStatus += Listener_OnStatus;
            this.listener.OnGuide += Listener_OnGuide;
            this.listener.OnDisconnected += Listener_OnDisconnected;

            this.listenerTask = this.listener.RunListener(useIpAddressAny, port, this.clientCTS.Token);
            this.listenerTask.GetAwaiter().OnCompleted(() => this.Disconnect());
            bool connectionSuccess = false;

            try {
                var connectTimeoutTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(METAGUIDE_CONNECT_TIMEOUT_MS));
                var connectCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(connectTimeoutTokenSource.Token, token);
                connectionSuccess = await WaitOnEventChangeCondition(() => this.latestStatus != null, connectCancellationTokenSource.Token);

                lock (this.lockobj) {
                    if (!connectionSuccess) {
                        Logger.Error("Failed to connect to MetaGuide. Check to make sure it is running, that broadcast is enabled in Setup -> Extra, and that the broadcast address and port match up with NINA settings.");
                        Notification.ShowError(Loc.Instance["LblMetaGuideConnectionFailed"]);
                    } else if (this.latestStatus != null && this.latestStatus.MetaGuideVersion < MINIMUM_MG_VERSION) {
                        Logger.Error($"MetaGuide is version {this.latestStatus.MetaGuideVersion} but must be at least {MINIMUM_MG_VERSION}");
                        Notification.ShowError(String.Format(Loc.Instance["LblMetaGuideVersionCheckFailed"], MINIMUM_MG_VERSION));
                        connectionSuccess = false;
                    }
                    this.Connected = connectionSuccess;
                    this.connecting = false;
                }
                return connectionSuccess;
            } catch (Exception ex) {
                Logger.Error("Failed to connect to MetaGuide. Check to make sure it is running, that broadcast is enabled in Setup -> Extra, and that the broadcast address and port match up with NINA settings.", ex);
                Notification.ShowError(Loc.Instance["LblMetaGuideConnectionFailed"]);
                return false;
            } finally {
                if (!connectionSuccess) {
                    this.Disconnect();
                }
            }
        }

        private void Listener_OnDisconnected() {
            this.Disconnect();
        }

        public void Disconnect() {
            var listenerTask = Interlocked.Exchange(ref this.listenerTask, null);
            lock (this.lockobj) {
                this.Connected = false;
                this.connecting = false;
                try { this.clientCTS?.Cancel(); this.clientCTS?.Dispose(); } catch { }
                this.clientCTS = null;
                this.latestStatus = null;
                this.IsGuiding = false;
                this.IsLocked = false;
                if (this.listener != null) {
                    this.listener.OnGuide -= Listener_OnGuide;
                    this.listener.OnStatus -= Listener_OnStatus;
                    this.listener.OnDisconnected -= Listener_OnDisconnected;
                }
                this.listener = null;
                this.listenerTask = null;
            }

            try {
                listenerTask?.Wait(TimeSpan.FromMilliseconds(METAGUIDE_QUEUE_FLUSH_TIMEOUT_MS));
            } catch (Exception) {
            }
        }

        public async Task<bool> Dither(IProgress<ApplicationStatus> progress, CancellationToken ct) {
            if (!Connected) {
                return false;
            }
            if (!IsGuiding) {
                return false;
            }

            var ditherRadius = this.profileService.ActiveProfile.GuiderSettings.DitherPixels / this.PixelScale;
            if (!PostAndCheckMessage("DitherRadius", remoteDitherRadiusMsg, (int)(Math.Round(ditherRadius * 10.0)), 0)) {
                return false;
            }
            if (!PostAndCheckMessage("Dither", remoteDitherMsg, 0, 0)) {
                return false;
            }

            // Give MG some time to process the dither message
            await Task.Delay(TimeSpan.FromSeconds(this.profileService.ActiveProfile.GuiderSettings.MetaGuideDitherSettleSeconds), ct);
            return !ct.IsCancellationRequested && this.IsGuiding;
        }

        public async Task<bool> StartGuiding(bool forceCalibration, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            if (!Connected) {
                return false;
            }

            if (this.profileService.ActiveProfile.GuiderSettings.MetaGuideLockWhenGuiding && !IsLocked) {
                if (!await LockStar(ct)) {
                    return false;
                }
            }

            return await Guide(ct);
        }

        public async Task<bool> StopGuiding(CancellationToken ct) {
            if (!Connected || !this.IsGuiding) {
                return false;
            }

            if (!PostAndCheckMessage("GuideOff", remoteUnguideMsg, 0, 0)) {
                return false;
            }
            if (!PostAndCheckMessage("LockOff", remoteUnlockMsg, 0, 0)) {
                return false;
            }
            return await WaitOnEventChangeCondition(() => !this.IsGuiding && !this.IsLocked, ct);
        }

        public bool CanClearCalibration => false;

        public bool CanGetLockPosition => false;
        public bool CanSetShiftRate => true;

        private bool shiftEnabled;

        public bool ShiftEnabled {
            get => shiftEnabled;
            private set {
                shiftEnabled = value;
                RaisePropertyChanged();
            }
        }

        private SiderealShiftTrackingRate shiftRate = SiderealShiftTrackingRate.Disabled;

        public SiderealShiftTrackingRate ShiftRate {
            get => shiftRate;
            private set {
                shiftRate = value;
                RaisePropertyChanged();
            }
        }

        public Task<bool> ClearCalibration(CancellationToken ct) {
            return Task.FromResult(false);
        }

        private static readonly TimeSpan LOW_INTENSITY_THRESHOLD = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan LOW_INTENSITY_GUIDING_TIMEOUT = TimeSpan.FromSeconds(5);
        private const int MAX_LOW_INTENSITY_GUIDING_RETRIES = 3;
        private DateTime? lowIntensityStart;
        private bool guidingHaltedDueToLowIntensity = false;
        private Task lowIntensityChangeGuidingTask = Task.CompletedTask;
        private volatile int lowIntensityGuidingAttemptCount = 0;

        private void Listener_OnStatus(MetaGuideStatusMsg message) {
            MetaGuideGuideMsg guideMsg;
            lock (this.lockobj) {
                if (!this.Connected && !this.connecting) {
                    return;
                }

                guideMsg = Interlocked.Exchange(ref this.latestUnpublishedGuide, null);
                this.IsLocked = message.Locked;
                this.IsGuiding = message.Guiding;
                this.PixelScale = message.ArcSecPerPixel;
                this.Intensity = message.Intensity;
                this.FWHM = message.FWHM;
                this.Seeing = message.Seeing;
                this.CalibrationState = message.CalibrationState;
                this.FocalLength = message.FocalLength;
                this.PixelSize = message.PixelSize;
                this.latestStatus = message;
            }

            // Guide star intensity is on a scale of 0-255, and can drop if clouds appear. This can cause spurious guide pulses,
            // so add a check to temporarily pause and restore guiding when intensity drops below a configurable threshold
            var minIntensity = profileService.ActiveProfile.GuiderSettings.MetaGuideMinIntensity;
            if (message.Intensity < minIntensity) {
                this.OnLowIntensity((int)message.Intensity, minIntensity);
            } else {
                this.OnAdequateIntensity((int)message.Intensity, minIntensity);
            }

            if (message.Guiding) {
                // GuideEvent expects RADuration in pixel units, and MetaGuide updates are in arcsecs
                var step = new MetaGuideGuideStep() {
                    RADistanceRaw = message.DeltaEastArcsec / this.PixelScale,
                    DECDistanceRaw = message.DeltaNorthArcsec / this.PixelScale,
                    RADuration = (double)(guideMsg?.WestPulse).GetValueOrDefault(0) / 1000.0,
                    DECDuration = (double)(guideMsg?.NorthPulse).GetValueOrDefault(0) / 1000.0,
                };
                GuideEvent?.Invoke(this, step);
            }
            this.metaGuideMessageReceivedEvent.Set();
        }

        private void OnLowIntensity(int currentIntensity, int minIntensity) {
            if (this.IsGuiding) {
                if (lowIntensityStart == null) {
                    lowIntensityStart = DateTime.Now;
                }

                var elapsedTime = DateTime.Now - lowIntensityStart;
                if (elapsedTime >= LOW_INTENSITY_THRESHOLD) {
                    // Stop guiding due to low star intensity
                    if (lowIntensityChangeGuidingTask.Status > TaskStatus.Running) {
                        Logger.Warning($"Star intensity {currentIntensity} lower than {minIntensity} for longer than {LOW_INTENSITY_THRESHOLD}. Stopping guiding until intensity comes back.");
                        Notification.ShowWarning(Loc.Instance["LblMetaGuideLowIntensityStopGuiding"]);
                        lowIntensityChangeGuidingTask = Task.Run(async () => {
                            if (await StopGuiding(new CancellationTokenSource(LOW_INTENSITY_GUIDING_TIMEOUT).Token)) {
                                guidingHaltedDueToLowIntensity = true;
                            } else {
                                Logger.Error("Failed to stop guiding due to low star intensity");
                            }
                        });
                    }
                }
            }
        }

        private void OnAdequateIntensity(int currentIntensity, int minIntensity) {
            lowIntensityStart = null;
            if (guidingHaltedDueToLowIntensity) {
                // Start guiding again due to restored star intensity
                if (lowIntensityChangeGuidingTask.Status > TaskStatus.Running) {
                    if (lowIntensityGuidingAttemptCount == 0) {
                        Logger.Info($"Star intensity {currentIntensity} now above {minIntensity}. Resuming guiding.");
                        Notification.ShowInformation(Loc.Instance["LblMetaGuideLowIntensityRestoreGuiding"]);
                    }

                    ++lowIntensityGuidingAttemptCount;
                    lowIntensityChangeGuidingTask = Task.Run(async () => {
                        if (await StartGuiding(false, default, new CancellationTokenSource(LOW_INTENSITY_GUIDING_TIMEOUT).Token)) {
                            guidingHaltedDueToLowIntensity = false;
                            lowIntensityGuidingAttemptCount = 0;
                        } else if (lowIntensityGuidingAttemptCount >= MAX_LOW_INTENSITY_GUIDING_RETRIES) {
                            Logger.Error("Failed to resume guiding after star intensity recovered");
                            Notification.ShowError(Loc.Instance["LblMetaGuideLowIntensityRestoreGuidingFailed"]);
                            guidingHaltedDueToLowIntensity = false;
                            lowIntensityGuidingAttemptCount = 0;
                        }
                    });
                }
            }
        }

        private void Listener_OnGuide(MetaGuideGuideMsg message) {
            lock (this.lockobj) {
                if (!this.Connected) {
                    return;
                }

                this.latestUnpublishedGuide = message;
                this.IsGuiding = true;
            }
            this.metaGuideMessageReceivedEvent.Set();
        }

        private async Task<bool> WaitOnEventChangeCondition(Func<bool> condition, CancellationToken ct) {
            var connectionAndCallerTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.clientCTS.Token, ct);
            while (!connectionAndCallerTokenSource.IsCancellationRequested) {
                if (condition.Invoke()) {
                    return true;
                }
                await this.metaGuideMessageReceivedEvent.WaitAsync(connectionAndCallerTokenSource.Token);
            }
            return false;
        }

        private static bool PostAndCheckMessage(string messageType, uint msg, int wParam, int lParam) {
            if (!PostMessage(HWND_BROADCAST, msg, wParam, lParam)) {
                Logger.Error($"Failed to post {messageType} message");
                Notification.ShowError($"Failed to post {messageType} message");
                return false;
            }
            return true;
        }

        private void UpdateState() {
            lock (this.lockobj) {
                if (!Connected) {
                    this.State = "Not Ready";
                } else if (this.IsGuiding && this.IsLocked) {
                    this.State = "Locked and Guiding";
                } else if (this.IsGuiding) {
                    this.State = "Guiding";
                } else if (this.IsLocked) {
                    this.State = "Locked";
                } else {
                    this.State = "Connected";
                }
            }
        }

        private async Task<bool> LockStar(CancellationToken ct) {
            if (!PostAndCheckMessage("LockOn", remoteLockMsg, 0, 0)) {
                return false;
            }
            return await WaitOnEventChangeCondition(() => this.IsLocked, ct);
        }

        private async Task<bool> Guide(CancellationToken ct) {
            if (!PostAndCheckMessage("GuideOn", remoteGuideMsg, 0, 0)) {
                return false;
            }
            return await WaitOnEventChangeCondition(() => this.IsGuiding, ct);
        }

        public void SetupDialog() {
            var windowService = windowServiceFactory.Create();
            windowService.ShowDialog(this, Loc.Instance["LblMetaGuideSetup"], System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.SingleBorderWindow);
        }

        public async Task<bool> SetShiftRate(SiderealShiftTrackingRate shiftTrackingRate, CancellationToken ct) {
            if (!shiftTrackingRate.Enabled) {
                return await StopShifting(ct);
            }
            ShiftRate = shiftTrackingRate;
            double raArcsecPerHour = shiftTrackingRate.RAArcsecsPerHour;
            double decArcsecPerHour = shiftTrackingRate.DecArcsecsPerHour;
            Logger.Info($"Setting shift rate to RA={raArcsecPerHour}, Dec={decArcsecPerHour}");
            if (!PostAndCheckMessage("SetShiftRate", remoteSetShift, (int)(10000 + Math.Round(raArcsecPerHour)), (int)(10000 + Math.Round(decArcsecPerHour)))) {
                Logger.Error($"Failed to set lock shift rate");
                return false;
            }
            if (!PostAndCheckMessage("StartShifting", remoteShift, 0, 0)) {
                Logger.Error($"Failed to enable lock shift");
                return false;
            }
            ShiftEnabled = true;
            return true;
        }

        public async Task<bool> StopShifting(CancellationToken ct) {
            if (!PostAndCheckMessage("StopShifting", remoteUnShift, 0, 0)) {
                return false;
            }
            ShiftEnabled = false;
            return true;
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

    public class MetaGuideGuideStep : IGuideStep {
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
            return (MetaGuideGuideStep)this.MemberwiseClone();
        }
    }
}