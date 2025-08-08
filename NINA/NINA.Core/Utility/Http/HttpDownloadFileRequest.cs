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

    public class HttpDownloadFileRequest : HttpRequest {

        public HttpDownloadFileRequest(string url, string targetLocation) : base(url) {
            this.TargetLocation = targetLocation;
        }

        public string TargetLocation { get; }

        public override async Task Request(CancellationToken ct, IProgress<int> progress = null) {
            try {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(CoreUtil.UserAgent);

                using var response = await httpClient.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                using var input = await response.Content.ReadAsStreamAsync(ct);
                using var output = new FileStream(TargetLocation, FileMode.Create, FileAccess.Write, FileShare.None);

                var buffer = new byte[81920];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await input.ReadAsync(buffer, 0, buffer.Length, ct)) > 0) {
                    await output.WriteAsync(buffer, 0, bytesRead, ct);
                    totalRead += bytesRead;

                    if (totalBytes > 0) {
                        int percent = (int)((totalRead * 100L) / totalBytes);
                        progress?.Report(percent);
                    }
                }
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception ex) {
                Logger.Error(ex);
                throw;
            }
        }
    }
}