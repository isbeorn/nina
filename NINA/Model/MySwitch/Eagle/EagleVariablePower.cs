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