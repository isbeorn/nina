using NINA.Utility;
using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.AstrometryIndexDownloader {
    public class AstrometryIndexDownloaderVM : BaseINPC {
        private const string WIDEFIELDINDEXESURL = "http://broiler.astrometry.net/~dstn/4100/";
        private const string INDEXESURL = "http://broiler.astrometry.net/~dstn/4200/";


        private AstrometryIndexDownloaderVM(string destinationfolder) {
            _focalLength = 750;
            _pixelSize = 3.8;
            _camHeight = 3520;
            _camWidth = 4656;
            _destinationfolder = destinationfolder;

            GetIndexList();
            SelectedWidest = Indexes.First();
            SelectedNarrowest = Indexes.Last();

            DownloadCommand = new AsyncCommand<bool>(() => DownloadAsync(new Progress<string>(p => DownloadStatus = p)));
            CancelDownloadCommand = new RelayCommand(CancelDownload);
        }

        private string _destinationfolder;

        CancellationTokenSource _cancelDownloadToken;
        private void CancelDownload(object obj) {
            _cancelDownloadToken?.Cancel();
        }

        private async Task<bool> DownloadAsync(IProgress<string> progress) {
            _cancelDownloadToken = new CancellationTokenSource();

            return await Task<bool>.Run(async () => await Download(progress));

        }

        private int _maximumFiles;
        public int MaximumFiles {
            get {
                return _maximumFiles;
            }
            set {
                _maximumFiles = value;
                RaisePropertyChanged();
            }
        }

        private int _processedFiles;
        public int ProcessedFiles {
            get {
                return _processedFiles;
            }
            set {
                _processedFiles = value;
                RaisePropertyChanged();
            }
        }

        private async Task<bool> Download(IProgress<string> progress) {
            var l = GetFullIndexList();

            ProcessedFiles = 0;
            try {
                System.IO.DirectoryInfo i = new System.IO.DirectoryInfo(_destinationfolder);
                if (!i.Exists) {
                    System.Windows.MessageBox.Show("Directory not found: " + _destinationfolder);
                    return false;
                }

                var filteredindexes = l.Where((x) => x.MaxArcMin <= SelectedWidest.MaxArcMin && x.MinArcMin >= SelectedNarrowest.MinArcMin);
                MaximumFiles = filteredindexes.Count();
                foreach (IndexFile f in filteredindexes) {
                    progress.Report("Downloading file " + f.Name);
                    _cancelDownloadToken.Token.ThrowIfCancellationRequested();
                    var success = await DownloadFile(f);
                    ProcessedFiles++;
                    if (!success) {
                        break;
                    }
                }
            } catch (OperationCanceledException) {

            }
            progress.Report("Finished");
            return true;
        }

        private async Task<bool> DownloadFile(IndexFile file) {
            var url = new Uri(INDEXESURL + file.Name);
            var success = false;
            using (var client = new WebClient()) {
                try {
                    await client.DownloadFileTaskAsync(url, _destinationfolder + file.Name);
                    success = true;
                } catch (Exception ex) {
                    Logger.Error(ex.Message, ex.StackTrace);
                    System.Windows.MessageBox.Show(ex.Message);
                }

            }
            return success;
        }

        private IAsyncCommand _downloadCommand;
        public IAsyncCommand DownloadCommand {
            get {
                return _downloadCommand;
            }
            set {
                _downloadCommand = value;
                RaisePropertyChanged();
            }
        }

        private RelayCommand _cancelDownloadCommand;
        public RelayCommand CancelDownloadCommand {
            get {
                return _cancelDownloadCommand;
            }

            set {
                _cancelDownloadCommand = value;
                RaisePropertyChanged();
            }
        }

        private string _downloadStatus;
        public string DownloadStatus {
            get {
                return _downloadStatus;
            }
            set {
                _downloadStatus = value;
                RaisePropertyChanged();
            }
        }

        public static void Show(string cygwinlocation) {
            var destinationfolder = cygwinlocation + "\\usr\\share\\astrometry\\data\\";
            AstrometryIndexDownloaderVM vm = new AstrometryIndexDownloaderVM(destinationfolder);

            System.Windows.Window win = new AstrometryIndexDownloader {
                DataContext = vm
            };

            var mainwindow = System.Windows.Application.Current.MainWindow;
            mainwindow.Opacity = 0.8;
            win.Left = mainwindow.Left + (mainwindow.Width - win.Width) / 2; ;
            win.Top = mainwindow.Top + (mainwindow.Height - win.Height) / 2;

            win.ShowDialog();
            mainwindow.Opacity = 1;
        }

        private int _focalLength;
        public int FocalLength {
            get {
                return _focalLength;
            }
            set {
                _focalLength = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ArcsecPerPixel));
                RaisePropertyChanged(nameof(FieldOfView));
                RaisePropertyChanged(nameof(RecommendedIndexes));
            }
        }

        private double _pixelSize;
        public double PixelSize {
            get {
                return _pixelSize;
            }
            set {
                _pixelSize = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ArcsecPerPixel));
                RaisePropertyChanged(nameof(FieldOfView));
                RaisePropertyChanged(nameof(RecommendedIndexes));
            }
        }

        private double _camWidth;
        public double CamWidth {
            get {
                return _camWidth;
            }
            set {
                _camWidth = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ArcsecPerPixel));
                RaisePropertyChanged(nameof(FieldOfView));
                RaisePropertyChanged(nameof(RecommendedIndexes));
            }
        }

        private double _camHeight;
        public double CamHeight {
            get {
                return _camHeight;
            }
            set {
                _camHeight = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ArcsecPerPixel));
                RaisePropertyChanged(nameof(FieldOfView));
                RaisePropertyChanged(nameof(RecommendedIndexes));

            }
        }

        IndexFile _selectedNarrowest;
        public IndexFile SelectedNarrowest {
            get {
                return _selectedNarrowest;
            }
            set {
                _selectedNarrowest = value;
                RaisePropertyChanged();
            }
        }

        IndexFile _selectedWidest;
        public IndexFile SelectedWidest {
            get {
                return _selectedWidest;
            }
            set {
                _selectedWidest = value;
                RaisePropertyChanged();
            }
        }

        public double ArcsecPerPixel {
            get {
                return Astrometry.ArcsecPerPixel(PixelSize, FocalLength);
            }
        }

        public double FieldOfView {
            get {
                return Astrometry.MaxFieldOfView(ArcsecPerPixel, CamWidth, CamHeight);
            }
        }

        public string RecommendedIndexes {
            get {
                var fov = FieldOfView;
                var min = fov * 0.2;
                var max = fov * 1.2;
                List<IndexFile> l = Indexes.ToList();

                IndexFile nearestmin = l.Aggregate((x, y) => Math.Abs(x.MinArcMin - min) < Math.Abs(y.MinArcMin - min) ? x : y);
                IndexFile nearestmax = l.Aggregate((x, y) => Math.Abs(x.MaxArcMin - max) < Math.Abs(y.MaxArcMin - max) ? x : y);

                return nearestmin.Name + " to " + nearestmax.Name;
            }
        }


        private void GetIndexList() {
            Indexes.Add(new IndexFile { Name = "index-4219.fits", Stars = 1080, MinArcMin = 1.4e+03, MaxArcMin = 2e+03 });
            Indexes.Add(new IndexFile { Name = "index-4218.fits", Stars = 1920, MinArcMin = 1e+03, MaxArcMin = 1.4e+03 });
            Indexes.Add(new IndexFile { Name = "index-4217.fits", Stars = 3000, MinArcMin = 680, MaxArcMin = 1e+03 });
            Indexes.Add(new IndexFile { Name = "index-4216.fits", Stars = 5880, MinArcMin = 480, MaxArcMin = 680 });
            Indexes.Add(new IndexFile { Name = "index-4215.fits", Stars = 12000, MinArcMin = 340, MaxArcMin = 480 });
            Indexes.Add(new IndexFile { Name = "index-4214.fits", Stars = 23520, MinArcMin = 240, MaxArcMin = 340 });
            Indexes.Add(new IndexFile { Name = "index-4213.fits", Stars = 48000, MinArcMin = 170, MaxArcMin = 240 });
            Indexes.Add(new IndexFile { Name = "index-4212.fits", Stars = 94080, MinArcMin = 120, MaxArcMin = 170 });
            Indexes.Add(new IndexFile { Name = "index-4211.fits", Stars = 182520, MinArcMin = 85, MaxArcMin = 120 });
            Indexes.Add(new IndexFile { Name = "index-4210.fits", Stars = 363000, MinArcMin = 60, MaxArcMin = 85 });
            Indexes.Add(new IndexFile { Name = "index-4209.fits", Stars = 730080, MinArcMin = 42, MaxArcMin = 60 });
            Indexes.Add(new IndexFile { Name = "index-4208.fits", Stars = 1452000, MinArcMin = 30, MaxArcMin = 42 });
            Indexes.Add(new IndexFile { Name = "index-4207.fits", Stars = 2920320, MinArcMin = 22, MaxArcMin = 30 });
            Indexes.Add(new IndexFile { Name = "index-4206.fits", Stars = 5808000, MinArcMin = 16, MaxArcMin = 22 });
            Indexes.Add(new IndexFile { Name = "index-4205.fits", Stars = 11681280, MinArcMin = 11, MaxArcMin = 16 });
            Indexes.Add(new IndexFile { Name = "index-4204.fits", Stars = 23231079, MinArcMin = 8, MaxArcMin = 11 });
            Indexes.Add(new IndexFile { Name = "index-4203.fits", Stars = 46189701, MinArcMin = 5.6, MaxArcMin = 8 });
            Indexes.Add(new IndexFile { Name = "index-4202.fits", Stars = 82778084, MinArcMin = 4, MaxArcMin = 5.6 });
            Indexes.Add(new IndexFile { Name = "index-4201.fits", Stars = 133027382, MinArcMin = 2.8, MaxArcMin = 4 });
            Indexes.Add(new IndexFile { Name = "index-4200.fits", Stars = 197025609, MinArcMin = 2, MaxArcMin = 2.8 });
        }

        private List<IndexFile> GetFullIndexList() {
            List<IndexFile> indexes = new List<IndexFile>();
            indexes.Add(new IndexFile { Name = "index-4219.fits", Stars = 1080, MinArcMin = 1.4e+03, MaxArcMin = 2e+03 });
            indexes.Add(new IndexFile { Name = "index-4218.fits", Stars = 1920, MinArcMin = 1e+03, MaxArcMin = 1.4e+03 });
            indexes.Add(new IndexFile { Name = "index-4217.fits", Stars = 3000, MinArcMin = 680, MaxArcMin = 1e+03 });
            indexes.Add(new IndexFile { Name = "index-4216.fits", Stars = 5880, MinArcMin = 480, MaxArcMin = 680 });
            indexes.Add(new IndexFile { Name = "index-4215.fits", Stars = 12000, MinArcMin = 340, MaxArcMin = 480 });
            indexes.Add(new IndexFile { Name = "index-4214.fits", Stars = 23520, MinArcMin = 240, MaxArcMin = 340 });
            indexes.Add(new IndexFile { Name = "index-4213.fits", Stars = 48000, MinArcMin = 170, MaxArcMin = 240 });
            indexes.Add(new IndexFile { Name = "index-4212.fits", Stars = 94080, MinArcMin = 120, MaxArcMin = 170 });
            indexes.Add(new IndexFile { Name = "index-4211.fits", Stars = 182520, MinArcMin = 85, MaxArcMin = 120 });
            indexes.Add(new IndexFile { Name = "index-4210.fits", Stars = 363000, MinArcMin = 60, MaxArcMin = 85 });
            indexes.Add(new IndexFile { Name = "index-4209.fits", Stars = 730080, MinArcMin = 42, MaxArcMin = 60 });
            indexes.Add(new IndexFile { Name = "index-4208.fits", Stars = 1452000, MinArcMin = 30, MaxArcMin = 42 });
            indexes.Add(new IndexFile { Name = "index-4207-00.fits", Stars = 243360, MinArcMin = 22, MaxArcMin = 30 });
            indexes.Add(new IndexFile { Name = "index-4207-01.fits", Stars = 243360, MinArcMin = 22, MaxArcMin = 30 });
            indexes.Add(new IndexFile { Name = "index-4207-02.fits", Stars = 243360, MinArcMin = 22, MaxArcMin = 30 });
            indexes.Add(new IndexFile { Name = "index-4207-03.fits", Stars = 243360, MinArcMin = 22, MaxArcMin = 30 });
            indexes.Add(new IndexFile { Name = "index-4207-04.fits", Stars = 243360, MinArcMin = 22, MaxArcMin = 30 });
            indexes.Add(new IndexFile { Name = "index-4207-05.fits", Stars = 243360, MinArcMin = 22, MaxArcMin = 30 });
            indexes.Add(new IndexFile { Name = "index-4207-06.fits", Stars = 243360, MinArcMin = 22, MaxArcMin = 30 });
            indexes.Add(new IndexFile { Name = "index-4207-07.fits", Stars = 243360, MinArcMin = 22, MaxArcMin = 30 });
            indexes.Add(new IndexFile { Name = "index-4207-08.fits", Stars = 243360, MinArcMin = 22, MaxArcMin = 30 });
            indexes.Add(new IndexFile { Name = "index-4207-09.fits", Stars = 243360, MinArcMin = 22, MaxArcMin = 30 });
            indexes.Add(new IndexFile { Name = "index-4207-10.fits", Stars = 243360, MinArcMin = 22, MaxArcMin = 30 });
            indexes.Add(new IndexFile { Name = "index-4207-11.fits", Stars = 243360, MinArcMin = 22, MaxArcMin = 30 });
            indexes.Add(new IndexFile { Name = "index-4206-00.fits", Stars = 484000, MinArcMin = 16, MaxArcMin = 22 });
            indexes.Add(new IndexFile { Name = "index-4206-01.fits", Stars = 484000, MinArcMin = 16, MaxArcMin = 22 });
            indexes.Add(new IndexFile { Name = "index-4206-02.fits", Stars = 484000, MinArcMin = 16, MaxArcMin = 22 });
            indexes.Add(new IndexFile { Name = "index-4206-03.fits", Stars = 484000, MinArcMin = 16, MaxArcMin = 22 });
            indexes.Add(new IndexFile { Name = "index-4206-04.fits", Stars = 484000, MinArcMin = 16, MaxArcMin = 22 });
            indexes.Add(new IndexFile { Name = "index-4206-05.fits", Stars = 484000, MinArcMin = 16, MaxArcMin = 22 });
            indexes.Add(new IndexFile { Name = "index-4206-06.fits", Stars = 484000, MinArcMin = 16, MaxArcMin = 22 });
            indexes.Add(new IndexFile { Name = "index-4206-07.fits", Stars = 484000, MinArcMin = 16, MaxArcMin = 22 });
            indexes.Add(new IndexFile { Name = "index-4206-08.fits", Stars = 484000, MinArcMin = 16, MaxArcMin = 22 });
            indexes.Add(new IndexFile { Name = "index-4206-09.fits", Stars = 484000, MinArcMin = 16, MaxArcMin = 22 });
            indexes.Add(new IndexFile { Name = "index-4206-10.fits", Stars = 484000, MinArcMin = 16, MaxArcMin = 22 });
            indexes.Add(new IndexFile { Name = "index-4206-11.fits", Stars = 484000, MinArcMin = 16, MaxArcMin = 22 });
            indexes.Add(new IndexFile { Name = "index-4205-00.fits", Stars = 973440, MinArcMin = 11, MaxArcMin = 16 });
            indexes.Add(new IndexFile { Name = "index-4205-01.fits", Stars = 973440, MinArcMin = 11, MaxArcMin = 16 });
            indexes.Add(new IndexFile { Name = "index-4205-02.fits", Stars = 973440, MinArcMin = 11, MaxArcMin = 16 });
            indexes.Add(new IndexFile { Name = "index-4205-03.fits", Stars = 973440, MinArcMin = 11, MaxArcMin = 16 });
            indexes.Add(new IndexFile { Name = "index-4205-04.fits", Stars = 973440, MinArcMin = 11, MaxArcMin = 16 });
            indexes.Add(new IndexFile { Name = "index-4205-05.fits", Stars = 973440, MinArcMin = 11, MaxArcMin = 16 });
            indexes.Add(new IndexFile { Name = "index-4205-06.fits", Stars = 973440, MinArcMin = 11, MaxArcMin = 16 });
            indexes.Add(new IndexFile { Name = "index-4205-07.fits", Stars = 973440, MinArcMin = 11, MaxArcMin = 16 });
            indexes.Add(new IndexFile { Name = "index-4205-08.fits", Stars = 973440, MinArcMin = 11, MaxArcMin = 16 });
            indexes.Add(new IndexFile { Name = "index-4205-09.fits", Stars = 973440, MinArcMin = 11, MaxArcMin = 16 });
            indexes.Add(new IndexFile { Name = "index-4205-10.fits", Stars = 973440, MinArcMin = 11, MaxArcMin = 16 });
            indexes.Add(new IndexFile { Name = "index-4205-11.fits", Stars = 973440, MinArcMin = 11, MaxArcMin = 16 });
            indexes.Add(new IndexFile { Name = "index-4204-00.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-01.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-02.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-03.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-04.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-05.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-06.fits", Stars = 483860, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-07.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-08.fits", Stars = 483990, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-09.fits", Stars = 483819, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-10.fits", Stars = 483999, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-11.fits", Stars = 483995, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-12.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-13.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-14.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-15.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-16.fits", Stars = 483969, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-17.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-18.fits", Stars = 483952, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-19.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-20.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-21.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-22.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-23.fits", Stars = 483981, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-24.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-25.fits", Stars = 483986, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-26.fits", Stars = 483974, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-27.fits", Stars = 483740, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-28.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-29.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-30.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-31.fits", Stars = 483988, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-32.fits", Stars = 483961, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-33.fits", Stars = 483953, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-34.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-35.fits", Stars = 483929, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-36.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-37.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-38.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-39.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-40.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-41.fits", Stars = 483994, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-42.fits", Stars = 483993, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-43.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-44.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-45.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-46.fits", Stars = 483996, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4204-47.fits", Stars = 484000, MinArcMin = 8, MaxArcMin = 11 });
            indexes.Add(new IndexFile { Name = "index-4203-00.fits", Stars = 969931, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-01.fits", Stars = 973314, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-02.fits", Stars = 973438, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-03.fits", Stars = 973440, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-04.fits", Stars = 966073, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-05.fits", Stars = 973170, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-06.fits", Stars = 910959, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-07.fits", Stars = 966229, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-08.fits", Stars = 956719, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-09.fits", Stars = 897010, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-10.fits", Stars = 971252, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-11.fits", Stars = 958467, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-12.fits", Stars = 973440, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-13.fits", Stars = 973440, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-14.fits", Stars = 973440, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-15.fits", Stars = 973440, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-16.fits", Stars = 930389, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-17.fits", Stars = 970213, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-18.fits", Stars = 932884, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-19.fits", Stars = 968018, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-20.fits", Stars = 973373, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-21.fits", Stars = 971132, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-22.fits", Stars = 973440, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-23.fits", Stars = 973297, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-24.fits", Stars = 971079, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-25.fits", Stars = 951167, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-26.fits", Stars = 946879, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-27.fits", Stars = 888222, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-28.fits", Stars = 973440, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-29.fits", Stars = 973417, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-30.fits", Stars = 973440, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-31.fits", Stars = 973417, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-32.fits", Stars = 969172, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-33.fits", Stars = 925266, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-34.fits", Stars = 968280, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-35.fits", Stars = 925153, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-36.fits", Stars = 973440, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-37.fits", Stars = 973432, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-38.fits", Stars = 973440, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-39.fits", Stars = 973438, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-40.fits", Stars = 973440, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-41.fits", Stars = 973391, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-42.fits", Stars = 973348, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-43.fits", Stars = 973414, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-44.fits", Stars = 973376, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-45.fits", Stars = 973440, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-46.fits", Stars = 966773, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4203-47.fits", Stars = 973329, MinArcMin = 5.6, MaxArcMin = 8 });
            indexes.Add(new IndexFile { Name = "index-4202-00.fits", Stars = 1715979, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-01.fits", Stars = 1900669, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-02.fits", Stars = 1935660, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-03.fits", Stars = 1935365, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-04.fits", Stars = 1633683, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-05.fits", Stars = 1880550, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-06.fits", Stars = 1160753, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-07.fits", Stars = 1633954, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-08.fits", Stars = 1474861, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-09.fits", Stars = 1113743, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-10.fits", Stars = 1764232, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-11.fits", Stars = 1495316, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-12.fits", Stars = 1935388, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-13.fits", Stars = 1935468, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-14.fits", Stars = 1935096, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-15.fits", Stars = 1932849, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-16.fits", Stars = 1244435, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-17.fits", Stars = 1713238, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-18.fits", Stars = 1251667, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-19.fits", Stars = 1659936, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-20.fits", Stars = 1912030, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-21.fits", Stars = 1768684, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-22.fits", Stars = 1934528, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-23.fits", Stars = 1935592, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-24.fits", Stars = 1735957, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-25.fits", Stars = 1450142, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-26.fits", Stars = 1397348, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-27.fits", Stars = 1087199, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-28.fits", Stars = 1935963, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-29.fits", Stars = 1919769, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-30.fits", Stars = 1935996, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-31.fits", Stars = 1934902, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-32.fits", Stars = 1712585, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-33.fits", Stars = 1220752, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-34.fits", Stars = 1653717, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-35.fits", Stars = 1221486, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-36.fits", Stars = 1935911, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-37.fits", Stars = 1935222, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-38.fits", Stars = 1934066, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-39.fits", Stars = 1925593, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-40.fits", Stars = 1935986, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-41.fits", Stars = 1933049, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-42.fits", Stars = 1935661, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-43.fits", Stars = 1903265, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-44.fits", Stars = 1899335, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-45.fits", Stars = 1935755, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-46.fits", Stars = 1593667, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4202-47.fits", Stars = 1901082, MinArcMin = 4, MaxArcMin = 5.6 });
            indexes.Add(new IndexFile { Name = "index-4201-00.fits", Stars = 2256055, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-01.fits", Stars = 3323320, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-02.fits", Stars = 3841638, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-03.fits", Stars = 3790819, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-04.fits", Stars = 2005120, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-05.fits", Stars = 3104494, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-06.fits", Stars = 1185054, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-07.fits", Stars = 1986688, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-08.fits", Stars = 1618319, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-09.fits", Stars = 1133721, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-10.fits", Stars = 2298507, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-11.fits", Stars = 1655858, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-12.fits", Stars = 3772894, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-13.fits", Stars = 3793438, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-14.fits", Stars = 3756628, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-15.fits", Stars = 3670023, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-16.fits", Stars = 1282483, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-17.fits", Stars = 2169674, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-18.fits", Stars = 1289502, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-19.fits", Stars = 2022788, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-20.fits", Stars = 3357278, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-21.fits", Stars = 2484373, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-22.fits", Stars = 3764638, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-23.fits", Stars = 3864289, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-24.fits", Stars = 2236482, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-25.fits", Stars = 1589549, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-26.fits", Stars = 1494824, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-27.fits", Stars = 1104582, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-28.fits", Stars = 3880994, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-29.fits", Stars = 3379579, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-30.fits", Stars = 3878614, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-31.fits", Stars = 3757822, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-32.fits", Stars = 2332744, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-33.fits", Stars = 1254417, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-34.fits", Stars = 1998528, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-35.fits", Stars = 1255574, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-36.fits", Stars = 3830615, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-37.fits", Stars = 3759569, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-38.fits", Stars = 3737829, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-39.fits", Stars = 3509850, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-40.fits", Stars = 3876461, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-41.fits", Stars = 3713578, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-42.fits", Stars = 3879577, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-43.fits", Stars = 3225372, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-44.fits", Stars = 3090302, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-45.fits", Stars = 3815940, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-46.fits", Stars = 1816256, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4201-47.fits", Stars = 3180723, MinArcMin = 2.8, MaxArcMin = 4 });
            indexes.Add(new IndexFile { Name = "index-4200-00.fits", Stars = 2372991, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-01.fits", Stars = 4822998, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-02.fits", Stars = 6847239, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-03.fits", Stars = 6611657, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-04.fits", Stars = 2062949, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-05.fits", Stars = 3846837, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-06.fits", Stars = 1185762, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-07.fits", Stars = 2029990, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-08.fits", Stars = 1625601, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-09.fits", Stars = 1134668, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-10.fits", Stars = 2372072, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-11.fits", Stars = 1664272, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-12.fits", Stars = 6429526, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-13.fits", Stars = 6707643, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-14.fits", Stars = 6385976, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-15.fits", Stars = 6038324, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-16.fits", Stars = 1283824, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-17.fits", Stars = 2233260, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-18.fits", Stars = 1290771, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-19.fits", Stars = 2065373, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-20.fits", Stars = 4832600, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-21.fits", Stars = 2724136, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-22.fits", Stars = 6519648, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-23.fits", Stars = 7126120, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-24.fits", Stars = 2326402, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-25.fits", Stars = 1596062, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-26.fits", Stars = 1498467, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-27.fits", Stars = 1105459, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-28.fits", Stars = 7731401, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-29.fits", Stars = 4704792, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-30.fits", Stars = 7561763, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-31.fits", Stars = 6278599, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-32.fits", Stars = 2736782, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-33.fits", Stars = 1255428, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-34.fits", Stars = 2050435, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-35.fits", Stars = 1256763, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-36.fits", Stars = 6786687, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-37.fits", Stars = 6416517, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-38.fits", Stars = 6349340, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-39.fits", Stars = 5397007, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-40.fits", Stars = 7493760, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-41.fits", Stars = 6179254, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-42.fits", Stars = 7711432, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-43.fits", Stars = 4266819, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-44.fits", Stars = 3672283, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-45.fits", Stars = 6609442, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-46.fits", Stars = 1830481, MinArcMin = 2, MaxArcMin = 2.8 });
            indexes.Add(new IndexFile { Name = "index-4200-47.fits", Stars = 3995997, MinArcMin = 2, MaxArcMin = 2.8 });
            return indexes;
            //SendWebRequest(INDEXESURL);
            //SendWebRequest(WIDEFIELDINDEXESURL);
        }

        private ObservableCollection<IndexFile> _indexes;
        public ObservableCollection<IndexFile> Indexes {
            get {
                if (_indexes == null) {
                    _indexes = new ObservableCollection<IndexFile>();
                }
                return _indexes;
            }
            set {
                _indexes = value;
            }
        }

        /*public string GetDirectoryListingRegexForUrl(string url) {
            
                return "<a href=\".*\">(?<name>.*.fits)</a>";
           
        }
        public void SendWebRequest(string url) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                using (StreamReader reader = new StreamReader(response.GetResponseStream())) {
                    string html = reader.ReadToEnd();
                    Regex regex = new Regex(GetDirectoryListingRegexForUrl(url));
                    MatchCollection matches = regex.Matches(html);
                    if (matches.Count > 0) {
                        List<string> bla = new List<string>();
                        foreach (Match match in matches) {
                            if (match.Success) {
                                Indexes.Add(match.Groups["name"].ToString());
                            }
                        }
                    }
                }
            }

        }
        */

        public class IndexFile {
            public string Name { get; set; }
            public int Stars { get; set; }
            public double MinArcMin { get; set; }
            public double MaxArcMin { get; set; }
        }

    }
}
