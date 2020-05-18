#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json.Linq;
using NINA.Model;
using NINA.Model.ImageData;
using NINA.Utility;
using NINA.Utility.Enum;
using NINA.Utility.Http;
using NINA.Utility.Notification;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.PlateSolving.Solvers {

    internal class AstrometryPlateSolver : BaseSolver {
        private const string AUTHURL = "/api/login/";
        private const string UPLOADURL = "/api/upload";
        private const string SUBMISSIONURL = "/api/submissions/{0}";
        private const string JOBSTATUSURL = "/api/jobs/{0}";
        private const string JOBINFOURL = "/api/jobs/{0}/info/";
        private const string ANNOTATEDIMAGEURL = "/annotated_display/{0}";

        private string _apiurl;
        private string _apikey;

        [Serializable()]
        internal class AstrometryNetFailedException : Exception {

            internal AstrometryNetFailedException(string type, JObject response) : base(CreateErrorMessage(type, response)) {
            }

            private static string CreateErrorMessage(string type, JObject response) {
                string message;
                string status = response.GetValue("status").ToString();

                // Failed solve jobs die with "failure". Failed jobs are normal and not neccessarily an error condition.
                if (status == "failure") {
                    message = $"{type} failed to solve";
                    Logger.Info($"Plate Solving: Astrometry.net: {message}");
                } else if (status == "error") {
                    message = response.GetValue("errormessage").ToString();
                    Logger.Error($"Plate Solving: Astrometry.net: {type} failed. Server response: {message}");
                } else {
                    message = "Unspecified error";
                    Logger.Error($"Plate Solving: Astrometry.net: {type} failed. {message}");
                }

                return message;
            }
        }

        public AstrometryPlateSolver(string apiurl, string apikey) {
            this._apiurl = apiurl;
            this._apikey = apikey;
        }

        private async Task<JObject> Authenticate(CancellationToken canceltoken) {
            string response = string.Empty;
            string json = "{\"apikey\":\"" + _apikey + "\"}";
            json = HttpRequest.EncodeUrl(json);
            string body = "request-json=" + json;
            var request = new HttpPostRequest(_apiurl + AUTHURL, body, "application/x-www-form-urlencoded");
            response = await request.Request(canceltoken);
            return JObject.Parse(response);
        }

        private async Task<JObject> SubmitImageStream(Stream ms, string session, CancellationToken canceltoken) {
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("request-json", "{\"publicly_visible\": \"n\", \"allow_modifications\": \"d\", \"session\": \"" + session + "\", \"allow_commercial_use\": \"d\"}");
            var request = new HttpUploadFile(_apiurl + UPLOADURL, ms, "file", "image/jpeg", nvc);
            string response = await request.Request(canceltoken);
            return JObject.Parse(response);
        }

        private async Task<JObject> GetSubmissionStatus(string subid, CancellationToken canceltoken) {
            var request = new HttpGetRequest(_apiurl + SUBMISSIONURL, subid);
            string response = await request.Request(canceltoken);
            return JObject.Parse(response);
        }

        private async Task<JObject> GetJobStatus(string jobid, CancellationToken canceltoken) {
            var request = new HttpGetRequest(_apiurl + JOBSTATUSURL, jobid);
            string response = await request.Request(canceltoken);
            return JObject.Parse(response);
        }

        private async Task<JObject> GetJobInfo(string jobid, CancellationToken canceltoken) {
            var request = new HttpGetRequest(_apiurl + JOBINFOURL, jobid);
            string response = await request.Request(canceltoken);
            return JObject.Parse(response);
        }

        private Task<BitmapSource> GetJobImage(string jobid, CancellationToken canceltoken) {
            var request = new HttpDownloadImageRequest(_apiurl + ANNOTATEDIMAGEURL, jobid);
            return request.Request(canceltoken);
        }

        private async Task<string> GetAuthenticationToken(CancellationToken cancelToken) {
            JObject authentication = await Authenticate(cancelToken);
            var status = authentication.GetValue("status");
            if (status?.ToString() == "success") {
                return authentication.GetValue("session").ToString();
            } else {
                throw new AstrometryNetFailedException("Authentication", authentication);
            }
        }

        private async Task<JObject> SubmitImage(string session, IImageData source, CancellationToken cancelToken) {
            string filePath = null;
            try {
                FileSaveInfo fileSaveInfo = new FileSaveInfo {
                    FilePath = WORKING_DIRECTORY,
                    FilePattern = Path.GetRandomFileName(),
                    FileType = FileTypeEnum.FITS
                };

                filePath = await source.SaveToDisk(fileSaveInfo, cancelToken);
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
            string imageSubmissionStatus = imageSubmission.GetValue("status")?.ToString();
            string subid = imageSubmission.GetValue("subid").ToString();

            if (imageSubmissionStatus != "success") {
                throw new AstrometryNetFailedException($"Job submission {subid}", imageSubmission);
            }

            Logger.Info($"Plate Solving: Astrometry.net: Submission {subid} created for solving.");
            progress.Report(new ApplicationStatus() { Status = $"Waiting for Astrometry.net to solve submission {subid}..." });

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
                    Logger.Info($"Plate Solving: Astrometry.net: Job {jobId} completed successfully.");
                    break;
                } else if (jobStatus == "failure") {
                    throw new AstrometryNetFailedException($"Job {jobId}", ojobstatus);
                }

                await Task.Delay(1000);
            };

            JObject job = await GetJobInfo(jobId, cancelToken);
            Logger.Info($"Plate Solving: Astrometry.net: Job {jobId} successfully retrieved.");

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
                progress.Report(new ApplicationStatus() { Status = "Authenticating to Astrometery.net..." });
                var session = await GetAuthenticationToken(cancelToken);

                progress.Report(new ApplicationStatus() { Status = "Uploading image to Astrometry.net..." });
                var jobId = await SubmitImageJob(progress, source, session, cancelToken);

                progress.Report(new ApplicationStatus() { Status = $"Getting result for Astrometry.net job {jobId}..." });
                JobResult jobinfo = await GetJobResult(jobId, cancelToken);

                result.Orientation = jobinfo.calibration.orientation;
                /* The orientation is mirrored on the x-axis */
                result.Orientation = 180 - result.Orientation + 360;

                result.Pixscale = jobinfo.calibration.pixscale;
                result.Coordinates = new Utility.Astrometry.Coordinates(jobinfo.calibration.ra, jobinfo.calibration.dec, Utility.Astrometry.Epoch.J2000, Utility.Astrometry.Coordinates.RAType.Degrees);
                result.Radius = jobinfo.calibration.radius;
            } catch (OperationCanceledException) {
                result.Success = false;
            } catch (Exception ex) {
                result.Success = false;
                Notification.ShowError($"Error plate solving with Astrometry.net. {ex.Message}");
            }

            if (result.Success) {
                progress.Report(new ApplicationStatus() { Status = "Solved" });
            } else {
                progress.Report(new ApplicationStatus() { Status = "Solve failed" });
            }
            return result;
        }

        protected override void EnsureSolverValid(PlateSolveParameter parameter) {
            if (string.IsNullOrWhiteSpace(_apikey)) {
                throw new ArgumentException("Astrometry.net API key is not configured");
            }

            if (string.IsNullOrWhiteSpace(_apiurl)) {
                throw new ArgumentException("Astrometry.net API URL is not configured");
            }

            // Trailing spaces on the API key text sometimes sneak in if it has been copy and pasted
            if (Regex.IsMatch(_apikey, @"\s")) {
                throw new ArgumentException("Astrometry.net API key contains an invalid space character");
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
