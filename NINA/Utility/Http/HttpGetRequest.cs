using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.Http {

    internal class HttpGetRequest : HttpRequest<string> {

        public HttpGetRequest(string url, params object[] parameters) : base(url) {
            this.Parameters = parameters;
        }

        public object[] Parameters { get; }

        public override async Task<string> Request(CancellationToken ct, IProgress<int> progress = null) {
            string result = string.Empty;

            var formattedUrl = Url;

            if (Parameters != null) {
                formattedUrl = string.Format(Url, Parameters);
            }

            HttpWebRequest request = null;
            HttpWebResponse response = null;
            using (ct.Register(() => request.Abort(), useSynchronizationContext: false)) {
                try {
                    request = (HttpWebRequest)WebRequest.Create(formattedUrl);
                    HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
                    request.CachePolicy = noCachePolicy;

                    response = (HttpWebResponse)await request.GetResponseAsync();

                    using (var streamReader = new StreamReader(response.GetResponseStream())) {
                        result = streamReader.ReadToEnd();
                    }
                } catch (Exception ex) {
                    Logger.Error(ex);
                    if (response != null) {
                        response.Close();
                        response = null;
                    }
                } finally {
                    request = null;
                }
            }

            return result;
        }
    }
}