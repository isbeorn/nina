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
using NINA.Utility.Http;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.Api {

    internal class JSONApi : ITcpIpApi {
        protected string baseAddress = "localhost";
        protected string protocol = "";
        protected int port = 8080;
        protected string rootPath = "/";
        protected int timeout = 1000;

        public JSONApi(string baseAddr, int port) {
            this.baseAddress = baseAddr;
            this.port = port;
        }

        public JSONApi() {
        }

        string ITcpIpApi.RootPath {
            get {
                return rootPath;
            }
            set {
                rootPath = value;
            }
        }

        string ITcpIpApi.BaseAddress {
            get {
                return baseAddress;
            }
            set {
                this.baseAddress = value;
            }
        }

        string ITcpIpApi.Protocol {
            get {
                return protocol;
            }
            set {
                this.protocol = value;
            }
        }

        int ITcpIpApi.Port {
            get {
                return port;
            }
            set {
                port = value;
            }
        }

        int ITcpIpApi.Timeout {
            get {
                return timeout;
            }
            set {
                timeout = value;
            }
        }

        /// <summary>
        /// builds an HTTP GET command for the server
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private string BuildCommandUrl(string action) {
            if (protocol == "") protocol = "//";
            string ret = protocol + baseAddress + ":" + port.ToString() + rootPath + action;
            return ret;
        }

        public async Task<dynamic> SendCommand(string action, ApiParameters pars, string method, ApiParameters headers) {
            return await SendRequest(BuildCommandUrl(action), pars, method, headers);
        }

        /// <summary>
        /// Sadly there does nto seem to be a better method to valdiate a json string in .NET
        /// </summary>
        /// <param name="inp"></param>
        /// <returns></returns>
        private bool IsValidJSON(string inp) {
            try {
                JsonConvert.DeserializeObject(inp);
            } catch (Exception ex) {
                return false;
            }
            return true;
        }

        public object JsonDecode(string ins) {
            return JsonConvert.DeserializeObject(ins);
        }

        /// <summary>
        /// pars are a Dictionary with key=value1, key2=value2 -> http://something/action?key1=value1&key2=value2
        /// </summary>
        /// <param name="request">the request url</param>
        /// <param name="pars">parameters to be added</param>
        /// <param name="method">GET / POST/ DELETE ...</param>
        /// <param name="headers">optional request headers</param>
        /// <returns></returns>
        public async Task<dynamic> SendRequest(string request, ApiParameters pars, string method, ApiParameters headers) {
            dynamic ret;
            try {
                string content = "false";
                if (method == null) method = "GET";

                // process parameters if any
                string requestData = "";
                if (pars != null) requestData = pars.RequestData();

                if ((requestData != "") && (method == "GET")) {
                    request += "?" + requestData;
                }

                if (method == "GET") {
                    //parameters are already embedded in the url
                    HttpGetRequest rq = new HttpGetRequest(request, null);
                    content = await rq.Request(new CancellationToken());
                } else {
                    HttpPostRequest rq = new HttpPostRequest(request, requestData, "application/json");
                    content = await rq.Request(new CancellationToken());
                }

                //Some JSON Apis are not 100% JSON compliant and return non json responses (i.e. Stellarium)
                if (IsValidJSON(content)) ret = JsonConvert.DeserializeObject(content);
                else ret = content;
            } catch (Exception ex) {
                ret = null;
                Logger.Error(ex);
            }
            return ret;
        }
    }
}