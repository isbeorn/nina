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
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.Http {

    internal class HttpPostRequest : HttpRequest<string> {

        public HttpPostRequest(string url, string body, string contentType) : base(url) {
            this.Body = body;
            this.ContentType = contentType;
        }

        public string Body { get; }
        public string ContentType { get; }

        public override async Task<string> Request(CancellationToken ct, IProgress<int> progress = null) {
            string result = string.Empty;

            HttpWebRequest request = null;
            HttpWebResponse response = null;
            using (ct.Register(() => request.Abort(), useSynchronizationContext: false)) {
                try {
                    request = (HttpWebRequest)WebRequest.Create(Url);
                    request.ContentType = ContentType;
                    request.Method = "POST";

                    using (var streamWriter = new StreamWriter(request.GetRequestStream())) {
                        streamWriter.Write(Body);
                        streamWriter.Flush();
                        streamWriter.Close();
                    }
                    response = (HttpWebResponse)await request.GetResponseAsync();
                    using (var streamReader = new StreamReader(response.GetResponseStream())) {
                        result = streamReader.ReadToEnd();
                    }
                } catch (Exception ex) {
                    ct.ThrowIfCancellationRequested();
                    Logger.Error(ex);
                    Notification.Notification.ShowError(string.Format("Unable to connect to {0}", Url));

                    response?.Close();
                    response = null;
                } finally {
                    request = null;
                }
            }

            return result;
        }
    }
}