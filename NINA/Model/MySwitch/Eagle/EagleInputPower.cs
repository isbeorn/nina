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

    internal class EagleInputPower : EagleSwitch {

        public EagleInputPower(short index, string baseUrl) : base(index, baseUrl) {
            getRoute = "getsupply";
        }

        public override string Name {
            get => "Power";
        }

        protected override async Task<double> GetValue() {
            var url = baseUrl + getRoute;

            Logger.Trace($"Try getting value via {url}");

            var request = new Utility.Http.HttpGetRequest(url, Id);
            var response = await request.Request(new CancellationToken());

            var jobj = JObject.Parse(response);
            var regoutResponse = jobj.ToObject<SupplyResponse>();
            if (regoutResponse.Success()) {
                return regoutResponse.Supply;
            } else {
                return double.NaN;
            }
        }

        private class SupplyResponse : EagleResponse {

            [JsonProperty(PropertyName = "supply")]
            public double Supply;
        }
    }
}