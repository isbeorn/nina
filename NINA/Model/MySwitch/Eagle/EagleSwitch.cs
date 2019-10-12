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
using NINA.Utility;
using Nito.AsyncEx;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MySwitch {

    internal abstract class EagleSwitch : BaseINPC, ISwitch {
        protected string baseUrl;
        protected string getRoute;

        public EagleSwitch(short index, string baseUrl) {
            this.Id = index;
            this.baseUrl = baseUrl;
        }

        public virtual string Name {
            get => Id.ToString();
        }

        public virtual string Description {
            get => string.Empty;
        }

        public double Value { get; private set; }

        public short Id { get; }

        public async Task<bool> Poll() {
            try {
                var val = await GetValue();
                if (!double.IsNaN(val)) {
                    this.Value = val;
                    RaisePropertyChanged(nameof(Value));
                } else {
                    return false;
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                return false;
            }

            return true;
        }

        protected abstract Task<double> GetValue();

        protected class EagleResponse {

            [JsonProperty(PropertyName = "result")]
            public string Result;

            public bool Success() {
                return Result.ToLower().Trim() == "ok";
            }
        }
    }
}