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

    internal class EagleVariablePower : EagleWritableSwitch {

        public EagleVariablePower(short index, string baseUrl) : base(index, baseUrl) {
            getRoute = "getregout?idx=${0}";
            setRoute = "setregout?idx=${0}&volt={1}";
        }

        /// <summary>
        /// 0-12V power out port number 5: 2
        /// 0-12V power out port number 6: 1
        /// 0-12V power out port number 7: 0
        /// </summary>
        public override string Name {
            get => "Variable Power Out " + (7 - Id).ToString();
        }

        public override string Description {
            get => "Variable Power output port";
        }

        public override double Maximum {
            get => 12d;
        }

        public override double Minimum {
            get => 0d;
        }

        public override double StepSize {
            get => 3d;
        }

        protected override async Task<double> GetValue() {
            var url = baseUrl + getRoute;

            Logger.Trace($"Try getting value via {url}");

            var request = new Utility.Http.HttpGetRequest(url, Id);
            var response = await request.Request(new CancellationToken());

            var jobj = JObject.Parse(response);
            var regoutResponse = jobj.ToObject<RegoutResponse>();
            if (regoutResponse.Success()) {
                return regoutResponse.Voltage;
            } else {
                return double.NaN;
            }
        }

        private class RegoutResponse : EagleResponse {

            [JsonProperty(PropertyName = "voltage")]
            public double Voltage;

            [JsonProperty(PropertyName = "current")]
            public double Current;

            [JsonProperty(PropertyName = "power")]
            public double Power;
        }
    }
}