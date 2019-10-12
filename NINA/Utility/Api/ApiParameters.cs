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

using System.Collections.Generic;
using System.Web;

namespace NINA.Utility.Api {

    internal class ApiParameters : Dictionary<string, string> {

        public ApiParameters(string key, string value) {
            base.Add(key, value);
        }

        public ApiParameters() {
        }

        /// <summary>
        /// Overrides the Add command
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public ApiParameters Add(string key, string value) {
            base.Add(key, value);
            return this;
        }

        /// <summary>
        ///  econdes data for webHttpRequest
        /// </summary>
        /// <returns></returns>
        public string RequestData() {
            string requestData = "";
            if ((Count > 0)) {
                int i = 0;
                foreach (KeyValuePair<string, string> par in this) {
                    if (i++ > 0) requestData += "&";
                    requestData += HttpUtility.UrlEncode(par.Key) + "=" + HttpUtility.UrlEncode(par.Value);
                }
            }
            return requestData;
        }
    }
}