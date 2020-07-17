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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Utility.Http {

    internal class HttpDownloadImageRequest : HttpRequest<BitmapSource> {

        public HttpDownloadImageRequest(string url, params object[] parameters) : base(url) {
            this.Parameters = parameters;
        }

        public object[] Parameters { get; }

        public override async Task<BitmapSource> Request(CancellationToken ct, IProgress<int> progress = null) {
            var img = new BitmapImage();

            var formattedUrl = Url;
            if (Parameters != null) {
                formattedUrl = string.Format(CultureInfo.InvariantCulture, Url, Parameters);
            }

            using (var client = new WebClient()) {
                using (ct.Register(() => client.CancelAsync(), useSynchronizationContext: false)) {
                    try {
                        client.DownloadProgressChanged += (s, e) => {
                            progress?.Report(e.ProgressPercentage);
                        };
                        var data = await client.DownloadDataTaskAsync(formattedUrl);
                        using (MemoryStream stream = new MemoryStream(data)) {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.StreamSource = stream;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            bitmap.Freeze();
                            img = bitmap;
                        }
                    } catch (WebException ex) {
                        if (ex.Status == WebExceptionStatus.RequestCanceled) {
                            throw new OperationCanceledException();
                        } else {
                            throw ex;
                        }
                    }
                }
            }
            return img;
        }
    }
}