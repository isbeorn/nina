using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace NINA.Utility.Http {

    internal abstract class HttpRequest {

        public HttpRequest(string url) {
            this.Url = url;
        }

        public string Url { get; }

        public abstract Task Request(CancellationToken ct, IProgress<int> progress = null);

        public static string EncodeUrl(string s) {
            return HttpUtility.UrlEncode(s);
        }
    }

    internal abstract class HttpRequest<T> {

        public HttpRequest(string url) {
            this.Url = url;
        }

        public string Url { get; }

        public abstract Task<T> Request(CancellationToken ct, IProgress<int> progress = null);
    }
}