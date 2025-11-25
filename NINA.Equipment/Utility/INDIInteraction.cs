#region "copyright"

/*
    Copyright © 2016 - 2025 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.INDI;
using NINA.INDI.Devices;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Interfaces;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System;
using NINA.Core.Utility;
using System.Collections.Generic;
using System.Threading.Tasks;
using NINA.Equipment.Equipment.MyTelescope;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyWeatherData;

namespace NINA.Equipment.Utility {

    public class INDIInteraction(IProfileService profileService) {
        private readonly IProfileService profileService = profileService;

        public List<ICamera> GetCameras(IExposureDataFactory exposureDataFactory) {
            var l = new List<ICamera>();
            return l;
        }

        public static async Task<List<IFocuser>> GetFocusers() {
            var l = new List<IFocuser>();
            // Wait for INDI server to be ready before trying to enumerate drivers.
            if (!await INDIClient.Instance.WaitForServerReadyAsync(TimeSpan.FromSeconds(15))) {
                Logger.Debug("INDI server not ready - skipping INDI focusers enumeration");
                return l;
            }
            foreach (var device in await INDIClient.Instance.GetDrivers(IndiDeviceInterface.FOCUSER_INTERFACE)) {
                IndiFocuser focuser = new(device);
                l.Add(focuser);
            }
            return l;
        }

        public async Task<List<ITelescope>> GetTelescopes() {
            var l = new List<ITelescope>();
            // Ensure INDI server is up before attempting to get telescope drivers
            if (!await INDIClient.Instance.WaitForServerReadyAsync(TimeSpan.FromSeconds(15))) {
                Logger.Debug("INDI server not ready - skipping INDI telescope enumeration");
                return l;
            }
            foreach (var device in await INDIClient.Instance.GetDrivers(IndiDeviceInterface.TELESCOPE_INTERFACE)) {
                IndiTelescope telescope = new(device, profileService);
                l.Add(telescope);
            }
            return l;
        }

        public static async Task<List<IRotator>> GetRotators() {
            var l = new List<IRotator>();
            if (!await INDIClient.Instance.WaitForServerReadyAsync(TimeSpan.FromSeconds(15))) {
                Logger.Debug("INDI server not ready - skipping INDI rotator enumeration");
                return l;
            }
            foreach (var device in await INDIClient.Instance.GetDrivers(IndiDeviceInterface.ROTATOR_INTERFACE)) {
                IndiRotator rotator = new(device);
                l.Add(rotator);
            }
            return l;
        }

        public async Task<List<IFilterWheel>> GetFilterWheels() {
            var l = new List<IFilterWheel>();
            if (!await INDIClient.Instance.WaitForServerReadyAsync(TimeSpan.FromSeconds(15))) {
                Logger.Debug("INDI server not ready - skipping INDI filterwheel enumeration");
                return l;
            }
            foreach (var device in await INDIClient.Instance.GetDrivers(IndiDeviceInterface.FILTER_INTERFACE)) {
                IndiFilterWheel filterWheel = new(device, profileService);
                l.Add(filterWheel);
            }
            return l;
        }

        public static async Task<List<IWeatherData>> GetWeatherData() {
            var l = new List<IWeatherData>();
            if (!await INDIClient.Instance.WaitForServerReadyAsync(TimeSpan.FromSeconds(15))) {
                Logger.Debug("INDI server not ready - skipping INDI weather-data enumeration");
                return l;
            }
            foreach (var device in await INDIClient.Instance.GetDrivers(IndiDeviceInterface.WEATHER_INTERFACE)) {
                IndiWeatherData weatherData = new(device);
                l.Add(weatherData);
            }
            return l;
        }

        public static string GetVersion() {
            return "0"; //return $"Version {INDI.Com.PlatformUtilities.PlatformVersion}";
        }

        public static Version GetPlatformVersion() {
            return new Version(); //return new Version(INDI.Com.PlatformUtilities.MajorVersion, INDI.Com.PlatformUtilities.MinorVersion, INDI.Com.PlatformUtilities.ServicePack, INDI.Com.PlatformUtilities.BuildNumber);
        }
    }
}
