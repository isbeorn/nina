#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Core.Utility.Http {

    public class HttpGetRequest : HttpRequest<string> {

        public HttpGetRequest(string url, params object[] parameters) : base(url) {
            this.Parameters = parameters;
        }

        public HttpGetRequest(string url, bool rethrowOnError, params object[] parameters) : this(url, parameters) {
            this.Parameters = parameters;
            this.rethrowOnError = rethrowOnError;
        }

        public object[] Parameters { get; }

        private bool rethrowOnError = false;

        public override async Task<string> Request(CancellationToken ct, IProgress<int> progress = null) {
            string result = string.Empty;

            var formattedUrl = Url;

            if (Parameters != null) {
                formattedUrl = string.Format(CultureInfo.InvariantCulture, Url, Parameters);
            }

            try {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(CoreUtil.UserAgent);

                using var response = await httpClient.GetAsync(formattedUrl, ct);

                response.EnsureSuccessStatusCode();

                result = await response.Content.ReadAsStringAsync();
            } catch (OperationCanceledException) {
                ct.ThrowIfCancellationRequested();
            } catch (Exception ex) {
                Logger.Error(formattedUrl + " " + ex);

                if (rethrowOnError) {
                    throw;
                }
            }

            return result;
        }
    }
}