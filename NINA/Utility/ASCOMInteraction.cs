#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFocuser;
using NINA.Model.MyRotator;
using NINA.Model.MySwitch;
using NINA.Model.MyTelescope;
using NINA.Model.MyWeatherData;
using NINA.Profile;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NINA.Utility {

    internal class ASCOMInteraction {

        public static List<ICamera> GetCameras(IProfileService profileService) {
            var l = new List<ICamera>();
            using (var ascomDevices = new ASCOM.Utilities.Profile()) {
                foreach (ASCOM.Utilities.KeyValuePair device in ascomDevices.RegisteredDevices("Camera")) {
                    try {
                        AscomCamera cam = new AscomCamera(device.Key, device.Value + " (ASCOM)", profileService);
                        Logger.Trace(string.Format("Adding {0}", cam.Name));
                        l.Add(cam);
                    } catch (Exception) {
                        //only add cameras which are supported. e.g. x86 drivers will not work in x64
                    }
                }
                return l;
            }
        }

        public static List<ITelescope> GetTelescopes(IProfileService profileService) {
            var l = new List<ITelescope>();
            using (var ascomDevices = new ASCOM.Utilities.Profile()) {
                foreach (ASCOM.Utilities.KeyValuePair device in ascomDevices.RegisteredDevices("Telescope")) {
                    try {
                        AscomTelescope telescope = new AscomTelescope(device.Key, device.Value, profileService);
                        l.Add(telescope);
                    } catch (Exception) {
                        //only add telescopes which are supported. e.g. x86 drivers will not work in x64
                    }
                }
                return l;
            }
        }

        public static List<IFilterWheel> GetFilterWheels(IProfileService profileService) {
            var l = new List<IFilterWheel>();
            using (var ascomDevices = new ASCOM.Utilities.Profile()) {
                foreach (ASCOM.Utilities.KeyValuePair device in ascomDevices.RegisteredDevices("FilterWheel")) {
                    try {
                        AscomFilterWheel fw = new AscomFilterWheel(device.Key, device.Value);
                        l.Add(fw);
                    } catch (Exception) {
                        //only add filter wheels which are supported. e.g. x86 drivers will not work in x64
                    }
                }
                return l;
            }
        }

        public static List<IRotator> GetRotators(IProfileService profileService) {
            var l = new List<IRotator>();
            using (var ascomDevices = new ASCOM.Utilities.Profile()) {
                foreach (ASCOM.Utilities.KeyValuePair device in ascomDevices.RegisteredDevices("Rotator")) {
                    try {
                        AscomRotator rotator = new AscomRotator(device.Key, device.Value);
                        l.Add(rotator);
                    } catch (Exception) {
                        //only add filter wheels which are supported. e.g. x86 drivers will not work in x64
                    }
                }
                return l;
            }
        }

        public static List<IFocuser> GetFocusers(IProfileService profileService) {
            var l = new List<IFocuser>();
            using (var ascomDevices = new ASCOM.Utilities.Profile()) {
                foreach (ASCOM.Utilities.KeyValuePair device in ascomDevices.RegisteredDevices("Focuser")) {
                    try {
                        AscomFocuser focuser = new AscomFocuser(device.Key, device.Value);
                        l.Add(focuser);
                    } catch (Exception) {
                        //only add filter wheels which are supported. e.g. x86 drivers will not work in x64
                    }
                }
                return l;
            }
        }

        public static List<ISwitchHub> GetSwitches(IProfileService profileService) {
            var l = new List<ISwitchHub>();
            using (var ascomDevices = new ASCOM.Utilities.Profile()) {
                foreach (ASCOM.Utilities.KeyValuePair device in ascomDevices.RegisteredDevices("Switch")) {
                    try {
                        AscomSwitchHub ascomSwitch = new AscomSwitchHub(device.Key, device.Value);
                        l.Add(ascomSwitch);
                    } catch (Exception) {
                        //only add filter wheels which are supported. e.g. x86 drivers will not work in x64
                    }
                }
                return l;
            }
        }

        public static List<IWeatherData> GetWeatherDataSources(IProfileService profileService) {
            var l = new List<IWeatherData>();
            var ascomDevices = new ASCOM.Utilities.Profile();

            foreach (ASCOM.Utilities.KeyValuePair device in ascomDevices.RegisteredDevices("ObservingConditions")) {
                try {
                    AscomObservingConditions obsdev = new AscomObservingConditions(device.Key, device.Value);
                    l.Add(obsdev);
                } catch (Exception) {
                }
            }
            return l;
        }

        public static string GetVersion() {
            using (var util = new ASCOM.Utilities.Util()) {
                return $"Version {util.PlatformVersion}";
            }
        }

        public static Version GetPlatformVersion() {
            using (var util = new ASCOM.Utilities.Util()) {
                return new Version(util.MajorVersion, util.MinorVersion);
            }
        }

        public static void LogComplianceIssue([CallerMemberName]string callerMember = "") {
            Logger.Error($"ASCOM {callerMember} threw a PropertyNotImplementedException. This is a driver compliance issue and should be fixed by the driver vendor.");
        }
    }
}