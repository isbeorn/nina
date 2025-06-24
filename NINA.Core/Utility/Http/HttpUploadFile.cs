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
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Core.Utility.Http {

    public class HttpUploadFile : HttpRequest<string> {
        public string ContentType { get; }
        public NameValueCollection NameValueCollection { get; }
        public FileStream File { get; }
        public string ParamName { get; }

        public HttpUploadFile(string url, FileStream file, string paramName, string contentType, NameValueCollection nvc) : base(url) {
            this.File = file;
            this.ParamName = paramName;
            this.ContentType = contentType;
            this.NameValueCollection = nvc;
        }

        public override async Task<string> Request(CancellationToken ct, IProgress<int> progress = null) {
            string result = string.Empty;

            try {
                using var httpClient = new HttpClient {
                    Timeout = TimeSpan.FromSeconds(300)
                };

                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(CoreUtil.UserAgent);

                using var form = new MultipartFormDataContent();

                // Add all key-value form fields
                foreach (string key in NameValueCollection.Keys) {
                    string value = NameValueCollection[key];
                    form.Add(new StringContent(value), key);
                }

                var fileContent = new StreamContent(File);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(ContentType);

                form.Add(fileContent, ParamName, Path.GetFileName(File.Name));

                using var response = await httpClient.PostAsync(Url, form, ct);
                response.EnsureSuccessStatusCode();

                result = await response.Content.ReadAsStringAsync(ct);
            } catch (OperationCanceledException) {
                ct.ThrowIfCancellationRequested();
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.Notification.ShowError($"Unable to connect to {Url}");
            }

            return result;
        }
    }
}