#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.INDI.Protocol;
using NINA.INDI.Interfaces;
using NINA.Core.Enum;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NINA.INDI.Enums;
using NINA.Astrometry;

namespace NINA.INDI.Devices {

    public class INDITelescope : INDIDevice, IINDITelescope {






        public override void OnTextPropertyUpdated(INDITextProperty p) {
            base.OnTextPropertyUpdated(p);
        }

        public override void OnNumberPropertyUpdated(INDINumberProperty p) {
            base.OnNumberPropertyUpdated(p);
        }

        public override void OnSwitchPropertyUpdated(INDISwitchProperty p) {
            base.OnSwitchPropertyUpdated(p);
        }



        /// <summary>
        /// Specify critical properties that must arrive before Connect() completes
        /// </summary>
        protected override string[] GetRequiredConnectionProperties() {
            return ["TELESCOPE_TRACK_MODE"];
        }


        public INDITelescope(INDIDeviceInfo device) : base(device) {
        }

        public AlignmentMode AlignmentMode { get; }
        public double Altitude => GetNumberPropertyValue("HORIZONTAL_COORD", "ALT") ?? double.NaN;
        public double ApertureArea => ApertureDiameter * ApertureDiameter * 0.25 * Math.PI;
        public double ApertureDiameter => GetNumberPropertyValue("TELESCOPE_INFO", "TELESCOPE_APERTURE") ?? double.NaN;
        public bool AtHome { get; }
        public bool AtPark => (bool)GetSwitchPropertyValue("TELESCOPE_PARK", "PARK");
        public double Azimuth => GetNumberPropertyValue("HORIZONTAL_COORD", "AZ") ?? double.NaN;
        public double Declination => GetNumberPropertyValue("EQUATORIAL_EOD_COORD", "DEC") ?? double.NaN;
        public double DeclinationRate { get; set; }
        public bool DoesRefraction { get; }
        public double FocalLength => GetNumberPropertyValue("TELESCOPE_INFO", "TELESCOPE_FOCAL_LENGTH") ?? double.NaN;
        public double GuideRateDeclination { get; }
        public double GuideRateRightAscension { get; }
        public bool IsPulseGuiding { get; }
        public double RightAscension => GetNumberPropertyValue("EQUATORIAL_EOD_COORD", "RA") ?? double.NaN;
        public double RightAscensionRate { get; set; }
        public PierSide SideOfPier => (bool)GetSwitchPropertyValue("TELESCOPE_PIER_SIDE", "PIER_EAST") ? PierSide.pierEast : PierSide.pierWest;
        public double SiderealTime {
            get {
                double? lst = GetNumberPropertyValue("TIME_LST", "LST");
                if (lst.HasValue) {
                    return lst.Value;
                }

                Logger.Debug("Mount does not supply sidereal time, falling back client computation");
                return AstroUtil.GetLocalSiderealTimeNow(SiteLongitude);
            }
        }
        public double SiteElevation {
            get => GetNumberPropertyValue("GEOGRAPHIC_COORD", "ELEV") ?? double.NaN;
            set {
                SetNumberValue("GEOGRAPHIC_COORD", "ELEV", value);
            }
        }
        public double SiteLatitude {
            get => GetNumberPropertyValue("GEOGRAPHIC_COORD", "LAT") ?? double.NaN;
            set {
                SetNumberValue("GEOGRAPHIC_COORD", "LAT", value);
            }
        }
        public double SiteLongitude {
            get => GetNumberPropertyValue("GEOGRAPHIC_COORD", "LONG") ?? double.NaN;
            set {
                SetNumberValue("GEOGRAPHIC_COORD", "LONG", value);
            }
        }
        public int SlewSettleTime { get; }
        public bool Slewing {
            get {
                bool motionWest = (bool)GetSwitchPropertyValue("TELESCOPE_MOTION_WE", "MOTION_WEST");
                bool motionEast = (bool)GetSwitchPropertyValue("TELESCOPE_MOTION_WE", "MOTION_EAST");
                bool motionNorth = (bool)GetSwitchPropertyValue("TELESCOPE_MOTION_NS", "MOTION_NORTH");
                bool motionSouth = (bool)GetSwitchPropertyValue("TELESCOPE_MOTION_NS", "MOTION_SOUTH");
                bool motionRaDec = GetProperty("EQUATORIAL_EOD_COORD")?.State == PropertyState.Busy;
                bool motionAltAz = GetProperty("HORIZONTAL_COORD")?.State == PropertyState.Busy;
                return motionWest || motionEast || motionNorth || motionSouth || motionRaDec || motionAltAz;
            }
        }
        public double TargetDeclination { get; }
        public double TargetRightAscension { get; }
        public bool Tracking {
            get => (bool)GetSwitchPropertyValue("TELESCOPE_TRACK_STATE", "TRACK_ON");
            set {
                // Use SetSwitchProperty to respect OneOfMany rule
                var switchValues = new Dictionary<string, bool> {
                    { "TRACK_ON", value },
                    { "TRACK_OFF", !value }
                };
                SetSwitchProperty("TELESCOPE_TRACK_STATE", switchValues);
            }
        }

        /// <summary>
        /// Set the telescope tracking mode (Sidereal, Lunar, Solar, Custom)
        /// Uses SetSwitchProperty to respect the OneOfMany rule for TELESCOPE_TRACK_MODE
        /// </summary>
        /// <param name="mode">Tracking mode: 0=Sidereal, 1=Lunar, 2=Solar, 3=King/Custom, 5=Stopped</param>
        public void SetTrackingMode(TrackingMode mode) {
            // If Stopped, just turn tracking off without modifying TELESCOPE_TRACK_MODE
            if (mode == TrackingMode.Stopped) {
                Tracking = false;
                return;
            }

            // Build the switch values dictionary based on the desired tracking mode
            var switchValues = new Dictionary<string, bool>();

            switch (mode) {
                case TrackingMode.Sidereal:
                    switchValues["TRACK_SIDEREAL"] = true;
                    switchValues["TRACK_LUNAR"] = false;
                    switchValues["TRACK_SOLAR"] = false;
                    switchValues["TRACK_CUSTOM"] = false;
                    break;
                case TrackingMode.Lunar:
                    switchValues["TRACK_SIDEREAL"] = false;
                    switchValues["TRACK_LUNAR"] = true;
                    switchValues["TRACK_SOLAR"] = false;
                    switchValues["TRACK_CUSTOM"] = false;
                    break;
                case TrackingMode.Solar:
                    switchValues["TRACK_SIDEREAL"] = false;
                    switchValues["TRACK_LUNAR"] = false;
                    switchValues["TRACK_SOLAR"] = true;
                    switchValues["TRACK_CUSTOM"] = false;
                    break;
                case TrackingMode.King:
                case TrackingMode.Custom:
                    switchValues["TRACK_SIDEREAL"] = false;
                    switchValues["TRACK_LUNAR"] = false;
                    switchValues["TRACK_SOLAR"] = false;
                    switchValues["TRACK_CUSTOM"] = true;
                    break;
            }

            // Use SetSwitchProperty to respect OneOfMany rule
            SetSwitchProperty("TELESCOPE_TRACK_MODE", switchValues);
            // Turn tracking on after setting the mode
            Tracking = true;
        }

        /// <summary>
        /// Get the current tracking mode from the TELESCOPE_TRACK_MODE property
        /// </summary>
        /// <returns>The current tracking mode</returns>
        public TrackingMode GetTrackingMode() {
            // If tracking is off, return Stopped
            if (!Tracking) {
                return TrackingMode.Stopped;
            }

            try {
                // Check which tracking mode switch is active
                if (GetSwitchPropertyValue("TELESCOPE_TRACK_MODE", "TRACK_SIDEREAL") == true) {
                    return TrackingMode.Sidereal;
                } else if (GetSwitchPropertyValue("TELESCOPE_TRACK_MODE", "TRACK_LUNAR") == true) {
                    return TrackingMode.Lunar;
                } else if (GetSwitchPropertyValue("TELESCOPE_TRACK_MODE", "TRACK_SOLAR") == true) {
                    return TrackingMode.Solar;
                } else if (GetSwitchPropertyValue("TELESCOPE_TRACK_MODE", "TRACK_CUSTOM") == true) {
                    return TrackingMode.King;
                }
            } catch (ArgumentException) {
                throw new NotImplementedException();
            }

            // Default to Sidereal if we can't determine the mode
            return TrackingMode.Sidereal;
        }

        /// <summary>
        /// Get the list of supported tracking modes from the TELESCOPE_TRACK_MODE property
        /// </summary>
        /// <returns>List of supported tracking modes</returns>
        public IList<TrackingMode> GetSupportedTrackingModes() {
            var modes = new List<TrackingMode>();

            // Sidereal is always supported
            modes.Add(TrackingMode.Sidereal);

            try {
                var trackModeProperty = GetSwitchProperty("TELESCOPE_TRACK_MODE");
                if (trackModeProperty != null) {
                    foreach (var sw in trackModeProperty.Switches) {
                        switch (sw.Name) {
                            case "TRACK_LUNAR":
                                modes.Add(TrackingMode.Lunar);
                                break;
                            case "TRACK_SOLAR":
                                modes.Add(TrackingMode.Solar);
                                break;
                            case "TRACK_CUSTOM":
                                modes.Add(TrackingMode.King);
                                break;
                        }
                    }
                }
            } catch (ArgumentException) {
                throw new NotImplementedException();
            }

            // Stopped is always available
            modes.Add(TrackingMode.Stopped);

            return modes;
        }

        public DateTime UTCDate { get; set; }


        public void AbortSlew() {
            try {
                SetSwitchValue("TELESCOPE_ABORT_MOTION", "ABORT_MOTION", true);
            } catch (ArgumentException) {
                throw new NotImplementedException();
            }
        }

        public IAxisRates AxisRates(TelescopeAxes axis) {
            try {
                // Try to get the TELESCOPE_MOTION_RATE property to find min/max values
                var motionRateProperty = GetNumberProperty("TELESCOPE_MOTION_RATE");
                if (motionRateProperty != null) {
                    var motionRateElement = motionRateProperty.Numbers.FirstOrDefault(n => n.Name == "MOTION_RATE");
                    if (motionRateElement != null) {
                        return new INDIAxisRates(motionRateElement.Min, motionRateElement.Max);
                    }
                }
            } catch (Exception) {
                // Property doesn't exist or error accessing it
            }

            // Return a default range if we can't get the property
            // Most INDI drivers support 0.0 to 1.0 (sidereal rate multiplier)
            return new INDIAxisRates(0.0, 1.0);
        }

        public void ConfigureJNOW() {
            try {
                // INDI drivers may support different coordinate properties:
                // - EQUATORIAL_EOD_COORD: Epoch of Date (JNOW)
                // - EQUATORIAL_COORD: J2000
                // We want to ensure we're using EOD (JNOW)

                // Some drivers have TELESCOPE_EQUATORIAL_COORD property to select which system
                // Try to set it to EOD if available
                try {
                    var eqCoordProp = GetSwitchProperty("TELESCOPE_EQUATORIAL_COORD");
                    if (eqCoordProp != null) {
                        Logger.Info("Setting TELESCOPE_EQUATORIAL_COORD to EOD (JNOW)");
                        SetSwitchValue("TELESCOPE_EQUATORIAL_COORD", "EOD", true);
                    }
                } catch (ArgumentException) {
                    // Property doesn't exist, that's okay
                }

                Logger.Info("INDI configured to use EQUATORIAL_EOD_COORD (JNOW)");
            } catch (Exception ex) {
                Logger.Warning($"Could not configure JNOW: {ex.Message}");
            }
        }

        public void MoveAxis(TelescopeAxes axis, double rate) {
            try {
                // Rate is in degrees per second, sign indicates direction
                // Per NINA convention: 
                // Primary: negative=West, positive=East
                // Secondary: positive=North, negative=South
                double absRate = Math.Abs(rate);

                Logger.Debug($"INDITelescope.MoveAxis: axis={axis}, rate={rate}, absRate={absRate}");

                switch (axis) {
                    case TelescopeAxes.Primary:
                        // Primary axis is RA/Azimuth - use West/East motion
                        if (rate != 0) {
                            // Set the motion rate if the property exists
                            try {
                                SetNumberValue("TELESCOPE_MOTION_RATE", "MOTION_RATE", absRate);
                            } catch (ArgumentException) {
                                // Property doesn't exist, telescope will use default rate
                            }

                            // Set the direction switch
                            var prop = GetSwitchProperty("TELESCOPE_MOTION_WE");
                            if (prop != null) {
                                if (rate < 0) {
                                    // Negative rate = West
                                    Logger.Info($"Setting TELESCOPE_MOTION_WE: MOTION_WEST=true (rate={rate})");
                                    foreach (var sw in prop.Switches) {
                                        sw.Value = (sw.Name == "MOTION_WEST");
                                    }
                                } else {
                                    // Positive rate = East
                                    Logger.Info($"Setting TELESCOPE_MOTION_WE: MOTION_EAST=true (rate={rate})");
                                    foreach (var sw in prop.Switches) {
                                        sw.Value = (sw.Name == "MOTION_EAST");
                                    }
                                }
                                INDIClient.Instance.SendProperty(prop);
                            }
                        } else {
                            // Stop motion - set both to false
                            Logger.Info($"Stopping TELESCOPE_MOTION_WE (rate={rate})");
                            var prop = GetSwitchProperty("TELESCOPE_MOTION_WE");
                            if (prop != null) {
                                foreach (var sw in prop.Switches) {
                                    sw.Value = false;
                                }
                                INDIClient.Instance.SendProperty(prop);
                            }
                        }
                        break;
                    case TelescopeAxes.Secondary:
                        // Secondary axis is Dec/Altitude - use North/South motion
                        if (rate != 0) {
                            // Set the motion rate if the property exists
                            try {
                                SetNumberValue("TELESCOPE_MOTION_RATE", "MOTION_RATE", absRate);
                            } catch (ArgumentException) {
                                // Property doesn't exist, telescope will use default rate
                            }

                            // Set the direction switch
                            var prop = GetSwitchProperty("TELESCOPE_MOTION_NS");
                            if (prop != null) {
                                if (rate > 0) {
                                    // Positive rate = North
                                    Logger.Info($"Setting TELESCOPE_MOTION_NS: MOTION_NORTH=true (rate={rate})");
                                    foreach (var sw in prop.Switches) {
                                        sw.Value = (sw.Name == "MOTION_NORTH");
                                    }
                                } else {
                                    // Negative rate = South
                                    Logger.Info($"Setting TELESCOPE_MOTION_NS: MOTION_SOUTH=true (rate={rate})");
                                    foreach (var sw in prop.Switches) {
                                        sw.Value = (sw.Name == "MOTION_SOUTH");
                                    }
                                }
                                INDIClient.Instance.SendProperty(prop);
                            }
                        } else {
                            // Stop motion - set both to false
                            Logger.Info($"Stopping TELESCOPE_MOTION_NS (rate={rate})");
                            var prop = GetSwitchProperty("TELESCOPE_MOTION_NS");
                            if (prop != null) {
                                foreach (var sw in prop.Switches) {
                                    sw.Value = false;
                                }
                                INDIClient.Instance.SendProperty(prop);
                            }
                        }
                        break;
                }
            } catch (ArgumentException) {
                throw new NotImplementedException();
            }
        }

        public void PulseGuide(GuideDirections direction, int duration) {
            try {
                switch (direction) {
                    case GuideDirections.guideNorth:
                        SetNumberValue("TELESCOPE_TIMED_GUIDE_NS", "TIMED_GUIDE_N", duration);
                        break;
                    case GuideDirections.guideSouth:
                        SetNumberValue("TELESCOPE_TIMED_GUIDE_NS", "TIMED_GUIDE_S", duration);
                        break;
                    case GuideDirections.guideWest:
                        SetNumberValue("TELESCOPE_TIMED_GUIDE_WE", "TIMED_GUIDE_W", duration);
                        break;
                    case GuideDirections.guideEast:
                        SetNumberValue("TELESCOPE_TIMED_GUIDE_WE", "TIMED_GUIDE_E", duration);
                        break;
                }
            } catch (ArgumentException) {
                throw new NotImplementedException();
            }
        }

        public async Task ParkAsync(CancellationToken ct = default) {
            try {
                SetSwitchValue("TELESCOPE_PARK", "PARK", true);

                // Wait for property to become busy then return to idle/ok
                await Task.Delay(100, ct);

                var parkProp = GetProperty("TELESCOPE_PARK");
                while (parkProp?.State == PropertyState.Busy && !ct.IsCancellationRequested) {
                    await Task.Delay(200, ct);
                    parkProp = GetProperty("TELESCOPE_PARK");
                }
            } catch (ArgumentException) {
                throw new NotImplementedException();
            }
        }

        public async Task UnparkAsync(CancellationToken ct = default) {
            try {
                SetSwitchValue("TELESCOPE_PARK", "UNPARK", true);

                // Wait for property to become busy then return to idle/ok
                await Task.Delay(100, ct);

                var parkProp = GetProperty("TELESCOPE_PARK");
                while (parkProp?.State == PropertyState.Busy && !ct.IsCancellationRequested) {
                    await Task.Delay(200, ct);
                    parkProp = GetProperty("TELESCOPE_PARK");
                }
            } catch (ArgumentException) {
                throw new NotImplementedException();
            }
        }

        public void SetPark() {
            try {
                SetSwitchValue("TELESCOPE_PARK_OPTION", "PARK_CURRENT", true);
            } catch (ArgumentException) {
                throw new NotImplementedException();
            }
        }

        public void SlewToCoordinates(double ra, double dec) {
            try {
                SetNumberValue("EQUATORIAL_EOD_COORD", "RA", ra);
                SetNumberValue("EQUATORIAL_EOD_COORD", "DEC", dec);
            } catch (ArgumentException) {
                throw new NotImplementedException();
            }
        }

        public async Task SlewToCoordinatesTaskAsync(double ra, double dec, CancellationToken ct = default) {
            try {
                SetNumberValue("EQUATORIAL_EOD_COORD", "RA", ra);
                SetNumberValue("EQUATORIAL_EOD_COORD", "DEC", dec);

                // Wait a bit for the slew to start
                await Task.Delay(100, ct);

                while (Slewing && !ct.IsCancellationRequested) {
                    await Task.Delay(200, ct);
                }
            } catch (ArgumentException) {
                throw new NotImplementedException();
            }
        }

        public void SlewToAltAz(double azimuth, double altitude) {
            try {
                SetNumberValue("HORIZONTAL_COORD", "ALT", altitude);
                SetNumberValue("HORIZONTAL_COORD", "AZ", azimuth);
            } catch (ArgumentException) {
                throw new NotImplementedException();
            }
        }

        public async Task SlewToAltAzTaskAsync(double azimuth, double altitude, CancellationToken cancellationToken = default) {
            try {
                SetNumberValue("HORIZONTAL_COORD", "ALT", altitude);
                SetNumberValue("HORIZONTAL_COORD", "AZ", azimuth);

                // Wait a bit for the slew to start
                await Task.Delay(100, cancellationToken);

                while (Slewing && !cancellationToken.IsCancellationRequested) {
                    await Task.Delay(200, cancellationToken);
                }
            } catch (ArgumentException) {
                throw new NotImplementedException();
            }
        }

        public void SyncToCoordinates(double ra, double dec) {
            try {
                SetNumberValue("ON_COORD_SET", "SYNC", 1);
                SetNumberValue("EQUATORIAL_EOD_COORD", "RA", ra);
                SetNumberValue("EQUATORIAL_EOD_COORD", "DEC", dec);
                // Reset to slew mode after sync
                SetNumberValue("ON_COORD_SET", "SLEW", 1);
            } catch (ArgumentException) {
                throw new NotImplementedException();
            }
        }

        public async Task FindHomeAsync(CancellationToken ct = default) {
            try {
                SetSwitchValue("TELESCOPE_HOME", "FIND", true);

                // Wait for property to become busy then return to idle/ok
                await Task.Delay(100, ct);

                var homeProp = GetProperty("TELESCOPE_HOME");
                while (homeProp?.State == NINA.INDI.Enums.PropertyState.Busy && !ct.IsCancellationRequested) {
                    await Task.Delay(200, ct);
                    homeProp = GetProperty("TELESCOPE_HOME");
                }
            } catch (ArgumentException) {
                throw new NotImplementedException();
            }
        }

        public bool CanMoveAxis(TelescopeAxes axis) {
            try {
                // Check if motion properties exist
                switch (axis) {
                    case TelescopeAxes.Primary:
                        GetProperty("TELESCOPE_MOTION_WE");
                        return true;
                    case TelescopeAxes.Secondary:
                        GetProperty("TELESCOPE_MOTION_NS");
                        return true;
                    default:
                        return false;
                }
            } catch {
                return false;
            }
        }

        public PierSide DestinationSideOfPier(double ra, double dec) {
            // INDI doesn't provide a standard way to predict pier side
            // Return unknown/current pier side as best guess
            return SideOfPier;
        }

        #region Unsupported

        public IList<string> SupportedActions { get; }

        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }

        public void CommandBlind(string command, bool raw = false) {
            throw new NotImplementedException();
        }

        public bool CommandBool(string command, bool raw = false) {
            throw new NotImplementedException();
        }

        public string CommandString(string command, bool raw = false) {
            throw new NotImplementedException();
        }

        #endregion
    }
}
