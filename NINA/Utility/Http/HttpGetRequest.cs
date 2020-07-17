#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.Http {

    internal class HttpGetRequest : HttpRequest<string> {

        public HttpGetRequest(string url, params object[] parameters) : base(url) {
            this.Parameters = parameters;
        }

        public object[] Parameters { get; }

        public override async Task<string> Request(CancellationToken ct, IProgress<int> progress = null) {
            string result = string.Empty;

            var formattedUrl = Url;

            if (Parameters != null) {
                formattedUrl = string.Format(CultureInfo.InvariantCulture, Url, Parameters);
            }

            HttpWebRequest request = null;
            HttpWebResponse response = null;
            using (ct.Register(() => request.Abort(), useSynchronizationContext: false)) {
                try {
                    request = (HttpWebRequest)WebRequest.Create(formattedUrl);
                    HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
                    request.CachePolicy = noCachePolicy;

                    response = (HttpWebResponse)await request.GetResponseAsync();

                    using (var streamReader = new StreamReader(response.GetResponseStream())) {
                        result = streamReader.ReadToEnd();
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                    if (response != null) {
                        response.Close();
                        response = null;
                    }
                } finally {
                    request = null;
                }
            }

            return result;
        }
    }
}