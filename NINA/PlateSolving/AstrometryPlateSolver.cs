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

using Newtonsoft.Json.Linq;
using NINA.Model;
using NINA.Model.ImageData;
using NINA.Utility.Http;
using NINA.Utility.Notification;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.PlateSolving {

    internal class AstrometryPlateSolver : BaseSolver {
        private const string AUTHURL = "/api/login/";
        private const string UPLOADURL = "/api/upload";
        private const string SUBMISSIONURL = "/api/submissions/{0}";
        private const string JOBSTATUSURL = "/api/jobs/{0}";
        private const string JOBINFOURL = "/api/jobs/{0}/info/";
        private const string ANNOTATEDIMAGEURL = "/annotated_display/{0}";

        private string _domain;
        private string _apikey;

        internal class AstrometryAuthenticationFailedException : Exception {

            internal AstrometryAuthenticationFailedException(string status) : base($"Authentication failed with status: {status}") {
            }
        }

        internal class AstrometrySubmissionFailedException : Exception {

            internal AstrometrySubmissionFailedException(string status) : base($"Submission failed with status: {status}") {
            }
        }

        internal class AstrometryJobFailedException : Exception {

            internal AstrometryJobFailedException(string status) : base($"Job failed with status: {status}") {
            }
        }

        public AstrometryPlateSolver(string domain, string apikey) {
            this._domain = domain;
            this._apikey = apikey;
        }

        private async Task<JObject> Authenticate(CancellationToken canceltoken) {
            string response = string.Empty;
            string json = "{\"apikey\":\"" + _apikey + "\"}";
            json = HttpRequest.EncodeUrl(json);
            string body = "request-json=" + json;
            var request = new HttpPostRequest(_domain + AUTHURL, body, "application/x-www-form-urlencoded");
            response = await request.Request(canceltoken);
            return JObject.Parse(response);
        }

        private async Task<JObject> SubmitImageStream(Stream ms, string session, CancellationToken canceltoken) {
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("request-json", "{\"publicly_visible\": \"n\", \"allow_modifications\": \"d\", \"session\": \"" + session + "\", \"allow_commercial_use\": \"d\"}");
            var request = new HttpUploadFile(_domain + UPLOADURL, ms, "file", "image/jpeg", nvc);
            string response = await request.Request(canceltoken);
            return JObject.Parse(response);
        }

        private async Task<JObject> GetSubmissionStatus(string subid, CancellationToken canceltoken) {
            var request = new HttpGetRequest(_domain + SUBMISSIONURL, subid);
            string response = await request.Request(canceltoken);
            return JObject.Parse(response);
        }

        private async Task<JObject> GetJobStatus(string jobid, CancellationToken canceltoken) {
            var request = new HttpGetRequest(_domain + JOBSTATUSURL, jobid);
            string response = await request.Request(canceltoken);
            return JObject.Parse(response);
        }

        private async Task<JObject> GetJobInfo(string jobid, CancellationToken canceltoken) {
            var request = new HttpGetRequest(_domain + JOBINFOURL, jobid);
            string response = await request.Request(canceltoken);
            return JObject.Parse(response);
        }

        private Task<BitmapSource> GetJobImage(string jobid, CancellationToken canceltoken) {
            var request = new HttpDownloadImageRequest(_domain + ANNOTATEDIMAGEURL, jobid);
            return request.Request(canceltoken);
        }

        private async Task<string> GetAuthenticationToken(CancellationToken cancelToken) {
            JObject authentication = await Authenticate(cancelToken);
            var status = authentication.GetValue("status");
            if (status?.ToString() == "success") {
                return authentication.GetValue("session").ToString();
            } else {
                throw new AstrometryAuthenticationFailedException(status?.ToString());
            }
        }

        private async Task<JObject> SubmitImage(string session, IImageData source, CancellationToken cancelToken) {
            string filePath = null;
            try {
                filePath = await source.SaveToDisk(WORKING_DIRECTORY, Path.GetRandomFileName(), Utility.Enum.FileTypeEnum.FITS, cancelToken);
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
                    return await SubmitImageStream(fs, session, cancelToken);
                }
            } finally {
                if (filePath != null && File.Exists(filePath)) {
                    File.Delete(filePath);
                }
            }
        }

        private async Task<string> SubmitImageJob(
            IProgress<ApplicationStatus> progress,
            IImageData source,
            string session,
            CancellationToken cancelToken) {
            JObject imageSubmission = await SubmitImage(session, source, cancelToken);
            var imageSubmissionStatus = imageSubmission.GetValue("status")?.ToString();
            if (imageSubmissionStatus != "success") {
                throw new AstrometrySubmissionFailedException(imageSubmissionStatus);
            }

            string subid = imageSubmission.GetValue("subid").ToString();
            progress.Report(new ApplicationStatus() { Status = "Waiting for plate solve to start ..." });
            while (true) {
                cancelToken.ThrowIfCancellationRequested();
                JObject submissionStatus = await GetSubmissionStatus(subid, cancelToken);

                JArray jobids;
                jobids = (JArray)submissionStatus.GetValue("jobs");
                if (jobids.Count > 0) {
                    string jobid = jobids.First.ToString();
                    if (jobid?.Length > 0) {
                        return jobid;
                    }
                }
                await Task.Delay(1000);
            };
        }

        private async Task<JobResult> GetJobResult(string jobId, CancellationToken cancelToken) {
            while (true) {
                cancelToken.ThrowIfCancellationRequested();
                JObject ojobstatus = await GetJobStatus(jobId, cancelToken);
                string jobStatus = ojobstatus.GetValue("status").ToString();
                if (jobStatus == "success") {
                    break;
                } else if (jobStatus == "failure") {
                    throw new AstrometryJobFailedException(jobStatus);
                }
                await Task.Delay(1000);
            };

            JObject job = await GetJobInfo(jobId, cancelToken);
            return job.ToObject<JobResult>();
        }

        protected override async Task<PlateSolveResult> SolveAsyncImpl(
            IImageData source,
            PlateSolveParameter parameter,
            PlateSolveImageProperties imageProperties,
            IProgress<ApplicationStatus> progress,
            CancellationToken cancelToken) {
            PlateSolveResult result = new PlateSolveResult();

            try {
                progress.Report(new ApplicationStatus() { Status = "Authenticating..." });
                var session = await GetAuthenticationToken(cancelToken);

                progress.Report(new ApplicationStatus() { Status = "Uploading Image..." });
                var jobId = await SubmitImageJob(progress, source, session, cancelToken);

                progress.Report(new ApplicationStatus() { Status = "Getting job result..." });
                JobResult jobinfo = await GetJobResult(jobId, cancelToken);
                result.Orientation = jobinfo.calibration.orientation;
                result.Pixscale = jobinfo.calibration.pixscale;
                result.Coordinates = new Utility.Astrometry.Coordinates(jobinfo.calibration.ra, jobinfo.calibration.dec, Utility.Astrometry.Epoch.J2000, Utility.Astrometry.Coordinates.RAType.Degrees);
                result.Radius = jobinfo.calibration.radius;
            } catch (OperationCanceledException) {
                result.Success = false;
            } catch (Exception ex) {
                result.Success = false;
                Notification.ShowError($"Error plate solving with astrometry.net. {ex.Message}");
            }

            if (result.Success) {
                progress.Report(new ApplicationStatus() { Status = "Solved" });
            } else {
                progress.Report(new ApplicationStatus() { Status = "Solve failed" });
            }
            return result;
        }

        protected override void EnsureSolverValid() {
            if (_apikey == "") {
                throw new ArgumentException("astrometry.net API key not set");
            }
        }
    }

    public class JobResult {
        public string status;
        public string[] machine_tags;
        public Calibration calibration;
        public string[] tags;
        public string original_filename;
        public string[] objects_in_field;
    }

    public class Calibration {
        public double parity;
        public double orientation;
        public double pixscale;
        public double radius;
        public double ra;
        public double dec;
    }
}