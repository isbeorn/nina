#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using NINA.Utility;
using NINA.Utility.Enum;
using NINA.Utility.Http;
using NINA.Utility.WindowService;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel {

    internal class VersionCheckVM : BaseINPC {
        private const string BASEURL = "https://nighttime-imaging.eu/";
        private const string VERSIONSURL = BASEURL + "index.php/wp-json/nina/v1/versioninfo/{0}";

        public VersionCheckVM() {
            ShowDownloadCommand = new AsyncCommand<bool>(ShowDownload);
            DownloadCommand = new AsyncCommand<bool>(Download);
            CancelDownloadCommand = new RelayCommand(CancelDownload);
            UpdateCommand = new RelayCommand(Update);
        }

        public ICommand UpdateCommand { get; set; }
        public ICommand CancelDownloadCommand { get; set; }
        public IAsyncCommand DownloadCommand { get; set; }
        public IAsyncCommand ShowDownloadCommand { get; set; }
        private CancellationTokenSource checkCts;
        private CancellationTokenSource downloadCts;
        private VersionInfo versionInfo;

        private string setupLocation;

        private IWindowServiceFactory windowServiceFactory;

        public IWindowServiceFactory WindowServiceFactory {
            get {
                if (windowServiceFactory == null) {
                    windowServiceFactory = new WindowServiceFactory();
                }
                return windowServiceFactory;
            }
            set {
                windowServiceFactory = value;
            }
        }

        public async Task<bool> CheckUpdate() {
            checkCts?.Dispose();
            checkCts = new CancellationTokenSource();
            try {
                versionInfo = await GetVersionInfo((AutoUpdateSourceEnum)NINA.Properties.Settings.Default.AutoUpdateSource, checkCts.Token);
                if (versionInfo?.IsNewer() == true) {
                    UpdateAvailable = true;
                    var projectVersion = new ProjectVersion(versionInfo.Version);
                    UpdateAvailableText = string.Format(Locale.Loc.Instance["LblNewUpdateAvailable"], projectVersion);
                    Changelog = await GetChangelog(versionInfo, checkCts.Token);
                } else {
                    return false;
                }
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                versionInfo = null;
                Logger.Error(ex);
            }
            return true;
        }

        private bool ValidateChecksum(VersionInfo versionInfo, string file) {
            using (var md5 = MD5.Create()) {
                using (var stream = File.OpenRead(file)) {
                    var fileChecksum = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty);
                    return fileChecksum == versionInfo?.GetChecksum();
                }
            }
        }

        private void Update(object o) {
            ProcessStartInfo Info = new ProcessStartInfo();
            Info.WindowStyle = ProcessWindowStyle.Hidden;
            Info.CreateNoWindow = true;
            Info.FileName = Path.Combine(setupLocation, "NINASetupBundle.exe");
            Process.Start(Info);
            System.Windows.Application.Current.Shutdown();
        }

        private async Task<bool> ShowDownload() {
            var ws = WindowServiceFactory.Create();
            await ws.ShowDialog(this, UpdateAvailableText, System.Windows.ResizeMode.CanResize, System.Windows.WindowStyle.SingleBorderWindow);
            return true;
        }

        private async Task<bool> Download() {
            downloadCts?.Dispose();
            downloadCts = new CancellationTokenSource();
            try {
                Downloading = true;
                setupLocation = await DownloadLatestVersion(versionInfo);
                if (ValidateChecksum(versionInfo, setupLocation)) {
                    setupLocation = Unzip(setupLocation);
                    if (!string.IsNullOrEmpty(setupLocation)) {
                        UpdateReady = true;
                    }
                } else {
                    Utility.Notification.Notification.ShowError(Locale.Loc.Instance["LblChecksumError"]);
                    UpdateReady = false;
                }
                return UpdateReady;
            } catch (OperationCanceledException) {
            }
            Downloading = false;
            return UpdateReady;
        }

        private void CancelDownload(object o) {
            downloadCts?.Cancel();
        }

        private string Unzip(string zipLocation) {
            var destination = Path.Combine(Path.GetTempPath(), "NINASetup");
            if (Directory.Exists(destination)) {
                Directory.Delete(destination, true);
            }
            checkCts.Token.ThrowIfCancellationRequested();
            ZipFile.ExtractToDirectory(zipLocation, destination);
            return destination;
        }

        private async Task<string> DownloadLatestVersion(VersionInfo versionInfo) {
            var url = versionInfo.GetFileUrl();
            var destination = Path.Combine(Path.GetTempPath(), "NINASetup.zip");
            Progress<int> downloadProgress = new Progress<int>((p) => { Progress = p; });
            var request = new HttpDownloadFileRequest(url, destination);
            await request.Request(downloadCts.Token, downloadProgress);
            return destination;
        }

        private int _progress;

        public int Progress {
            get {
                return _progress;
            }
            set {
                _progress = value;
                RaisePropertyChanged();
            }
        }

        private bool downloadReady = false;

        public bool Downloading {
            get {
                return downloadReady;
            }
            set {
                downloadReady = value;
                RaisePropertyChanged();
            }
        }

        private bool _updateReady = false;

        public bool UpdateReady {
            get {
                return _updateReady;
            }
            set {
                _updateReady = value;
                RaisePropertyChanged();
            }
        }

        private bool updateAvailable = false;

        public bool UpdateAvailable {
            get {
                return updateAvailable;
            }
            set {
                updateAvailable = value;
                RaisePropertyChanged();
            }
        }

        private string updateAvailableText;

        public string UpdateAvailableText {
            get => updateAvailableText;
            set {
                updateAvailableText = value;
                RaisePropertyChanged();
            }
        }

        private string changelog = string.Empty;

        public string Changelog {
            get {
                return changelog;
            }
            set {
                changelog = value;
                RaisePropertyChanged();
            }
        }

        private async Task<VersionInfo> GetVersionInfo(AutoUpdateSourceEnum source, CancellationToken ct) {
            try {
                var url = string.Empty;
                switch (source) {
                    case AutoUpdateSourceEnum.NIGHTLY:
                        url = string.Format(VERSIONSURL, "nightly");
                        break;

                    case AutoUpdateSourceEnum.BETA:
                        url = string.Format(VERSIONSURL, "beta");
                        break;

                    default:
                        url = string.Format(VERSIONSURL, "release");
                        break;
                }

                var request = new Utility.Http.HttpGetRequest(url);
                var response = await request.Request(ct);

                //Validate the returned json against the schema
                var schema = await NJsonSchema.JsonSchema.FromJsonAsync(VersionInfo.Schema);
                var validationErrors = schema.Validate(response);
                if (validationErrors.Count == 0) {
                    JObject jobj = JObject.Parse(response);
                    var versionInfo = jobj.ToObject<VersionInfo>();
                    return versionInfo;
                } else {
                    var errorString = string.Join(Environment.NewLine, validationErrors.Select(v => {
                        if (v.HasLineInfo) {
                            return $"Property {v.Property} validation failed due to {v.Kind} at Line {v.LineNumber} Position {v.LinePosition}";
                        } else {
                            return $"Property {v.Property} validation failed due to {v.Kind}";
                        }
                    }));

                    Logger.Error($"VersionInfo JSON did not validate against schema! {Environment.NewLine}{errorString}");
                }
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                Logger.Error(ex);
            }
            return null;
        }

        private async Task<string> GetChangelog(VersionInfo versionInfo, CancellationToken ct) {
            string changelog = string.Empty;
            var changelogUrl = versionInfo.GetChangelogUrl();
            if (!string.IsNullOrEmpty(changelogUrl)) {
                try {
                    var request = new HttpGetRequest(changelogUrl);
                    changelog = await request.Request(ct);
                } catch (OperationCanceledException) {
                } catch (Exception ex) {
                    Logger.Error(ex);
                    changelog = string.Empty;
                }
            }
            return changelog;
        }

        public class VersionInfo {

            public static string Schema {
                get => @"{
	                '$schema': 'http://json-schema.org/draft-07/schema#',
                    'additionalProperties': false,
                    'properties': {
                      'version': {
                        'type': 'string',
                        'pattern': '^(\\d+\\.){3}(\\d+)$'
                      },
                      'file': {
                        'type': 'string',
                        'format': 'uri-reference'
                      },
                      'file_x86': {
                        'type': 'string',
                        'format': 'uri-reference'
                      },
                      'changelog': {
                        'type': 'string',
                        'format': 'uri-reference'
                      },
                      'checksum': {
                        'type': 'string',
                        'minLength': 32,
                        'maxLength': 32,
                        'pattern': '^[A-Fa-f0-9]{32}$'
                      },
                      'checksum_x86': {
                        'type': 'string',
                        'minLength': 32,
                        'maxLength': 32,
                        'pattern': '^[A-Fa-f0-9]{32}$'
                      }
                    },
  	                'required': ['version', 'checksum', 'file', 'checksum_x86', 'file_x86', 'changelog']
                }";
            }

            [JsonProperty(PropertyName = "version")]
            public Version Version;

            [JsonProperty(PropertyName = "checksum")]
            public string Checksum;

            [JsonProperty(PropertyName = "file")]
            public string File;

            [JsonProperty(PropertyName = "checksum_x86")]
            public string Checksum_x86;

            [JsonProperty(PropertyName = "file_x86")]
            public string File_x86;

            [JsonProperty(PropertyName = "changelog")]
            public string Changelog;

            public string GetChecksum() {
                if (DllLoader.IsX86()) {
                    return this.Checksum_x86;
                } else {
                    return this.Checksum;
                }
            }

            public string GetChangelogUrl() {
                return BASEURL + this.Changelog;
            }

            public string GetFileUrl() {
                string filename = "";
                if (DllLoader.IsX86()) {
                    filename = BASEURL + this.File_x86;
                } else {
                    filename = BASEURL + this.File;
                }
                return filename;
            }

            public bool IsNewer() {
                if (GetApplicationVersion() < this.Version) {
                    return true;
                } else {
                    return false;
                }
            }

            private Version GetApplicationVersion() {
                return new Version(Utility.Utility.Version);
            }
        }
    }
}