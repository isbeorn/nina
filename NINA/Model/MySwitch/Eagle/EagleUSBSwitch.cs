#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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
