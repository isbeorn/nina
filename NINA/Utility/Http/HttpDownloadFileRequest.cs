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
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.Http {

    internal class HttpDownloadFileRequest : HttpRequest {

        public HttpDownloadFileRequest(string url, string targetLocation) : base(url) {
            this.TargetLocation = targetLocation;
        }

        public string TargetLocation { get; }

        public override async Task Request(CancellationToken ct, IProgress<int> progress = null) {
            using (var client = new WebClient()) {
                using (ct.Register(() => client.CancelAsync(), useSynchronizationContext: false)) {
                    try {
                        client.DownloadProgressChanged += (s, e) => {
                            progress?.Report(e.ProgressPercentage);
                        };
                        await client.DownloadFileTaskAsync(Url, TargetLocation);
                    } catch (WebException ex) {
                        if (ex.Status == WebExceptionStatus.RequestCanceled) {
                            throw new OperationCanceledException();
                        } else {
                            throw ex;
                        }
                    }
                }
            }
        }
    }
}
