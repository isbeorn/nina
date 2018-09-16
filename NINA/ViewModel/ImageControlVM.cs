using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyTelescope;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Behaviors;
using NINA.Utility.Enum;
using NINA.Utility.Mediator;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using NINA.Utility.WindowService;
using nom.tam.fits;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NINA.ViewModel {

    internal class ImageControlVM : DockableVM, ICameraConsumer, ITelescopeConsumer {

        public ImageControlVM(IProfileService profileService, ICameraMediator cameraMediator, ITelescopeMediator telescopeMediator, IImagingMediator imagingMediator, IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            Title = "LblImage";

            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);

            this.telescopeMediator = telescopeMediator;
            this.telescopeMediator.RegisterConsumer(this);

            this.imagingMediator = imagingMediator;
            this.applicationStatusMediator = applicationStatusMediator;

            CanClose = false;
            AutoStretch = false;
            DetectStars = false;
            ShowCrossHair = false;
            ShowBahtinovAnalyzer = false;
            ShowSubSampler = false;

            _progress = new Progress<ApplicationStatus>(p => Status = p);

            PrepareImageCommand = new AsyncCommand<bool>(() => PrepareImageHelper());
            PlateSolveImageCommand = new AsyncCommand<bool>(() => PlateSolveImage(), (object o) => Image != null);
            CancelPlateSolveImageCommand = new RelayCommand(CancelPlateSolveImage);
            DragStartCommand = new RelayCommand(BahtinovDragStart);
            DragStopCommand = new RelayCommand(BahtinovDragStop);
            DragMoveCommand = new RelayCommand(BahtinovDragMove);
            SubSampleDragStartCommand = new RelayCommand(SubSampleDragStart);
            SubSampleDragStopCommand = new RelayCommand(SubSampleDragStop);
            SubSampleDragMoveCommand = new RelayCommand(SubSampleDragMove);
            InspectAberrationCommand = new RelayCommand(InspectAberration, (object o) => Image != null);

            BahtinovRectangle = new ObservableRectangle(-1, -1, 200, 200);
            SubSampleRectangle = new ObservableRectangle(-1, -1, 600, 600);
            BahtinovRectangle.PropertyChanged += Rectangle_PropertyChanged;
            SubSampleRectangle.PropertyChanged += SubSampleRectangle_PropertyChanged;
        }

        private void InspectAberration(object obj) {
            try {
                var vm = new AberrationInspectorVM(profileService, Image);
                var service = WindowServiceFactory.Create();
                service.Show(vm, Locale.Loc.Instance["LblAberrationInspector"], ResizeMode.CanResize, WindowStyle.ToolWindow);
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
            }
        }

        private void Rectangle_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (BahtinovRectangle.Width > (Image?.Width * 0.8)) {
                BahtinovRectangle.Width = Image.Width * 0.8;
            }
            if (BahtinovRectangle.Height > (Image?.Height * 0.8)) {
                BahtinovRectangle.Height = Image.Height * 0.8;
            }
            BahtinovDragMove(new DragResult() { Delta = new Vector(0, 0), Mode = DragMode.Move });
        }

        private void SubSampleRectangle_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (SubSampleRectangle.Width > (Image?.Width * 0.8)) {
                SubSampleRectangle.Width = Image.Width * 0.8;
            }
            if (SubSampleRectangle.Height > (Image?.Height * 0.8)) {
                SubSampleRectangle.Height = Image.Height * 0.8;
            }
            SubSampleDragMove(new DragResult() { Delta = new Vector(0, 0), Mode = DragMode.Move });
        }

        private bool _showBahtinovAnalyzer;

        public bool ShowBahtinovAnalyzer {
            get {
                return _showBahtinovAnalyzer;
            }
            set {
                _showBahtinovAnalyzer = value;
                if (value) {
                    ShowSubSampler = false;
                    ShowCrossHair = false;
                    BahtinovDragMove(new DragResult() { Delta = new Vector(0, 0), Mode = DragMode.Move });
                }
                RaisePropertyChanged();
            }
        }

        private ObservableRectangle _rectangle;

        public ObservableRectangle BahtinovRectangle {
            get {
                return _rectangle;
            }
            set {
                _rectangle = value;
                RaisePropertyChanged();
            }
        }

        private ObservableRectangle _subSampleRectangle;

        public ObservableRectangle SubSampleRectangle {
            get {
                return _subSampleRectangle;
            }
            set {
                _subSampleRectangle = value;
                RaisePropertyChanged();
            }
        }

        public double DragResizeBoundary { get; } = 10;

        private void BahtinovDragStart(object obj) {
        }

        private void BahtinovDragStop(object obj) {
        }

        private void BahtinovDragMove(object obj) {
            BahtinovRectangle.PropertyChanged -= Rectangle_PropertyChanged;
            if (ShowBahtinovAnalyzer && Image != null) {
                var dragResult = (DragResult)obj;

                if (dragResult.Mode == DragMode.Move) {
                    MoveRectangleInBounds(BahtinovRectangle, dragResult.Delta);
                } else {
                    ResizeRectangleBounds(BahtinovRectangle, dragResult.Delta, dragResult.Mode);
                }

                if (!IsLiveViewEnabled) {
                    AnalyzeBahtinov();
                }

                BahtinovRectangle.PropertyChanged += Rectangle_PropertyChanged;
            }
        }

        private void AnalyzeBahtinov() {
            /* Get Pixels */
            var crop = new CroppedBitmap(Image, new Int32Rect((int)BahtinovRectangle.X, (int)BahtinovRectangle.Y, (int)BahtinovRectangle.Width, (int)BahtinovRectangle.Height));
            BahtinovImage = new BahtinovAnalysis(crop, profileService.ActiveProfile.ColorSchemaSettings.BackgroundColor).GrabBahtinov();
        }

        private void SubSampleDragStart(object obj) {
        }

        private void SubSampleDragStop(object obj) {
        }

        private void SubSampleDragMove(object obj) {
            if (ShowSubSampler && Image != null) {
                SubSampleRectangle.PropertyChanged -= SubSampleRectangle_PropertyChanged;

                var dragResult = (DragResult)obj;

                if (dragResult.Mode == DragMode.Move) {
                    MoveRectangleInBounds(SubSampleRectangle, dragResult.Delta);
                } else {
                    ResizeRectangleBounds(SubSampleRectangle, dragResult.Delta, dragResult.Mode);
                }

                /* set subsample values */
                cameraMediator.SetSubSampleArea(
                    (int)SubSampleRectangle.X,
                    (int)SubSampleRectangle.Y,
                    (int)SubSampleRectangle.Width,
                    (int)SubSampleRectangle.Height
                );
                SubSampleRectangle.PropertyChanged += SubSampleRectangle_PropertyChanged;
            }
        }

        private void ResizeRectangleBounds(ObservableRectangle rect, Vector vector, DragMode mode) {
            if (mode == DragMode.Resize_Top_Left) {
                rect.X += vector.X;
                rect.Y += vector.Y;
                rect.Width -= vector.X;
                rect.Height -= vector.Y;
            } else if (mode == DragMode.Resize_Top_Right) {
                rect.Y += vector.Y;
                rect.Width += vector.X;
                rect.Height -= vector.Y;
            } else if (mode == DragMode.Resize_Bottom_Left) {
                rect.X += vector.X;
                rect.Width -= vector.X;
                rect.Height += vector.Y;
            } else if (mode == DragMode.Resize_Left) {
                rect.X += vector.X;
                rect.Width -= vector.X;
            } else if (mode == DragMode.Resize_Right) {
                rect.Width += vector.X;
            } else if (mode == DragMode.Resize_Top) {
                rect.Y += vector.Y;
                rect.Height -= vector.Y;
            } else if (mode == DragMode.Resize_Bottom) {
                rect.Height += vector.Y;
            } else {
                rect.Width += vector.X;
                rect.Height += vector.Y;
            }

            CheckRectangleBounds(rect);
        }

        private void CheckRectangleBounds(ObservableRectangle rect) {
            rect.Width = Math.Round(rect.Width, 0);
            rect.Height = Math.Round(rect.Height, 0);
            /* Check boundaries */
            if (rect.X + rect.Width > Image.Width) {
                rect.X = Image.Width - rect.Width;
            }
            if (rect.Y + rect.Height > Image.Height) {
                rect.Y = Image.Height - rect.Height;
            }
            if (rect.X < 0) {
                rect.X = 0;
            }
            if (rect.Y < 0) {
                rect.Y = 0;
            }
            if (rect.Width > Image.Width) {
                rect.Width = Image.Width;
            }
            if (rect.Height > Image.Height) {
                rect.Height = Image.Height;
            }
            if (rect.Width < 20) {
                rect.Width = 20;
            }
            if (rect.Height < 20) {
                rect.Height = 20;
            }
        }

        private void MoveRectangleInBounds(ObservableRectangle rect, Vector vector) {
            rect.X += vector.X;
            rect.Y += vector.Y;
            CheckRectangleBounds(rect);
        }

        private BahtinovImage _bahtinovImage;

        public BahtinovImage BahtinovImage {
            get {
                return _bahtinovImage;
            }
            private set {
                _bahtinovImage = value;
                RaisePropertyChanged();
            }
        }

        public ICommand InspectAberrationCommand { get; private set; }
        public ICommand DragStartCommand { get; private set; }
        public ICommand DragStopCommand { get; private set; }
        public ICommand DragMoveCommand { get; private set; }
        public ICommand SubSampleDragStartCommand { get; private set; }
        public ICommand SubSampleDragStopCommand { get; private set; }
        public ICommand SubSampleDragMoveCommand { get; private set; }

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

        private async Task<bool> PlateSolveImage() {
            if (Image != null) {
                _plateSolveToken = new CancellationTokenSource();
                if (!AutoStretch) {
                    AutoStretch = true;
                }
                await PrepareImageHelper();
                var solver = new PlatesolveVM(profileService, cameraMediator, telescopeMediator, imagingMediator, applicationStatusMediator);
                solver.Image = Image;
                var service = WindowServiceFactory.Create();
                service.Show(solver, this.Title + " - " + solver.Title, System.Windows.ResizeMode.CanResize, System.Windows.WindowStyle.ToolWindow);
                await solver.Solve(Image, _progress, _plateSolveToken.Token);
                return true;
            } else {
                return false;
            }
        }

        private IProgress<ApplicationStatus> _progress;

        private void CancelPlateSolveImage(object o) {
            _plateSolveToken?.Cancel();
        }

        private CancellationTokenSource _plateSolveToken;

        private Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        private ImageArray _imgArr;

        public ImageArray ImgArr {
            get {
                return _imgArr;
            }
            private set {
                _imgArr = value;
                RaisePropertyChanged();
            }
        }

        private ImageHistoryVM _imgHistoryVM;

        public ImageHistoryVM ImgHistoryVM {
            get {
                if (_imgHistoryVM == null) {
                    _imgHistoryVM = new ImageHistoryVM(profileService);
                }
                return _imgHistoryVM;
            }
            set {
                _imgHistoryVM = value;
                RaisePropertyChanged();
            }
        }

        private ImageStatisticsVM _imgStatisticsVM;

        public ImageStatisticsVM ImgStatisticsVM {
            get {
                if (_imgStatisticsVM == null) {
                    _imgStatisticsVM = new ImageStatisticsVM(profileService);
                }
                return _imgStatisticsVM;
            }
            set {
                _imgStatisticsVM = value;
                RaisePropertyChanged();
            }
        }

        private BitmapSource _image;

        public BitmapSource Image {
            get {
                return _image;
            }
            private set {
                _image = value;
                if (_image != null) {
                    ResizeRectangleToImageSize(_image, BahtinovRectangle);
                    // when subsampling is enabled and a new image is loaded disable the subsampler
                    // so it doesn't get resized
                    if (cameraInfo.IsSubSampleEnabled) {
                        ShowSubSampler = false;
                    } else {
                        ResizeRectangleToImageSize(_image, SubSampleRectangle);
                    }
                }
                RaisePropertyChanged();
            }
        }

        private void ResizeRectangleToImageSize(BitmapSource image, ObservableRectangle rectangle) {
            if (rectangle.X < 0 || rectangle.Y < 0
                || rectangle.X + rectangle.Width > image.PixelWidth
                || rectangle.Y + rectangle.Height > image.PixelHeight) {
                rectangle.X = image.PixelWidth / 2 - rectangle.Width / 2;
                rectangle.Y = image.PixelHeight / 2 - rectangle.Height / 2;
            }
        }

        private bool _autoStretch;

        public bool AutoStretch {
            get {
                return _autoStretch;
            }
            set {
                _autoStretch = value;
                if (!_autoStretch && _detectStars) { _detectStars = false; RaisePropertyChanged(nameof(DetectStars)); }
                RaisePropertyChanged();
            }
        }

        private async Task<bool> PrepareImageHelper() {
            _prepImageCancellationSource?.Cancel();
            try {
                _prepImageTask?.Wait(_prepImageCancellationSource.Token);
            } catch (OperationCanceledException) {
            }
            _prepImageCancellationSource = new CancellationTokenSource();
            _prepImageTask = PrepareImage(ImgArr, _prepImageCancellationSource.Token);
            await _prepImageTask;
            return true;
        }

        public AsyncCommand<bool> PrepareImageCommand { get; private set; }

        private Task _prepImageTask;
        private CancellationTokenSource _prepImageCancellationSource;

        private bool _showCrossHair;

        public bool ShowCrossHair {
            get {
                return _showCrossHair;
            }
            set {
                _showCrossHair = value;
                if (value) {
                    ShowBahtinovAnalyzer = false;
                    ShowSubSampler = false;
                }
                RaisePropertyChanged();
            }
        }

        private bool _detectStars;

        public bool DetectStars {
            get {
                return _detectStars;
            }
            set {
                _detectStars = value;
                if (_detectStars) { _autoStretch = true; RaisePropertyChanged(nameof(AutoStretch)); }
                RaisePropertyChanged();
            }
        }

        private ApplicationStatus _status;

        public ApplicationStatus Status {
            get {
                return _status;
            }
            set {
                _status = value;
                _status.Source = Title;
                RaisePropertyChanged();

                applicationStatusMediator.StatusUpdate(_status);
            }
        }

        public IAsyncCommand PlateSolveImageCommand { get; private set; }

        public ICommand CancelPlateSolveImageCommand { get; private set; }

        private bool _showSubSampler;

        public bool ShowSubSampler {
            get {
                return _showSubSampler;
            }
            set {
                _showSubSampler = value;
                if (value) {
                    ShowBahtinovAnalyzer = false;
                    ShowCrossHair = false;
                    SubSampleDragMove(new DragResult() { Delta = new Vector(0, 0), Mode = DragMode.Move });
                }
                RaisePropertyChanged();
            }
        }

        public bool IsLiveViewEnabled { get; internal set; }

        public static SemaphoreSlim ss = new SemaphoreSlim(1, 1);
        private ICameraMediator cameraMediator;
        private CameraInfo cameraInfo = DeviceInfo.CreateDefaultInstance<CameraInfo>();
        private ITelescopeMediator telescopeMediator;
        private TelescopeInfo telescopeInfo = DeviceInfo.CreateDefaultInstance<TelescopeInfo>();
        private IImagingMediator imagingMediator;
        private IApplicationStatusMediator applicationStatusMediator;

        public async Task<BitmapSource> PrepareImage(
                ImageArray iarr,
                CancellationToken token,
                bool bSave = false,
                ImageParameters parameters = null) {
            BitmapSource source = null;
            try {
                await ss.WaitAsync(token);

                if (iarr != null) {
                    _progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblPrepareImage"] });
                    source = ImageAnalysis.CreateSourceFromArray(iarr, System.Windows.Media.PixelFormats.Gray16);

                    if (AutoStretch) {
                        _progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblStretchImage"] });
                        source = await StretchAsync(iarr, source, profileService.ActiveProfile.ImageSettings.AutoStretchFactor);
                    }

                    if (DetectStars) {
                        var analysis = new ImageAnalysis(source, iarr);
                        await analysis.DetectStarsAsync(_progress, token);

                        if (profileService.ActiveProfile.ImageSettings.AnnotateImage) {
                            source = analysis.GetAnnotatedImage();
                        }

                        iarr.Statistics.HFR = analysis.AverageHFR;
                        iarr.Statistics.DetectedStars = analysis.DetectedStars;
                    }

                    source = ImageAnalysis.Convert16BppTo8BppSource(source);

                    if (iarr.IsBayered && profileService.ActiveProfile.ImageSettings.DebayerImage) {
                        _progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblDebayeringImage"] });
                        source = ImageAnalysis.Debayer(source, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale);
                    }

                    if (parameters != null) {
                        iarr.Statistics.ExposureTime = parameters.ExposureTime;
                    }

                    await _dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                        Image = null;
                        ImgArr = null;
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        ImgArr = iarr;
                        Image = source;
                        ImgStatisticsVM.Add(ImgArr.Statistics);
                        ImgHistoryVM.Add(iarr.Statistics);
                    }));

                    AnalyzeBahtinov();

                    if (bSave) {
                        await SaveToDisk(parameters, token);
                    }
                }
            } finally {
                _progress.Report(new ApplicationStatus() { Status = string.Empty });
                ss.Release();
            }
            return source;
        }

        public static async Task<BitmapSource> StretchAsync(ImageArray iarr, BitmapSource source, double factor) {
            return await Task<BitmapSource>.Run(() => Stretch(iarr.Statistics.Mean, source, System.Windows.Media.PixelFormats.Gray16, factor));
        }

        public static async Task<BitmapSource> StretchAsync(double mean, BitmapSource source, double factor) {
            return await Task<BitmapSource>.Run(() => Stretch(mean, source, System.Windows.Media.PixelFormats.Gray16, factor));
        }

        public static async Task<BitmapSource> StretchAsync(double mean, BitmapSource source, System.Windows.Media.PixelFormat pf, double factor) {
            return await Task<BitmapSource>.Run(() => Stretch(mean, source, pf, factor));
        }

        public static BitmapSource Stretch(double mean, BitmapSource source, double factor) {
            return Stretch(mean, source, System.Windows.Media.PixelFormats.Gray16, factor);
        }

        public static BitmapSource Stretch(double mean, BitmapSource source, System.Windows.Media.PixelFormat pf, double factor) {
            using (var img = ImageAnalysis.BitmapFromSource(source)) {
                return Stretch(mean, img, pf, factor);
            }
        }

        public static BitmapSource Stretch(double mean, System.Drawing.Bitmap img, System.Windows.Media.PixelFormat pf, double factor) {
            using (MyStopWatch.Measure()) {
                var filter = ImageAnalysis.GetColorRemappingFilter(mean, factor);
                filter.ApplyInPlace(img);

                var source = ImageAnalysis.ConvertBitmap(img, pf);
                source.Freeze();
                return source;
            }
        }

        public async Task<bool> SaveToDisk(ImageParameters parameters, CancellationToken token) {
            var filter = parameters.FilterName;
            var framenr = parameters.ExposureNumber;
            var success = false;
            try {
                using (MyStopWatch.Measure()) {
                    success = await SaveToDiskAsync(parameters, token);
                }
            } catch (OperationCanceledException ex) {
                throw new OperationCanceledException(ex.Message);
            } catch (Exception ex) {
                Notification.ShowError(ex.Message);
                Logger.Error(ex);
            } finally {
                _progress.Report(new ApplicationStatus() { Status = string.Empty });
            }
            return success;
        }

        private async Task<bool> SaveToDiskAsync(ImageParameters parameters, CancellationToken token) {
            _progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblSave"] });
            await Task.Run(() => {
                ImagePatterns p = new ImagePatterns();

                p.Set(ImagePatternKeys.Filter, parameters.FilterName);
                p.Set(ImagePatternKeys.ExposureTime, parameters.ExposureTime);
                p.Set(ImagePatternKeys.Date, DateTime.Now.ToString("yyyy-MM-dd"));
                p.Set(ImagePatternKeys.Time, DateTime.Now.ToString("HH-mm-ss"));
                p.Set(ImagePatternKeys.DateTime, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
                p.Set(ImagePatternKeys.FrameNr, parameters.ExposureNumber);
                p.Set(ImagePatternKeys.ImageType, parameters.ImageType);

                if (parameters.Binning == string.Empty) {
                    p.Set(ImagePatternKeys.Binning, "1x1");
                } else {
                    p.Set(ImagePatternKeys.Binning, parameters.Binning);
                }

                p.Set(ImagePatternKeys.SensorTemp, cameraInfo.Temperature);
                p.Set(ImagePatternKeys.TargetName, parameters.TargetName);
                p.Set(ImagePatternKeys.Gain, cameraInfo.Gain);
                p.Set(ImagePatternKeys.RMS, parameters.RecordedRMS.Total);
                p.Set(ImagePatternKeys.RMSArcSec, parameters.RecordedRMS.Total * parameters.RecordedRMS.Scale);

                string path = Path.GetFullPath(profileService.ActiveProfile.ImageFileSettings.FilePath);
                string filename = p.GetImageFileString(profileService.ActiveProfile.ImageFileSettings.FilePattern);
                string completefilename = Path.Combine(path, filename);

                Stopwatch sw = Stopwatch.StartNew();
                var fileType = profileService.ActiveProfile.ImageFileSettings.FileType;
                if (ImgArr.RAWData != null) {
                    completefilename = SaveRAW(completefilename);
                    fileType = FileTypeEnum.RAW;
                } else {
                    if (profileService.ActiveProfile.ImageFileSettings.FileType == FileTypeEnum.FITS) {
                        if (parameters.ImageType == "SNAP") parameters.ImageType = "LIGHT";
                        completefilename = SaveFits(completefilename, parameters);
                    } else if (profileService.ActiveProfile.ImageFileSettings.FileType == FileTypeEnum.TIFF) {
                        completefilename = SaveTiff(completefilename, TiffCompressOption.None);
                    } else if (profileService.ActiveProfile.ImageFileSettings.FileType == FileTypeEnum.TIFF_ZIP) {
                        completefilename = SaveTiff(completefilename, TiffCompressOption.Zip);
                    } else if (profileService.ActiveProfile.ImageFileSettings.FileType == FileTypeEnum.TIFF_LZW) {
                        completefilename = SaveTiff(completefilename, TiffCompressOption.Lzw);
                    } else if (profileService.ActiveProfile.ImageFileSettings.FileType == FileTypeEnum.XISF) {
                        if (parameters.ImageType == "SNAP") parameters.ImageType = "LIGHT";
                        completefilename = SaveXisf(completefilename, parameters);
                    } else {
                        completefilename = SaveTiff(completefilename, TiffCompressOption.None);
                    }
                }

                imagingMediator.OnImageSaved(
                    new ImageSavedEventArgs() {
                        PathToImage = new Uri(completefilename),
                        Image = Image,
                        FileType = fileType,
                        Mean = ImgArr.Statistics.Mean,
                        HFR = ImgArr.Statistics.HFR,
                        Duration = parameters.ExposureTime,
                        IsBayered = ImgArr.IsBayered,
                        Filter = parameters.FilterName,
                        StatisticsId = ImgArr.Statistics.Id
                    }
                );

                sw.Stop();
                Debug.Print("Time to save: " + sw.Elapsed);
                sw = null;
            });

            token.ThrowIfCancellationRequested();
            return true;
        }

        private byte[] GetEncodedFitsHeaderInternal(string keyword, string value, string comment) {
            /* Specification: http://archive.stsci.edu/fits/fits_standard/node29.html#SECTION00912100000000000000 */
            Encoding ascii = Encoding.GetEncoding("iso-8859-1"); /* Extended ascii */
            var header = keyword.ToUpper().PadRight(8) + "=" + value.PadLeft(21) + " / " + comment.PadRight(47);
            return ascii.GetBytes(header);
        }

        private byte[] GetEncodedFitsHeader(string keyword, string value, string comment) {
            return GetEncodedFitsHeaderInternal(keyword, "'" + value + "'", comment);
        }

        private byte[] GetEncodedFitsHeader(string keyword, int value, string comment) {
            return GetEncodedFitsHeaderInternal(keyword, value.ToString(CultureInfo.InvariantCulture), comment);
        }

        private byte[] GetEncodedFitsHeader(string keyword, double value, string comment) {
            return GetEncodedFitsHeaderInternal(keyword, value.ToString(CultureInfo.InvariantCulture), comment);
        }

        private byte[] GetEncodedFitsHeader(string keyword, bool value, string comment) {
            return GetEncodedFitsHeaderInternal(keyword, value ? "T" : "F", comment);
        }

        private string SaveRAW(string path) {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var uniquePath = Utility.Utility.GetUniqueFilePath(path + "." + ImgArr.RAWType);
            File.WriteAllBytes(uniquePath, ImgArr.RAWData);
            return uniquePath;
        }

        private string SaveFits(string path, ImageParameters parameters) {
            try {
                FITS f = new FITS(
                    this.ImgArr.FlatArray,
                    this.ImgArr.Statistics.Width,
                    this.ImgArr.Statistics.Height,
                    parameters.ImageType,
                    parameters.ExposureTime
                );

                f.AddHeaderCard("FOCALLEN", profileService.ActiveProfile.TelescopeSettings.FocalLength, "");
                f.AddHeaderCard("XPIXSZ", profileService.ActiveProfile.CameraSettings.PixelSize, "");
                f.AddHeaderCard("YPIXSZ", profileService.ActiveProfile.CameraSettings.PixelSize, "");
                f.AddHeaderCard("SITELAT", Astrometry.HoursToHMS(profileService.ActiveProfile.AstrometrySettings.Latitude), "");
                f.AddHeaderCard("SITELONG", Astrometry.HoursToHMS(profileService.ActiveProfile.AstrometrySettings.Longitude), "");

                if (!string.IsNullOrEmpty(parameters.FilterName)) {
                    f.AddHeaderCard("FILTER", parameters.FilterName, "");
                }

                if (cameraInfo.BinX > 0) {
                    f.AddHeaderCard("XBINNING", cameraInfo.BinX, "");
                }
                if (cameraInfo.BinY > 0) {
                    f.AddHeaderCard("YBINNING", cameraInfo.BinY, "");
                }
                f.AddHeaderCard("EGAIN", cameraInfo.Gain, "");

                if (telescopeInfo != null) {
                    f.AddHeaderCard("OBJCTRA", Astrometry.HoursToFitsHMS(telescopeInfo.RightAscension), "");
                    f.AddHeaderCard("OBJCTDEC", Astrometry.DegreesToFitsDMS(telescopeInfo.Declination), "");
                }

                var temp = cameraInfo.Temperature;
                if (!double.IsNaN(temp)) {
                    f.AddHeaderCard("TEMPERAT", temp, "");
                    f.AddHeaderCard("CCD-TEMP", temp, "");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var uniquePath = Utility.Utility.GetUniqueFilePath(path + ".fits");

                using (FileStream fs = new FileStream(uniquePath, FileMode.Create)) {
                    f.Write(fs);
                }

                return uniquePath;
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(Locale.Loc.Instance["LblImageFileError"] + Environment.NewLine + ex.Message);
                return string.Empty;
            }
        }

        private string SaveTiff(String path, TiffCompressOption c) {
            try {
                BitmapSource bmpSource = ImageAnalysis.CreateSourceFromArray(ImgArr, System.Windows.Media.PixelFormats.Gray16);

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var uniquePath = Utility.Utility.GetUniqueFilePath(path + ".tif");

                using (FileStream fs = new FileStream(uniquePath, FileMode.Create)) {
                    TiffBitmapEncoder encoder = new TiffBitmapEncoder();
                    encoder.Compression = c;
                    encoder.Frames.Add(BitmapFrame.Create(bmpSource));
                    encoder.Save(fs);
                }
                return uniquePath;
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(Locale.Loc.Instance["LblImageFileError"] + Environment.NewLine + ex.Message);
                return string.Empty;
            }
        }

        private string SaveXisf(String path, ImageParameters parameters) {
            try {
                var header = new XISFHeader();

                header.AddEmbeddedImage(ImgArr, parameters.ImageType);

                header.AddImageProperty(XISFImageProperty.Observation.Time.Start, DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture));
                header.AddImageProperty(XISFImageProperty.Observation.Location.Latitude, profileService.ActiveProfile.AstrometrySettings.Latitude.ToString(CultureInfo.InvariantCulture));
                header.AddImageProperty(XISFImageProperty.Observation.Location.Longitude, profileService.ActiveProfile.AstrometrySettings.Longitude.ToString(CultureInfo.InvariantCulture));
                header.AddImageProperty(XISFImageProperty.Instrument.Telescope.FocalLength, profileService.ActiveProfile.TelescopeSettings.FocalLength.ToString(CultureInfo.InvariantCulture));
                header.AddImageProperty(XISFImageProperty.Instrument.Sensor.XPixelSize, profileService.ActiveProfile.CameraSettings.PixelSize.ToString(CultureInfo.InvariantCulture));
                header.AddImageProperty(XISFImageProperty.Instrument.Sensor.YPixelSize, profileService.ActiveProfile.CameraSettings.PixelSize.ToString(CultureInfo.InvariantCulture));

                if (telescopeInfo != null) {
                    header.AddImageProperty(XISFImageProperty.Instrument.Telescope.Name, telescopeInfo.Name);

                    /* Location */
                    header.AddImageProperty(XISFImageProperty.Observation.Location.Elevation, telescopeInfo.SiteElevation.ToString(CultureInfo.InvariantCulture));
                    /* convert to degrees */
                    var RA = telescopeInfo.RightAscension * 360 / 24;
                    header.AddImageProperty(XISFImageProperty.Observation.Center.RA, RA.ToString(CultureInfo.InvariantCulture), string.Empty, false);
                    header.AddImageFITSKeyword(XISFImageProperty.Observation.Center.RA[2], Astrometry.HoursToFitsHMS(telescopeInfo.RightAscension));

                    header.AddImageProperty(XISFImageProperty.Observation.Center.Dec, telescopeInfo.Declination.ToString(CultureInfo.InvariantCulture), string.Empty, false);
                    header.AddImageFITSKeyword(XISFImageProperty.Observation.Center.Dec[2], Astrometry.DegreesToFitsDMS(telescopeInfo.Declination));
                }

                header.AddImageProperty(XISFImageProperty.Instrument.Camera.Name, cameraInfo.Name);

                if (cameraInfo.Gain > 0) {
                    /* Add offset as a comment. There is no dedicated keyword for this */
                    string offset = string.Empty;
                    if (cameraInfo.Offset > 0) {
                        offset = cameraInfo.Offset.ToString(CultureInfo.InvariantCulture);
                    }
                    header.AddImageProperty(XISFImageProperty.Instrument.Camera.Gain, cameraInfo.Gain.ToString(CultureInfo.InvariantCulture), offset);
                }

                if (cameraInfo.BinX > 0) {
                    header.AddImageProperty(XISFImageProperty.Instrument.Camera.XBinning, cameraInfo.BinX.ToString(CultureInfo.InvariantCulture));
                }
                if (cameraInfo.BinY > 0) {
                    header.AddImageProperty(XISFImageProperty.Instrument.Camera.YBinning, cameraInfo.BinY.ToString(CultureInfo.InvariantCulture));
                }

                var temp = cameraInfo.Temperature;
                if (!double.IsNaN(temp)) {
                    header.AddImageProperty(XISFImageProperty.Instrument.Sensor.Temperature, temp.ToString(CultureInfo.InvariantCulture));
                }

                if (!string.IsNullOrEmpty(parameters.FilterName)) {
                    header.AddImageProperty(XISFImageProperty.Instrument.Filter.Name, parameters.FilterName);
                }

                header.AddImageProperty(XISFImageProperty.Instrument.ExposureTime, parameters.ExposureTime.ToString(System.Globalization.CultureInfo.InvariantCulture));

                XISF img = new XISF(header);

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var uniquePath = Utility.Utility.GetUniqueFilePath(path + ".xisf");

                using (FileStream fs = new FileStream(uniquePath, FileMode.Create)) {
                    img.Save(fs);
                }
                return uniquePath;
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(Locale.Loc.Instance["LblImageFileError"] + Environment.NewLine + ex.Message);
                return string.Empty;
            }
        }

        public void UpdateDeviceInfo(CameraInfo cameraInfo) {
            this.cameraInfo = cameraInfo;
        }

        public void UpdateDeviceInfo(TelescopeInfo telescopeInfo) {
            this.telescopeInfo = telescopeInfo;
        }
    }

    public class ImageParameters {
        public string FilterName { get; internal set; }
        public int ExposureNumber { get; internal set; }
        public string ImageType { get; internal set; }
        public string Binning { get; internal set; }
        public double ExposureTime { get; internal set; }
        public string TargetName { get; internal set; }
        public RMS RecordedRMS { get; internal set; }
    }
}