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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Core.Utility.Http {

    public class HttpDownloadImageRequest : HttpRequest<BitmapSource> {

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

            try {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(CoreUtil.UserAgent);

                using var data = await httpClient.GetStreamAsync(formattedUrl, ct);
                using var ms = new MemoryStream();
                await data.CopyToAsync(ms, ct);
                ms.Seek(0, SeekOrigin.Begin);

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                img = bitmap;
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception ex) {
                Logger.Error(ex);
                throw;
            }

            return img;
        }
    }
}