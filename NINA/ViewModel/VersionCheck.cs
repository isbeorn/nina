using Newtonsoft.Json.Linq;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.ViewModel {
    class VersionCheck {
        const string VERSIONSURL = "https://api.bitbucket.org/2.0/repositories/Isbeorn/nina/versions";
        const string DOWNLOADSURL = "https://api.bitbucket.org/2.0/repositories/Isbeorn/nina/downloads";

        private CancellationTokenSource _cancelTokenSource;

        private Version _latestVersion;

        public async Task CheckUpdate() {
            _cancelTokenSource = new CancellationTokenSource();
            try {
                var updateAvailable = await CheckIfUpdateIsAvailable();
                if (updateAvailable) {
                    var result = MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblNewUpdateAvailable"], "", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.Yes);
                    if (result == System.Windows.MessageBoxResult.Yes) {
                        var setupLocation = await DownloadLatestVersion();
                        setupLocation = Unzip(setupLocation);
                        Update(setupLocation);
                    }
                } else {
                    return;
                }
            } catch (OperationCanceledException) {

            } catch (Exception ex) {

            }
        }

        private void Update(string setupLocation) {
            ProcessStartInfo Info = new ProcessStartInfo();
            Info.WindowStyle = ProcessWindowStyle.Hidden;
            Info.CreateNoWindow = true;
            Info.FileName = setupLocation + "setup.exe";
            Process.Start(Info);
            System.Windows.Application.Current.Shutdown();
        }

        private async Task<bool> CheckIfUpdateIsAvailable() {
            _latestVersion = await CheckLatestVersion();
            var compareVersion = _latestVersion.CompareTo(CurrentVersion);

            if (compareVersion > 0) {
                return true;
            } else {
                return false;
            }
        }

        private string Unzip(string zipLocation) {
            var destination = Path.GetTempPath() + "NINASetup\\";
            if (Directory.Exists(destination)) {
                Directory.Delete(destination, true);
            }
            ZipFile.ExtractToDirectory(zipLocation, destination);
            return destination;
        }

        private async Task<string> DownloadLatestVersion() {
            var url = await GetDownloadUrl(_latestVersion.ToString());
            var destination = Path.GetTempPath() + "NINASetup.zip";
            await Utility.Utility.HttpDownloadFile(new Uri(url), destination, _cancelTokenSource.Token);
            return destination;
        }

        private async Task<string> GetDownloadUrl(string version) {
            var downloads = await Utility.Utility.HttpGetRequest(_cancelTokenSource.Token, DOWNLOADSURL, null);
            JObject o = JObject.Parse(downloads);
            BitBucketDownloads d = o.ToObject<BitBucketDownloads>();

            var filename = "NINASetup_{0}{1}.zip";
            if (DllLoader.IsX86()) {
                filename = string.Format(filename, version, "_x86");
            } else {
                filename = string.Format(filename, version, "");
            }
            

            var download = d.values.Where((x) => x.name == filename).FirstOrDefault();
            if(download != null) {
                return download.links.self.href;
            } else {
                return "";
            }
        }

        private async Task<Version> CheckLatestVersion() {
            var versions = await Utility.Utility.HttpGetRequest(_cancelTokenSource.Token, VERSIONSURL, null);
            JObject o = JObject.Parse(versions);
            BitBucketVersions v = o.ToObject<BitBucketVersions>();

            var max = v.values.Max((x) => x.name);
            return max;
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

    public class BitBucketDownloads {
        public int pagelen;
        public ICollection<BitBucketDownload> values;
        public int page;
        public int size;
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

    public class BitBucketVersions {
        public int pagelen;
        public ICollection<BitBucketVersion> values;
        public int page;
        public int size;
    }

    public class BitBucketVersion {
        public Version name;        
    }
}
