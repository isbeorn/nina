using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using NINA.Model.MyTelescope;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

#pragma warning disable 1998

namespace NINA.Model.MyGuider {

    public class DirectGuider : BaseINPC, IGuider, ITelescopeConsumer {
        private IProfileService profileService;
        private ITelescopeMediator telescopeMediator;

        public DirectGuider(IProfileService profileService, ITelescopeMediator telescopeMediator) {
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
            this.telescopeMediator.RegisterConsumer(this);
        }

        public string Name => "Direct Guider";

        private TelescopeInfo telescopeInfo = DeviceInfo.CreateDefaultInstance<TelescopeInfo>();

        public void UpdateDeviceInfo(TelescopeInfo telescopeInfo) {
            this.telescopeInfo = telescopeInfo;
        }

        private bool _connected;

        public bool Connected {
            get {
                if (_connected) {
                    if (telescopeInfo.Connected) {
                        return true;
                    }
                    else {
                        Notification.ShowWarning(Locale.Loc.Instance["LblDirectGuiderTelescopeDisconnect"]);
                        Logger.Trace("Telescope is disconnected. Direct Guide will disconnect. Dither will not occur.");
                        return Disconnect();
                    }
                }
                return false;
            }
            set {
                _connected = value;
                RaisePropertyChanged();
            }
        }

        public double PixelScale { get; set; }

        private string _state = "Idle";

        public string State {
            get {
                return _state;
            }
            set {
                _state = value;
                RaisePropertyChanged();
            }
        }

        public IGuideStep GuideStep { get; }

        public async Task<bool> Connect() {
            Connected = false;
            if (telescopeInfo.Connected) {
                Connected = true;
            }
            else {
                var telescopeConnect = await telescopeMediator.Connect();
                if (telescopeConnect) {
                    Connected = true;
                }
                else {
                    Notification.ShowWarning(Locale.Loc.Instance["LblDirectGuiderConnectionFail"]);
                    Connected = false;
                }
            }

            return Connected;
        }

        public async Task<bool> AutoSelectGuideStar() {
            return true;
        }

        public bool Disconnect() {
            Connected = false;

            return Connected;
        }

        public async Task<bool> Pause(bool pause, CancellationToken ct) {
            return true;
        }

        public async Task<bool> StartGuiding(CancellationToken ct) {
            return true;
        }

        public async Task<bool> StopGuiding(CancellationToken ct) {
            return true;
        }

        public async Task<bool> Dither(CancellationToken ct) {
            State = "Dithering...";

            int Duration = profileService.ActiveProfile.GuiderSettings.DirectGuideDuration * 1000;

            bool DitherRAOnly = profileService.ActiveProfile.GuiderSettings.DitherRAOnly;
            int SettleTime = profileService.ActiveProfile.GuiderSettings.SettleTime * 1000;

            //In theory should not be hit as guider gets disconnected when telescope disconnects
            if (!telescopeInfo.Connected) {
                return false;
            }
            else {
                if (!DitherRAOnly) {
                    var PulseInstructions = SelectDitherPulse(Duration);
                    telescopeMediator.PulseGuide(PulseInstructions.Item1, PulseInstructions.Item2);
                    await Utility.Utility.Delay(TimeSpan.FromMilliseconds(PulseInstructions.Item2), ct);
                    telescopeMediator.PulseGuide(PulseInstructions.Item3, PulseInstructions.Item4);
                    await Utility.Utility.Delay(TimeSpan.FromMilliseconds(PulseInstructions.Item4), ct);
                    await Utility.Utility.Delay(TimeSpan.FromMilliseconds(SettleTime), ct);
                }
                else {
                    GuideDirections direction = GuideDirections.guideWest;
                    Random random = new Random();
                    bool raDirection = random.NextDouble() >= 0.5;
                    if (raDirection) {
                        direction = GuideDirections.guideEast;
                    }
                    //Adjust Pulse Duration for RA only dithering. Otherwise RA only dithering will likely provide terrible results.
                    Duration = (int)Math.Round(Duration * (0.5 + random.NextDouble()));
                    telescopeMediator.PulseGuide(direction, Duration);
                    await Utility.Utility.Delay(TimeSpan.FromMilliseconds(Duration), ct);
                    await Utility.Utility.Delay(TimeSpan.FromMilliseconds(SettleTime), ct);
                }
            }
            State = "Idle";
            return true;
        }

        /// <summary>
        /// This function will figure out what guiding pulses to send to the mount to achieve
        /// a random guide direction equivalent to a total guide pulse duration set by the user.
        /// Note that total guide pulse duration sent to mount will be more than the guide
        /// pulse duration set by the user, but overall distance from origin will be the same
        /// as if the pulse had been fully applied to one of the N/S/W/E axes.
        /// </summary>
        /// <param name="duration">Rather than a time, should be considered as actual distance from origin prior to dither</param>
        /// <returns>Parameters for two guide pulses, one in N/S direction and one in E/W direction</returns>

        private (GuideDirections, int, GuideDirections, int) SelectDitherPulse(int duration) {
            Random random = new Random();
            double ditherAngle = random.NextDouble() * 2 * Math.PI;
            double cosAngle = Math.Cos(ditherAngle);
            double sinAngle = Math.Sin(ditherAngle);
            GuideDirections direction1;
            GuideDirections direction2;

            if (cosAngle >= 0) {
                direction1 = GuideDirections.guideEast;
            }
            else {
                direction1 = GuideDirections.guideWest;
            }

            if (sinAngle >= 0) {
                direction2 = GuideDirections.guideNorth;
            }
            else {
                direction2 = GuideDirections.guideSouth;
            }

            int duration1 = (int)Math.Round(Math.Abs(duration * cosAngle));
            int duration2 = (int)Math.Round(Math.Abs(duration * sinAngle));

            return (direction1, duration1, direction2, duration2);
        }
    }
}