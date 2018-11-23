using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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