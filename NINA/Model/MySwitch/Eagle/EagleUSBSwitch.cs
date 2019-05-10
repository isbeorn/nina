#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NINA.Utility;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MySwitch {

    internal class EagleUSBSwitch : EagleWritableSwitch {

        public EagleUSBSwitch(short index, string baseUrl) : base(index, baseUrl) {
            getRoute = "getpwrhub?idx=${0}";
            setRoute = "setpwrhub?idx=${0}&state={1}";
        }

        public override double Maximum {
            get => 1d;
        }

        public override double Minimum {
            get => 0d;
        }

        public override double StepSize {
            get => 1d;
        }

        /// <summary>
        /// USB 2.0 A port: 0
        /// USB 2.0 B port: 1
        /// USB 2.0 C port: 2
        /// USB 2.0 D port: 3
        /// </summary>
        public override string Name {
            get {
                switch (Id) {
                    case 0: return "USB A";
                    case 1: return "USB B";
                    case 2: return "USB C";
                    case 3: return "USB D";
                    default: return "USB Unknown";
                }
            }
        }

        public override string Description {
            get => "Usb hub output";
        }

        protected override async Task<double> GetValue() {
            var url = baseUrl + getRoute;

            Logger.Trace($"Try getting value via {url}");

            var request = new Utility.Http.HttpGetRequest(url, Id);
            var response = await request.Request(new CancellationToken());

            var jobj = JObject.Parse(response);
            var regoutResponse = jobj.ToObject<PowerHubResponse>();
            if (regoutResponse.Success()) {
                return regoutResponse.Status;
            } else {
                return double.NaN;
            }
        }

        private class PowerHubResponse : EagleResponse {

            [JsonProperty(PropertyName = "status")]
            public int Status;
        }
    }
}