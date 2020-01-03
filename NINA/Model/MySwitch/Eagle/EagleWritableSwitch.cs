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

using Newtonsoft.Json.Linq;
using NINA.Utility;
using Nito.AsyncEx;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MySwitch {

    internal abstract class EagleWritableSwitch : EagleSwitch, IWritableSwitch {

        public EagleWritableSwitch(short index, string baseUrl) : base(index, baseUrl) {
            this.TargetValue = this.Value;
        }

        public abstract double Maximum { get; }

        public abstract double Minimum { get; }

        public abstract double StepSize { get; }

        protected string setRoute;

        public async Task SetValue() {
            try {
                var url = baseUrl + setRoute;

                Logger.Trace($"Try setting value {TargetValue} via {url}");

                var request = new Utility.Http.HttpGetRequest(url, Id, TargetValue);
                var response = await request.Request(new CancellationToken());

                var jobj = JObject.Parse(response);
                var regoutResponse = jobj.ToObject<EagleResponse>();
                if (!regoutResponse.Success()) {
                    Logger.Warning($"Unable to set value {TargetValue} via {url}");
                } else {
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        private double targetValue;

        public double TargetValue {
            get => targetValue;
            set {
                targetValue = value;
                RaisePropertyChanged();
            }
        }
    }
}