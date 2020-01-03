#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Profile;
using NINA.Model.MyTelescope;
using System;
using System.Threading;
using System.Threading.Tasks;

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

        public string Id => "Direct_Guider";

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
                    } else {
                        Notification.ShowWarning(Locale.Loc.Instance["LblDirectGuiderTelescopeDisconnect"]);
                        Logger.Warning("Telescope is disconnected. Direct Guide will disconnect. Dither will not occur.");
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

        public async Task<bool> Connect() {
            Connected = false;
            if (telescopeInfo.Connected) {
                Connected = true;
            } else {
                var telescopeConnect = await telescopeMediator.Connect();
                if (telescopeConnect) {
                    Connected = true;
                } else {
                    Notification.ShowWarning(Locale.Loc.Instance["LblDirectGuiderConnectionFail"]);
                    Connected = false;
                }
            }

            return Connected;
        }

        public Task<bool> AutoSelectGuideStar() {
            return Task.FromResult(true);
        }

        public bool Disconnect() {
            Connected = false;

            return Connected;
        }

        public Task<bool> Pause(bool pause, CancellationToken ct) {
            return Task.FromResult(true);
        }

        public Task<bool> StartGuiding(CancellationToken ct) {
            return Task.FromResult(true);
        }

        public Task<bool> StopGuiding(CancellationToken ct) {
            return Task.FromResult(true);
        }

        private Random random = new Random();

        private double previousAngle = 0;

        public event EventHandler<IGuideStep> GuideEvent;

        public async Task<bool> Dither(CancellationToken ct) {
            State = "Dithering...";

            TimeSpan Duration = TimeSpan.FromSeconds(profileService.ActiveProfile.GuiderSettings.DirectGuideDuration);
            TimeSpan SettleTime = TimeSpan.FromSeconds(profileService.ActiveProfile.GuiderSettings.SettleTime);

            bool DitherRAOnly = profileService.ActiveProfile.GuiderSettings.DitherRAOnly;

            //In theory should not be hit as guider gets disconnected when telescope disconnects
            if (!telescopeInfo.Connected) {
                return false;
            } else {
                GuidePulses PulseInstructions = SelectDitherPulse(Duration);
                if (!DitherRAOnly) {
                    telescopeMediator.PulseGuide(PulseInstructions.directionWestEast, (int)PulseInstructions.durationWestEast.TotalMilliseconds);
                    await Utility.Utility.Delay(PulseInstructions.durationWestEast, ct);
                    telescopeMediator.PulseGuide(PulseInstructions.directionNorthSouth, (int)PulseInstructions.durationNorthSouth.TotalMilliseconds);
                    await Utility.Utility.Delay(PulseInstructions.durationNorthSouth, ct);
                    await Utility.Utility.Delay(SettleTime, ct);
                } else {
                    //Adjust Pulse Duration for RA only dithering. Otherwise RA only dithering will likely provide terrible results.
                    Duration = TimeSpan.FromMilliseconds((int)Math.Round(Duration.TotalMilliseconds * (0.5 + random.NextDouble())));
                    telescopeMediator.PulseGuide(PulseInstructions.directionWestEast, (int)Duration.TotalMilliseconds);
                    await Utility.Utility.Delay(Duration, ct);
                    await Utility.Utility.Delay(SettleTime, ct);
                }
            }
            State = "Idle";
            return true;
        }

        private struct GuidePulses {
            public GuideDirections directionWestEast;
            public GuideDirections directionNorthSouth;
            public TimeSpan durationWestEast;
            public TimeSpan durationNorthSouth;
        }

        /// <summary>
        /// This function will figure out what guiding pulses to send to the mount to achieve
        /// a random guide direction equivalent to a total guide pulse duration set by the user.
        /// Note that total guide pulse duration sent to mount will be more than the guide
        /// pulse duration set by the user, but overall distance from origin will be the same
        /// as if the pulse had been fully applied to one of the N/S/W/E axes. Guide directions are
        /// set to be roughly countering one another, to avoid too much deviation from target.
        /// </summary>
        /// <param name="duration">Rather than a time, should be considered as actual distance from origin prior to dither</param>
        /// <returns>Parameters for two guide pulses, one in N/S direction and one in E/W direction</returns>

        private GuidePulses SelectDitherPulse(TimeSpan duration) {
            double ditherAngle = (previousAngle + Math.PI) + random.NextDouble() * Math.PI - Math.PI / 2;
            previousAngle = ditherAngle;
            double cosAngle = Math.Cos(ditherAngle);
            double sinAngle = Math.Sin(ditherAngle);
            GuidePulses resultPulses = new GuidePulses();

            if (cosAngle >= 0) {
                resultPulses.directionWestEast = GuideDirections.guideEast;
            } else {
                resultPulses.directionWestEast = GuideDirections.guideWest;
            }

            if (sinAngle >= 0) {
                resultPulses.directionNorthSouth = GuideDirections.guideNorth;
            } else {
                resultPulses.directionNorthSouth = GuideDirections.guideSouth;
            }

            resultPulses.durationWestEast = TimeSpan.FromMilliseconds((int)Math.Round(Math.Abs(duration.TotalMilliseconds * cosAngle)));
            resultPulses.durationNorthSouth = TimeSpan.FromMilliseconds((int)Math.Round(Math.Abs(duration.TotalMilliseconds * sinAngle)));

            return resultPulses;
        }

        public void Dispose() {
            this.telescopeMediator.RemoveConsumer(this);
        }
    }
}