using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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
                formattedUrl = string.Format(Url, Parameters);
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