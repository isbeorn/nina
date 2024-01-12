#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Core.Utility.Http;
using NINA.Equipment.Equipment.MyGPS.PegasusAstro.UnityApi;
using NINA.Equipment.Exceptions;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyGPS.PegasusAstro {
    public class UranusMeteo : BaseINPC, IGnss {
        private const string pegasusUnityUrl = "http://localhost:32000";
        private const string pegasusUnityUranusReport = "/Driver/Uranus/Report";
        private const string pegasusUnityDevicesConnected = "/Server/DeviceManager/Connected";

        public UranusMeteo(IProfileService profileService) {
        }

        public string Name => "PegasusAstro Uranus Meteo";

        public async Task<Location> GetLocation() {
            var location = new Location();

            try {
                var uranus = await FindUranus();

                var http = new HttpGetRequest(pegasusUnityUrl + pegasusUnityUranusReport + $"?DriverUniqueKey={uranus.UniqueKey}");
                var response = await http.Request(CancellationToken.None);
                Logger.Debug(response);

                var report = JsonConvert.DeserializeObject<DriverUranusReport.Report>(response);
                var message = report.Data.Message;

                Logger.Info($"PegasusAstro {uranus.Name} ({uranus.DeviceID}): Fixed: {message.IsGpsFixed}, Satellites: {message.TotalSatellites}");

                if (!message.IsGpsFixed) {
                    throw new GnssNoFixException(string.Empty);
                }

                location.Latitude = message.Latitude.Dd.DecimalDegree;
                location.Longitude = message.Longitude.Dd.DecimalDegree;
                location.Elevation = message.Altitude.Meters;

                return location;
            } catch (GnssNoFixException) {
                throw;
            } catch (GnssFailedToConnectException) {
                throw;
            } catch (Exception ex) {
                throw new GnssFailedToConnectException(ex.Message);
            }

        }

        private static async Task<ServerConnectedDevices.Device> FindUranus() {
            var http = new HttpGetRequest(pegasusUnityUrl + pegasusUnityDevicesConnected);

            var response = await http.Request(CancellationToken.None);
            Logger.Debug(response);

            var devices = JsonConvert.DeserializeObject<ServerConnectedDevices.Response>(response);

            foreach (var device in devices.Devices) {
                if (device.Name.ToLower().Equals("uranus")) {
                    Logger.Info($"Found PegasusAstro {device.FullName}, Device ID: {device.DeviceID}, FW: {device.Firmware}, Rev: {device.Revision}");
                    return device;
                }
            }

            throw new GnssFailedToConnectException(Loc.Instance["LblPegasusUranusNotFound"]);
        }
    }
}