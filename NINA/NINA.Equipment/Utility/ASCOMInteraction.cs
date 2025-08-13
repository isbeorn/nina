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
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyDome;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Equipment.MySafetyMonitor;
using NINA.Equipment.Equipment.MySwitch;
using NINA.Equipment.Equipment.MySwitch.Ascom;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Equipment.MyWeatherData;
using NINA.Equipment.Interfaces;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NINA.Equipment.Utility {

    public class ASCOMInteraction {
        private readonly IProfileService profileService;

        public ASCOMInteraction( IProfileService profileService) {
            this.profileService = profileService;
        }

        public List<ICamera> GetCameras(IExposureDataFactory exposureDataFactory) {
            var l = new List<ICamera>();
            foreach (ASCOM.Com.ASCOMRegistration device in ASCOM.Com.Profile.GetDrivers(ASCOM.Common.DeviceTypes.Camera)) {
                try {
                    AscomCamera cam = new AscomCamera(device.ProgID, device.Name + " (ASCOM)", profileService, exposureDataFactory);
                    Logger.Trace(string.Format("Adding {0}", cam.Name));
                    l.Add(cam);
                } catch (Exception) {
                    //only add cameras which are supported. e.g. x86 drivers will not work in x64
                }
            }
            return l;
        }

        public List<ITelescope> GetTelescopes() {
            var l = new List<ITelescope>();
                foreach (ASCOM.Com.ASCOMRegistration device in ASCOM.Com.Profile.GetDrivers(ASCOM.Common.DeviceTypes.Telescope)) {
                    try {
                        AscomTelescope telescope = new AscomTelescope(device.ProgID, device.Name, profileService);
                        l.Add(telescope);
                    } catch (Exception) {
                        //only add telescopes which are supported. e.g. x86 drivers will not work in x64
                    }
                }
                return l;
        }

        public List<IFilterWheel> GetFilterWheels() {
            var l = new List<IFilterWheel>();
                foreach (ASCOM.Com.ASCOMRegistration device in ASCOM.Com.Profile.GetDrivers(ASCOM.Common.DeviceTypes.FilterWheel)) {
                    try {
                        AscomFilterWheel fw = new AscomFilterWheel(device.ProgID, device.Name, profileService);
                        l.Add(fw);
                    } catch (Exception) {
                        //only add filter wheels which are supported. e.g. x86 drivers will not work in x64
                    }
                }
                return l;
        }

        public List<IRotator> GetRotators() {
            var l = new List<IRotator>();
                foreach (ASCOM.Com.ASCOMRegistration device in ASCOM.Com.Profile.GetDrivers(ASCOM.Common.DeviceTypes.Rotator)) {
                    try {
                        AscomRotator rotator = new AscomRotator(device.ProgID, device.Name);
                        l.Add(rotator);
                    } catch (Exception) {
                        //only add filter wheels which are supported. e.g. x86 drivers will not work in x64
                    }
                }
                return l;
        }

        public List<ISafetyMonitor> GetSafetyMonitors() {
            var l = new List<ISafetyMonitor>();
                foreach (ASCOM.Com.ASCOMRegistration device in ASCOM.Com.Profile.GetDrivers(ASCOM.Common.DeviceTypes.SafetyMonitor)) {
                    try {
                        AscomSafetyMonitor safetyMonitor = new AscomSafetyMonitor(device.ProgID, device.Name);
                        l.Add(safetyMonitor);
                    } catch (Exception) {
                        //only add filter wheels which are supported. e.g. x86 drivers will not work in x64
                    }
                }
                return l;
        }

        public List<IFocuser> GetFocusers() {
            var l = new List<IFocuser>();
                foreach (ASCOM.Com.ASCOMRegistration device in ASCOM.Com.Profile.GetDrivers(ASCOM.Common.DeviceTypes.Focuser)) {
                    try {
                        AscomFocuser focuser = new AscomFocuser(device.ProgID, device.Name);
                        l.Add(focuser);
                    } catch (Exception) {
                        //only add filter wheels which are supported. e.g. x86 drivers will not work in x64
                    }
                }
                return l;
        }

        public List<ISwitchHub> GetSwitches() {
            var l = new List<ISwitchHub>();
            foreach (ASCOM.Com.ASCOMRegistration device in ASCOM.Com.Profile.GetDrivers(ASCOM.Common.DeviceTypes.Switch)) {
                try {
                    AscomSwitchHub ascomSwitch = new AscomSwitchHub(device.ProgID, device.Name);
                    l.Add(ascomSwitch);
                } catch (Exception) {
                    //only add filter wheels which are supported. e.g. x86 drivers will not work in x64
                }
            }
            return l;
        }

        public List<IWeatherData> GetWeatherDataSources() {
            var l = new List<IWeatherData>();

            foreach (ASCOM.Com.ASCOMRegistration device in ASCOM.Com.Profile.GetDrivers(ASCOM.Common.DeviceTypes.ObservingConditions)) {
                try {
                    AscomObservingConditions obsdev = new AscomObservingConditions(device.ProgID, device.Name);
                    l.Add(obsdev);
                } catch (Exception) {
                }
            }
            return l;
        }

        public List<IDome> GetDomes() {
            var l = new List<IDome>();

            foreach (ASCOM.Com.ASCOMRegistration device in ASCOM.Com.Profile.GetDrivers(ASCOM.Common.DeviceTypes.Dome)) {
                try {
                    AscomDome ascomDome = new AscomDome(device.ProgID, device.Name);
                    l.Add(ascomDome);
                } catch (Exception) {
                }
            }
            return l;
        }

        public List<IFlatDevice> GetCoverCalibrators() {
            var l = new List<IFlatDevice>();

            foreach (ASCOM.Com.ASCOMRegistration device in ASCOM.Com.Profile.GetDrivers(ASCOM.Common.DeviceTypes.CoverCalibrator)) {
                try {
                    AscomCoverCalibrator ascomCoverCalibrator = new AscomCoverCalibrator(device.ProgID, device.Name);
                    l.Add(ascomCoverCalibrator);
                } catch (Exception) {
                }
            }
            return l;
        }

        public static string GetVersion() {
            return $"Version {ASCOM.Com.PlatformUtilities.PlatformVersion}";
            
        }

        public static Version GetPlatformVersion() {            
            return new Version(ASCOM.Com.PlatformUtilities.MajorVersion, ASCOM.Com.PlatformUtilities.MinorVersion, ASCOM.Com.PlatformUtilities.ServicePack, ASCOM.Com.PlatformUtilities.BuildNumber);
            
        }

        public static void LogComplianceIssue([CallerMemberName] string callerMember = "") {
            Logger.Error($"ASCOM {callerMember} threw a NotImplementedException. This is a driver compliance issue and should be fixed by the driver vendor.");
        }
    }
}