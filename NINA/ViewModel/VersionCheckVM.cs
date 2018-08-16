using Newtonsoft.Json.Linq;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.ViewModel {

    internal class VersionCheckVM : BaseINPC {
        private const string VERSIONSURL = "https://api.bitbucket.org/2.0/repositories/Isbeorn/nina/versions";
        private const string DOWNLOADSURL = "https://api.bitbucket.org/2.0/repositories/Isbeorn/nina/downloads";

        public VersionCheckVM() {
            UpdateCommand = new RelayCommand(Update);
        }

        public ICommand UpdateCommand { get; set; }
        private CancellationTokenSource _cancelTokenSource;

        private Version _latestVersion;
        private string _setupLocation;

        public async Task<bool> CheckUpdate() {
            _cancelTokenSource = new CancellationTokenSource();
            try {
                var updateAvailable = await CheckIfUpdateIsAvailable();
                if (updateAvailable) {
                    var result = MyMessageBox.MyMessageBox.Show(string.Format(Locale.Loc.Instance["LblNewUpdateAvailable"], _latestVersion.ToString()), "", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.Yes);
                    if (result == System.Windows.MessageBoxResult.Yes) {
                        WindowService ws = new WindowService();
                        ws.OnDialogResultChanged += (s, e) => {
                            var dialogResult = (WindowService.DialogResultEventArgs)e;
                            if (dialogResult.DialogResult != true) {
                                _cancelTokenSource.Cancel();
                            }
                        };
                        ws.ShowDialog(this, Locale.Loc.Instance["LblUpdating"], System.Windows.ResizeMode.CanResize, System.Windows.WindowStyle.SingleBorderWindow);

                        _setupLocation = await DownloadLatestVersion();
                        _setupLocation = Unzip(_setupLocation);
                        if (!string.IsNullOrEmpty(_setupLocation)) {
                            UpdateReady = true;
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

        private void Update(object o) {
            ProcessStartInfo Info = new ProcessStartInfo();
            Info.WindowStyle = ProcessWindowStyle.Hidden;
            Info.CreateNoWindow = true;
            Info.FileName = _setupLocation + "NINASetup.msi";
            Process.Start(Info);
            System.Windows.Application.Current.Shutdown();
        }

        private async Task<bool> CheckIfUpdateIsAvailable() {
            try {
                _latestVersion = await GetLatestVersion();

                if (_latestVersion > CurrentVersion) {
                    return true;
                } else {
                    return false;
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }
            return false;
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

        private async Task<string> DownloadLatestVersion() {
            var url = await GetDownloadUrl(_latestVersion.ToString());
            var destination = Path.GetTempPath() + "NINASetup.zip";
            Progress<int> downloadProgress = new Progress<int>((p) => { Progress = p; });
            await Utility.Utility.HttpDownloadFile(new Uri(url), destination, _cancelTokenSource.Token, downloadProgress);
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

        private async Task<string> GetDownloadUrl(string version) {
            var downloads = await GetBitBucketRecursive<BitBucketDownload>(DOWNLOADSURL);

            var filename = "NINASetup_{0}{1}.zip";
            if (DllLoader.IsX86()) {
                filename = string.Format(filename, version, "_x86");
            } else {
                filename = string.Format(filename, version, "");
            }

            var download = downloads.values.Where((x) => x.name == filename).FirstOrDefault();
            if (download != null) {
                return download.links.self.href;
            } else {
                return "";
            }
        }

        private async Task<Version> GetLatestVersion() {
            var versions = await GetBitBucketRecursive<BitBucketVersion>(VERSIONSURL);

            var max = versions.values.Max((x) => x.name);
            return max;
        }

        private async Task<BitBucketBase<T>> GetBitBucketRecursive<T>(string url) {
            var stringversions = await Utility.Utility.HttpGetRequest(_cancelTokenSource.Token, url, null);
            JObject o = JObject.Parse(stringversions);
            BitBucketBase<T> versions = o.ToObject<BitBucketBase<T>>();

            _cancelTokenSource.Token.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(versions.next)) {
                return versions;
            } else {
                var next = await GetBitBucketRecursive<T>(versions.next);
                foreach (T v in next.values) {
                    versions.values.Add(v);
                }
                return versions;
            }
        }

        private Version CurrentVersion {
            get {
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                Version version = new Version(fvi.FileVersion);
                return version;
            }
        }
    }

    public class BitBucketBase<T> {
        public int pagelen;
        public ICollection<T> values;
        public int page;
        public int size;
        public string next;
    }

    public class BitBucketDownload {
        public string name;
        public BitBucketLink links;
        public string type;
        public int size;
    }

    public class BitBucketLink {
        public BitBucketHRef self;
    }

    public class BitBucketHRef {
        public string href;
    }

    public class BitBucketVersion {
        public Version name;
    }
}