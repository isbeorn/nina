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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NINA.Utility;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MySwitch {

    internal class Eagle12VPower : EagleWritableSwitch {

        public Eagle12VPower(short index, string baseUrl) : base(index, baseUrl) {
            getRoute = "getpwrout?idx=${0}";
            setRoute = "setpwrout?idx=${0}&state={1}";
        }

        /// <summary>
        /// 12V power out  port number 1: 3
        /// 12V power out  port number 2: 2
        /// 12V power out  port number 3: 1
        /// 12V power out  port number 4: 0
        /// </summary>
        public override string Name {
            get => "12V Power Out " + (4 - Id).ToString();
        }

        public override string Description {
            get => "Fixed Power output port";
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

        protected override async Task<double> GetValue() {
            var url = baseUrl + getRoute;

            Logger.Trace($"Try getting value via {url}");

            var request = new Utility.Http.HttpGetRequest(url, Id);
            var response = await request.Request(new CancellationToken());

            var jobj = JObject.Parse(response);
            var regoutResponse = jobj.ToObject<PowerOutResponse>();
            if (regoutResponse.Success()) {
                return regoutResponse.Voltage > 0d ? 1d : 0d;
            } else {
                return double.NaN;
            }
        }

        private class PowerOutResponse : EagleResponse {

            [JsonProperty(PropertyName = "voltage")]
            public double Voltage;

            [JsonProperty(PropertyName = "current")]
            public double Current;

            [JsonProperty(PropertyName = "power")]
            public double Power;
        }
    }
}