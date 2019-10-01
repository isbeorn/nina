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