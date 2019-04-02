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
using NINA.Utility.Http;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.PlateSolving {

    internal class AstrometryPlateSolver : IPlateSolver {
        private const string AUTHURL = "/api/login/";
        private const string UPLOADURL = "/api/upload";
        private const string SUBMISSIONURL = "/api/submissions/{0}";
        private const string JOBSTATUSURL = "/api/jobs/{0}";
        private const string JOBINFOURL = "/api/jobs/{0}/info/";
        private const string ANNOTATEDIMAGEURL = "/annotated_display/{0}";

        private string _domain;
        private string _apikey;

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

            JObject o = JObject.Parse(response);

            return o;
        }

        private async Task<JObject> SubmitImage(MemoryStream ms, string session, CancellationToken canceltoken) {
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("request-json", "{\"publicly_visible\": \"n\", \"allow_modifications\": \"d\", \"session\": \"" + session + "\", \"allow_commercial_use\": \"d\"}");
            var request = new HttpUploadFile(_domain + UPLOADURL, ms, "file", "image/jpeg", nvc);
            string response = await request.Request(canceltoken);
            JObject o = JObject.Parse(response);

            return o;
        }

        private async Task<JObject> GetSubmissionStatus(string subid, CancellationToken canceltoken) {
            var request = new HttpGetRequest(_domain + SUBMISSIONURL, subid);
            string response = await request.Request(canceltoken);
            JObject o = JObject.Parse(response);

            return o;
        }

        private async Task<JObject> GetJobStatus(string jobid, CancellationToken canceltoken) {
            var request = new HttpGetRequest(_domain + JOBSTATUSURL, jobid);
            string response = await request.Request(canceltoken);
            JObject o = JObject.Parse(response);

            return o;
        }

        private async Task<JObject> GetJobInfo(string jobid, CancellationToken canceltoken) {
            var request = new HttpGetRequest(_domain + JOBINFOURL, jobid);
            string response = await request.Request(canceltoken);
            JObject o = JObject.Parse(response);

            return o;
        }

        private Task<BitmapSource> GetJobImage(string jobid, CancellationToken canceltoken) {
            var request = new HttpDownloadImageRequest(_domain + ANNOTATEDIMAGEURL, jobid);
            return request.Request(canceltoken);
        }

        public async Task<PlateSolveResult> SolveAsync(MemoryStream image, IProgress<ApplicationStatus> progress, CancellationToken canceltoken) {
            PlateSolveResult result = new PlateSolveResult();

            try {
                progress.Report(new ApplicationStatus() { Status = "Authenticating..." });
                JObject authentication = await Authenticate(canceltoken);
                var status = authentication.GetValue("status");
                string session = string.Empty;
                if (status?.ToString() == "success") {
                    session = authentication.GetValue("session").ToString();

                    progress.Report(new ApplicationStatus() { Status = "Uploading Image..." });
                    JObject imagesubmission = await SubmitImage(image, session, canceltoken);

                    string subid = string.Empty;
                    string jobid = string.Empty;
                    if (imagesubmission.GetValue("status")?.ToString() == "success") {
                        subid = imagesubmission.GetValue("subid").ToString();

                        progress.Report(new ApplicationStatus() { Status = "Waiting for plate solve to start ..." });
                        while (true) {
                            canceltoken.ThrowIfCancellationRequested();

                            JObject submissionstatus = await GetSubmissionStatus(subid, canceltoken);

                            JArray jobids;
                            jobids = (JArray)submissionstatus.GetValue("jobs");
                            if (jobids.Count > 0) {
                                jobid = jobids.First.ToString();
                                if (jobid != "") {
                                    break;
                                }
                            }
                            await Task.Delay(1000);
                        };

                        if (!string.IsNullOrWhiteSpace(jobid)) {
                            string jobstatus = string.Empty;
                            progress.Report(new ApplicationStatus() { Status = "Solving ..." });
                            while (true) {
                                canceltoken.ThrowIfCancellationRequested();
                                JObject ojobstatus = await GetJobStatus(jobid, canceltoken);
                                jobstatus = ojobstatus.GetValue("status").ToString();

                                if ((jobstatus == "failure") || (jobstatus == "success")) {
                                    break;
                                }
                                await Task.Delay(1000);
                            };

                            if (jobstatus == "success") {
                                progress.Report(new ApplicationStatus() { Status = "Getting plate solve result ..." });
                                JObject job = await GetJobInfo(jobid, canceltoken);
                                JobResult jobinfo = job.ToObject<JobResult>();

                                result.Orientation = jobinfo.calibration.orientation;
                                result.Pixscale = jobinfo.calibration.pixscale;
                                result.Coordinates = new Utility.Astrometry.Coordinates(jobinfo.calibration.ra, jobinfo.calibration.dec, Utility.Astrometry.Epoch.J2000, Utility.Astrometry.Coordinates.RAType.Degrees);
                                result.Radius = jobinfo.calibration.radius;

                                progress.Report(new ApplicationStatus() { Status = "Solved" });
                            } else {
                                result.Success = false;
                                progress.Report(new ApplicationStatus() { Status = "Plate solve failed" });
                            }
                        } else {
                            result.Success = false;
                            progress.Report(new ApplicationStatus() { Status = "Failed to get job result" });
                        }
                    } else {
                        result.Success = false;
                        progress.Report(new ApplicationStatus() { Status = "Failed to get submission" });
                    }
                } else {
                    result.Success = false;
                    progress.Report(new ApplicationStatus() { Status = "Authorization failed ..." });
                }
            } catch (System.OperationCanceledException) {
                result.Success = false;
            } finally {
                progress.Report(new ApplicationStatus() { Status = string.Empty });
            }
            return result;
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