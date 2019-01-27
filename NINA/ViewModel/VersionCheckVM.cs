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
using NINA.Utility;
using NINA.Utility.Enum;
using NINA.Utility.Http;
using NINA.Utility.Profile;
using NINA.Utility.WindowService;
using System;
using System.Collections.Generic;
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
            UpdateCommand = new RelayCommand(Update);
        }

        private IProfileService profileService;

        public ICommand UpdateCommand { get; set; }
        private CancellationTokenSource _cancelTokenSource;

        private string _setupLocation;

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
            _cancelTokenSource = new CancellationTokenSource();
            try {
                var versionInfo = await GetVersionInfo((AutoUpdateSourceEnum)NINA.Properties.Settings.Default.AutoUpdateSource);

                if (versionInfo?.IsNewer() == true) {
                    var result = MyMessageBox.MyMessageBox.Show(string.Format(Locale.Loc.Instance["LblNewUpdateAvailable"], versionInfo.version.ToString()), "", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.Yes);
                    if (result == System.Windows.MessageBoxResult.Yes) {
                        var ws = WindowServiceFactory.Create();
                        ws.OnDialogResultChanged += (s, e) => {
                            var dialogResult = (DialogResultEventArgs)e;
                            if (dialogResult.DialogResult != true) {
                                _cancelTokenSource.Cancel();
                            }
                        };
                        var t = ws.ShowDialog(this, Locale.Loc.Instance["LblUpdating"], System.Windows.ResizeMode.CanResize, System.Windows.WindowStyle.SingleBorderWindow);

                        _setupLocation = await DownloadLatestVersion(versionInfo);
                        if (ValidateChecksum(versionInfo, _setupLocation)) {
                            _setupLocation = Unzip(_setupLocation);
                            if (!string.IsNullOrEmpty(_setupLocation)) {
                                UpdateReady = true;
                            }
                            await t;
                        } else {
                            await ws.Close();
                            throw new Exception("Checksum does not match expected value. Downloaded file may be corrupted!");
                        }
                    }
                } else {
                    return false;
                }
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
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
            Info.FileName = _setupLocation + "NINASetupBundle.exe";
            Process.Start(Info);
            System.Windows.Application.Current.Shutdown();
        }

        private string Unzip(string zipLocation) {
            var destination = Path.GetTempPath() + "NINASetup\\";
            if (Directory.Exists(destination)) {
                Directory.Delete(destination, true);
            }
            _cancelTokenSource.Token.ThrowIfCancellationRequested();
            ZipFile.ExtractToDirectory(zipLocation, destination);
            return destination;
        }

        private async Task<string> DownloadLatestVersion(VersionInfo versionInfo) {
            var url = versionInfo.GetFileUrl();
            var destination = Path.GetTempPath() + "NINASetup.zip";
            Progress<int> downloadProgress = new Progress<int>((p) => { Progress = p; });
            var request = new HttpDownloadFileRequest(url, destination);
            await request.Request(_cancelTokenSource.Token, downloadProgress);
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

        private async Task<VersionInfo> GetVersionInfo(AutoUpdateSourceEnum source) {
            try {
                var request = new Utility.Http.HttpGetRequest(string.Format(VERSIONSURL, source.ToString().ToLower()));
                var response = await request.Request(new CancellationToken());

                var jobj = JObject.Parse(response);
                var versionInfo = jobj.ToObject<VersionInfo>();
                return versionInfo;
            } catch (Exception ex) {
                Logger.Error(ex);
            }
            return null;
        }

        public class VersionInfo {
            public Version version;
            public string checksum;
            public string file;
            public string checksum_x86;
            public string file_x86;
            public string changelog;

            public string GetChecksum() {
                if (DllLoader.IsX86()) {
                    return this.checksum_x86;
                } else {
                    return this.checksum;
                }
            }

            public string GetFileUrl() {
                string filename = "";
                if (DllLoader.IsX86()) {
                    filename = BASEURL + this.file_x86;
                } else {
                    filename = BASEURL + this.file;
                }
                return filename;
            }

            public bool IsNewer() {
                if (GetApplicationVersion() < this.version) {
                    return true;
                } else {
                    return false;
                }
            }

            private Version GetApplicationVersion() {
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                Version version = new Version(fvi.FileVersion);
                return version;
            }
        }
    }
}