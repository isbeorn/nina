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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Core.Utility.Http {

    public class HttpPostRequest : HttpRequest<string> {

        public HttpPostRequest(string url, string body, string contentType) : base(url) {
            this.Body = body;
            this.ContentType = contentType;
        }

        public string Body { get; }
        public string ContentType { get; }

        public override async Task<string> Request(CancellationToken ct, IProgress<int> progress = null) {
            string result = string.Empty;

            try {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(CoreUtil.UserAgent);

                var content = new StringContent(Body);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(ContentType);

                using var response = await httpClient.PostAsync(Url, content, ct);

                response.EnsureSuccessStatusCode();

                result = await response.Content.ReadAsStringAsync();
            } catch (OperationCanceledException) {
                ct.ThrowIfCancellationRequested();
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.Notification.ShowError(String.Format(Locale.Loc.Instance["LblUnableToConnectTo"], Url));
            }

            return result;
        }
    }
}