using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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