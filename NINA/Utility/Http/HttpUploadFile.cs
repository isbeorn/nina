using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility.Http {

    internal class HttpUploadFile : HttpRequest<string> {
        public string ContentType { get; }
        public NameValueCollection NameValueCollection { get; }
        public MemoryStream File { get; }
        public string ParamName { get; }

        public HttpUploadFile(string url, MemoryStream file, string paramName, string contentType, NameValueCollection nvc) : base(url) {
            this.File = file;
            this.ParamName = paramName;
            this.ContentType = contentType;
            this.NameValueCollection = nvc;
        }

        public override async Task<string> Request(CancellationToken ct, IProgress<int> progress = null) {
            string result = string.Empty;
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(Url);
            wr.ContentType = "multipart/form-data; boundary=" + boundary;
            wr.Method = "POST";
            //wr.KeepAlive = true;
            //wr.Credentials = System.Net.CredentialCache.DefaultCredentials;

            Stream rs = wr.GetRequestStream();

            string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
            foreach (string key in NameValueCollection.Keys) {
                rs.Write(boundarybytes, 0, boundarybytes.Length);
                string formitem = string.Format(formdataTemplate, key, NameValueCollection[key]);
                byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                rs.Write(formitembytes, 0, formitembytes.Length);
            }
            rs.Write(boundarybytes, 0, boundarybytes.Length);

            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
            string header = string.Format(headerTemplate, ParamName, File, ContentType);
            byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
            rs.Write(headerbytes, 0, headerbytes.Length);

            // FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[4096];
            int bytesRead = 0;
            while ((bytesRead = File.Read(buffer, 0, buffer.Length)) != 0) {
                rs.Write(buffer, 0, bytesRead);
                ct.ThrowIfCancellationRequested();
            }
            File.Close();

            byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            rs.Write(trailer, 0, trailer.Length);
            ct.ThrowIfCancellationRequested();
            WebResponse wresp = null;
            using (ct.Register(() => wr.Abort(), useSynchronizationContext: false)) {
                try {
                    wresp = await wr.GetResponseAsync();
                    using (var streamReader = new StreamReader(wresp.GetResponseStream())) {
                        result = streamReader.ReadToEnd();
                    }
                } catch (Exception ex) {
                    ct.ThrowIfCancellationRequested();
                    Logger.Error(ex);
                    Notification.Notification.ShowError(string.Format("Unable to connect to {0}", Url));

                    wresp?.Close();
                    wresp = null;
                } finally {
                    wr = null;
                }
            }
            return result;
        }
    }
}