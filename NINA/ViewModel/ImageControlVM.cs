using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyTelescope;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Enum;
using NINA.Utility.Mediator;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
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

    public class ImageControlVM : DockableVM {

        public ImageControlVM() {
            Title = "LblImage";

            ContentId = nameof(ImageControlVM);
            CanClose = false;
            AutoStretch = false;
            DetectStars = false;
            ShowCrossHair = false;
            ShowBahtinovAnalyzer = false;
            ShowSubSampler = false;

            _progress = new Progress<ApplicationStatus>(p => Status = p);

            PrepareImageCommand = new AsyncCommand<bool>(() => PrepareImageHelper());
            PlateSolveImageCommand = new AsyncCommand<bool>(() => PlateSolveImage());
            CancelPlateSolveImageCommand = new RelayCommand(CancelPlateSolveImage);
            DragStartCommand = new RelayCommand(BahtinovDragStart);
            DragStopCommand = new RelayCommand(BahtinovDragStop);
            DragMoveCommand = new RelayCommand(BahtinovDragMove);
            SubSampleDragStartCommand = new RelayCommand(SubSampleDragStart);
            SubSampleDragStopCommand = new RelayCommand(SubSampleDragStop);
            SubSampleDragMoveCommand = new RelayCommand(SubSampleDragMove);

            RegisterMediatorMessages();

            BahtinovRectangle = new ObservableRectangle(-1, -1, 200, 200);
            SubSampleRectangle = new ObservableRectangle(-1, -1, 600, 600);
            BahtinovRectangle.PropertyChanged += Rectangle_PropertyChanged;
            SubSampleRectangle.PropertyChanged += SubSampleRectangle_PropertyChanged;
        }

        private void Rectangle_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (BahtinovRectangle.Width > (Image?.Width * 0.8)) {
                BahtinovRectangle.Width = Image.Width * 0.8;
            }
            if (BahtinovRectangle.Height > (Image?.Height * 0.8)) {
                BahtinovRectangle.Height = Image.Height * 0.8;
            }
            BahtinovDragMove(new Vector(0, 0));
        }

        private void SubSampleRectangle_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (SubSampleRectangle.Width > (Image?.Width * 0.8)) {
                SubSampleRectangle.Width = Image.Width * 0.8;
            }
            if (SubSampleRectangle.Height > (Image?.Height * 0.8)) {
                SubSampleRectangle.Height = Image.Height * 0.8;
            }
            SubSampleDragMove(new Vector(0, 0));
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
                    BahtinovDragMove(new Vector(0, 0));
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

        private void BahtinovDragStart(object obj) {
        }

        private void BahtinovDragStop(object obj) {
        }

        private void BahtinovDragMove(object obj) {
            BahtinovRectangle.PropertyChanged -= Rectangle_PropertyChanged;
            if (ShowBahtinovAnalyzer && Image != null) {
                MoveRectangleInBounds(BahtinovRectangle, (Vector)obj);

                /* Get Pixels */
                var crop = new CroppedBitmap(Image, new Int32Rect((int)BahtinovRectangle.X, (int)BahtinovRectangle.Y, (int)BahtinovRectangle.Width, (int)BahtinovRectangle.Height));
                BahtinovImage = new BahtinovAnalysis(crop).GrabBahtinov();
                BahtinovRectangle.PropertyChanged += Rectangle_PropertyChanged;
            }
        }

        private void SubSampleDragStart(object obj) {
        }

        private void SubSampleDragStop(object obj) {
        }

        private void SubSampleDragMove(object obj) {
            SubSampleRectangle.PropertyChanged -= SubSampleRectangle_PropertyChanged;
            if (ShowSubSampler && Image != null) {
                MoveRectangleInBounds(SubSampleRectangle, (Vector)obj);

                /* Get Pixels */
                Cam.SubSampleHeight = (int)SubSampleRectangle.Height;
                Cam.SubSampleWidth = (int)SubSampleRectangle.Width;
                Cam.SubSampleX = (int)SubSampleRectangle.X;
                Cam.SubSampleY = (int)SubSampleRectangle.Y;

                var crop = new CroppedBitmap(Image, new Int32Rect((int)SubSampleRectangle.X, (int)SubSampleRectangle.Y, (int)SubSampleRectangle.Width, (int)SubSampleRectangle.Height));
                //BahtinovImage = new BahtinovAnalysis(crop).GrabBahtinov();
                SubSampleRectangle.PropertyChanged += SubSampleRectangle_PropertyChanged;
            }
        }

        private void MoveRectangleInBounds(ObservableRectangle rect, Vector vector) {
            rect.X += vector.X;
            rect.Y += vector.Y;

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

        public ICommand DragStartCommand { get; private set; }
        public ICommand DragStopCommand { get; private set; }
        public ICommand DragMoveCommand { get; private set; }
        public ICommand SubSampleDragStartCommand { get; private set; }
        public ICommand SubSampleDragStopCommand { get; private set; }
        public ICommand SubSampleDragMoveCommand { get; private set; }

        private async Task<bool> PlateSolveImage() {
            if (Image != null) {
                _plateSolveToken = new CancellationTokenSource();
                if (!AutoStretch) {
                    AutoStretch = true;
                }
                await PrepareImageHelper();
                await Mediator.Instance.RequestAsync(new PlateSolveMessage() { Progress = _progress, Token = _plateSolveToken.Token, Image = Image });
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

        private void RegisterMediatorMessages() {
            Mediator.Instance.Register((object o) => {
                AutoStretch = (bool)o;
            }, MediatorMessages.ChangeAutoStretch);
            Mediator.Instance.Register((object o) => {
                DetectStars = (bool)o;
            }, MediatorMessages.ChangeDetectStars);

            Mediator.Instance.Register((object o) => {
                Cam = (ICamera)o;
            }, MediatorMessages.CameraChanged);
            Mediator.Instance.Register((object o) => {
                Telescope = (ITelescope)o;
            }, MediatorMessages.TelescopeChanged);

            Mediator.Instance.RegisterAsyncRequest(
                new SetImageMessageHandle(async (SetImageMessage msg) => {
                    ImgArr = msg.ImageArray;

                    await PrepareImage(ImgArr, new CancellationToken());
                    return true;
                })
            );
        }

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
                    _imgHistoryVM = new ImageHistoryVM();
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
                    _imgStatisticsVM = new ImageStatisticsVM();
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
                    if (Cam.EnableSubSample) {
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
                Mediator.Instance.Notify(MediatorMessages.AutoStrechChanged, _autoStretch);
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
                Mediator.Instance.Notify(MediatorMessages.DetectStarsChanged, _detectStars);
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

                Mediator.Instance.Request(new StatusUpdateMessage() { Status = _status });
            }
        }

        private ICamera Cam { get; set; }

        private ITelescope Telescope { get; set; }

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
                }
                RaisePropertyChanged();
            }
        }

        public static SemaphoreSlim ss = new SemaphoreSlim(1, 1);

        public async Task<BitmapSource> PrepareImage(
                ImageArray iarr,
                CancellationToken token,
                bool bSave = false,
                ImageParameters parameters = null) {
            BitmapSource source = null;
            try {
                await ss.WaitAsync(token);

                if (iarr != null) {
                    _progress.Report(new ApplicationStatus() { Status = "Preparing image" });
                    source = ImageAnalysis.CreateSourceFromArray(iarr, System.Windows.Media.PixelFormats.Gray16);

                    if (AutoStretch) {
                        _progress.Report(new ApplicationStatus() { Status = "Stretching image" });
                        source = await StretchAsync(iarr, source);
                    }

                    if (DetectStars) {
                        var analysis = new ImageAnalysis(source, iarr);
                        await analysis.DetectStarsAsync(_progress, token);

                        if (ProfileManager.Instance.ActiveProfile.ImageSettings.AnnotateImage) {
                            source = analysis.GetAnnotatedImage();
                        }

                        iarr.Statistics.HFR = analysis.AverageHFR;
                        iarr.Statistics.DetectedStars = analysis.DetectedStars;
                    }

                    if (iarr.IsBayered) {
                        _progress.Report(new ApplicationStatus() { Status = "Debayer image" });
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

                    BahtinovDragMove(new Vector(0, 0));

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

        public static async Task<BitmapSource> StretchAsync(ImageArray iarr, BitmapSource source) {
            return await Task<BitmapSource>.Run(() => Stretch(iarr.Statistics.Mean, source, System.Windows.Media.PixelFormats.Gray16));
        }

        public static async Task<BitmapSource> StretchAsync(double mean, BitmapSource source) {
            return await Task<BitmapSource>.Run(() => Stretch(mean, source, System.Windows.Media.PixelFormats.Gray16));
        }

        public static async Task<BitmapSource> StretchAsync(double mean, BitmapSource source, System.Windows.Media.PixelFormat pf) {
            return await Task<BitmapSource>.Run(() => Stretch(mean, source, pf));
        }

        public static BitmapSource Stretch(double mean, BitmapSource source) {
            return Stretch(mean, source, System.Windows.Media.PixelFormats.Gray16);
        }

        public static BitmapSource Stretch(double mean, BitmapSource source, System.Windows.Media.PixelFormat pf) {
            var img = ImageAnalysis.BitmapFromSource(source);
            return Stretch(mean, img, pf);
        }

        public static BitmapSource Stretch(double mean, System.Drawing.Bitmap img, System.Windows.Media.PixelFormat pf) {
            using (MyStopWatch.Measure()) {
                var filter = ImageAnalysis.GetColorRemappingFilter(mean, ProfileManager.Instance.ActiveProfile.ImageSettings.AutoStretchFactor);
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
            _progress.Report(new ApplicationStatus() { Status = "Saving..." });
            await Task.Run(async () => {
                List<OptionsVM.ImagePattern> p = new List<OptionsVM.ImagePattern>();

                p.Add(new OptionsVM.ImagePattern("$$FILTER$$", "Filtername", parameters.FilterName));

                p.Add(new OptionsVM.ImagePattern("$$EXPOSURETIME$$", "Exposure Time in seconds", string.Format("{0:0.00}", parameters.ExposureTime)));
                p.Add(new OptionsVM.ImagePattern("$$DATE$$", "Date with format YYYY-MM-DD", DateTime.Now.ToString("yyyy-MM-dd")));
                p.Add(new OptionsVM.ImagePattern("$$TIME$$", "Time with format HH-mm-ss", DateTime.Now.ToString("HH-mm-ss")));
                p.Add(new OptionsVM.ImagePattern("$$DATETIME$$", "Date with format YYYY-MM-DD_HH-mm-ss", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")));
                p.Add(new OptionsVM.ImagePattern("$$FRAMENR$$", "# of the Frame with format ####", string.Format("{0:0000}", parameters.ExposureNumber)));
                p.Add(new OptionsVM.ImagePattern("$$IMAGETYPE$$", "Light, Flat, Dark, Bias", parameters.ImageType));

                if (parameters.Binning == string.Empty) {
                    p.Add(new OptionsVM.ImagePattern("$$BINNING$$", "Binning of the camera", "1x1"));
                } else {
                    p.Add(new OptionsVM.ImagePattern("$$BINNING$$", "Binning of the camera", parameters.Binning));
                }

                p.Add(new OptionsVM.ImagePattern("$$SENSORTEMP$$", "Temperature of the Camera", string.Format("{0:00}", Cam?.Temperature)));

                p.Add(new OptionsVM.ImagePattern("$$TARGETNAME$$", "Target Name if available", parameters.TargetName));

                p.Add(new OptionsVM.ImagePattern("$$GAIN$$", "Camera Gain", Cam?.Gain.ToString() ?? string.Empty));

                string path = Path.GetFullPath(ProfileManager.Instance.ActiveProfile.ImageFileSettings.FilePath);
                string filename = Utility.Utility.GetImageFileString(p);
                string completefilename = Path.Combine(path, filename);

                Stopwatch sw = Stopwatch.StartNew();
                if (ProfileManager.Instance.ActiveProfile.ImageFileSettings.FileType == FileTypeEnum.FITS) {
                    if (parameters.ImageType == "SNAP") parameters.ImageType = "LIGHT";
                    completefilename = SaveFits(completefilename, parameters);
                } else if (ProfileManager.Instance.ActiveProfile.ImageFileSettings.FileType == FileTypeEnum.TIFF) {
                    completefilename = SaveTiff(completefilename, TiffCompressOption.None);
                } else if (ProfileManager.Instance.ActiveProfile.ImageFileSettings.FileType == FileTypeEnum.TIFF_ZIP) {
                    completefilename = SaveTiff(completefilename, TiffCompressOption.Zip);
                } else if (ProfileManager.Instance.ActiveProfile.ImageFileSettings.FileType == FileTypeEnum.TIFF_LZW) {
                    completefilename = SaveTiff(completefilename, TiffCompressOption.Lzw);
                } else if (ProfileManager.Instance.ActiveProfile.ImageFileSettings.FileType == FileTypeEnum.XISF) {
                    if (parameters.ImageType == "SNAP") parameters.ImageType = "LIGHT";
                    completefilename = SaveXisf(completefilename, parameters);
                } else {
                    completefilename = SaveTiff(completefilename, TiffCompressOption.None);
                }
                await Mediator.Instance.RequestAsync(
                    new AddThumbnailMessage() {
                        PathToImage = new Uri(completefilename),
                        Image = Image,
                        FileType = ProfileManager.Instance.ActiveProfile.ImageFileSettings.FileType,
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

        private string SaveFits(string path, ImageParameters parameters) {
            try {
                FITS f = new FITS(
                    this.ImgArr.FlatArray,
                    this.ImgArr.Statistics.Width,
                    this.ImgArr.Statistics.Height,
                    parameters.ImageType,
                    parameters.ExposureTime
                );

                f.AddHeaderCard("FOCALLEN", ProfileManager.Instance.ActiveProfile.TelescopeSettings.FocalLength, "");
                f.AddHeaderCard("XPIXSZ", ProfileManager.Instance.ActiveProfile.CameraSettings.PixelSize, "");
                f.AddHeaderCard("YPIXSZ", ProfileManager.Instance.ActiveProfile.CameraSettings.PixelSize, "");
                f.AddHeaderCard("SITELAT", Astrometry.HoursToHMS(ProfileManager.Instance.ActiveProfile.AstrometrySettings.Latitude), "");
                f.AddHeaderCard("SITELONG", Astrometry.HoursToHMS(ProfileManager.Instance.ActiveProfile.AstrometrySettings.Longitude), "");

                if (!string.IsNullOrEmpty(parameters.FilterName)) {
                    f.AddHeaderCard("FILTER", parameters.FilterName, "");
                }

                if (Cam != null) {
                    if (Cam.BinX > 0) {
                        f.AddHeaderCard("XBINNING", Cam.BinX, "");
                    }
                    if (Cam.BinY > 0) {
                        f.AddHeaderCard("YBINNING", Cam.BinY, "");
                    }
                    f.AddHeaderCard("EGAIN", Cam.Gain, "");
                }

                if (Telescope != null) {
                    f.AddHeaderCard("OBJCTRA", Astrometry.HoursToFitsHMS(Telescope.RightAscension), "");
                    f.AddHeaderCard("OBJCTDEC", Astrometry.DegreesToFitsDMS(Telescope.Declination), "");
                }

                var temp = Cam.Temperature;
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

        [Obsolete]
        private string SaveFits2(string path, ImageParameters parameters) {
            try {
                Header h = new Header();
                h.AddValue("SIMPLE", "T", "C# FITS");
                h.AddValue("BITPIX", 16, "");
                h.AddValue("NAXIS", 2, "Dimensionality");
                h.AddValue("NAXIS1", this.ImgArr.Statistics.Width, "");
                h.AddValue("NAXIS2", this.ImgArr.Statistics.Height, "");
                h.AddValue("BZERO", 32768, "");
                h.AddValue("EXTEND", "T", "Extensions are permitted");

                h.AddValue("DATE-OBS", DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture), "");

                if (!string.IsNullOrEmpty(parameters.FilterName)) {
                    h.AddValue("FILTER", parameters.FilterName, "");
                }

                if (Cam != null) {
                    if (Cam.BinX > 0) {
                        h.AddValue("XBINNING", Cam.BinX, "");
                    }
                    if (Cam.BinY > 0) {
                        h.AddValue("YBINNING", Cam.BinY, "");
                    }
                    h.AddValue("EGAIN", Cam.Gain, "");
                }

                if (Telescope != null) {
                    h.AddValue("SITELAT", Telescope.SiteLatitude.ToString(CultureInfo.InvariantCulture), "");
                    h.AddValue("SITELONG", Telescope.SiteLongitude.ToString(CultureInfo.InvariantCulture), "");
                    h.AddValue("OBJCTRA", Telescope.RightAscensionString, "");
                    h.AddValue("OBJCTDEC", Telescope.DeclinationString, "");
                }

                var temp = Cam.Temperature;
                if (!double.IsNaN(temp)) {
                    h.AddValue("TEMPERAT", temp, "");
                    h.AddValue("CCD-TEMP", temp, "");
                }

                h.AddValue("IMAGETYP", parameters.ImageType, "");
                h.AddValue("EXPOSURE", parameters.ExposureTime, "");

                short[][] curl = new short[this.ImgArr.Statistics.Height][];
                int idx = 0;
                for (int i = 0; i < this.ImgArr.Statistics.Height; i++) {
                    curl[i] = new short[this.ImgArr.Statistics.Width];
                    for (int j = 0; j < this.ImgArr.Statistics.Width; j++) {
                        curl[i][j] = (short)(short.MinValue + this.ImgArr.FlatArray[idx]);
                        idx++;
                    }
                }
                ImageData d = new ImageData(curl);

                Fits fits = new Fits();
                BasicHDU hdu = FitsFactory.HDUFactory(h, d);
                fits.AddHDU(hdu);

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var uniquePath = Utility.Utility.GetUniqueFilePath(path + ".fits");

                using (FileStream fs = new FileStream(uniquePath, FileMode.Create)) {
                    fits.Write(fs);
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
                header.AddImageProperty(XISFImageProperty.Observation.Location.Latitude, ProfileManager.Instance.ActiveProfile.AstrometrySettings.Latitude.ToString(CultureInfo.InvariantCulture));
                header.AddImageProperty(XISFImageProperty.Observation.Location.Longitude, ProfileManager.Instance.ActiveProfile.AstrometrySettings.Longitude.ToString(CultureInfo.InvariantCulture));
                header.AddImageProperty(XISFImageProperty.Instrument.Telescope.FocalLength, ProfileManager.Instance.ActiveProfile.TelescopeSettings.FocalLength.ToString(CultureInfo.InvariantCulture));
                header.AddImageProperty(XISFImageProperty.Instrument.Sensor.XPixelSize, ProfileManager.Instance.ActiveProfile.CameraSettings.PixelSize.ToString(CultureInfo.InvariantCulture));
                header.AddImageProperty(XISFImageProperty.Instrument.Sensor.YPixelSize, ProfileManager.Instance.ActiveProfile.CameraSettings.PixelSize.ToString(CultureInfo.InvariantCulture));

                if (Telescope != null) {
                    header.AddImageProperty(XISFImageProperty.Instrument.Telescope.Name, Telescope.Name);

                    /* Location */
                    header.AddImageProperty(XISFImageProperty.Observation.Location.Elevation, Telescope.SiteElevation.ToString(CultureInfo.InvariantCulture));
                    /* convert to degrees */
                    var RA = Telescope.RightAscension * 360 / 24;
                    header.AddImageProperty(XISFImageProperty.Observation.Center.RA, RA.ToString(CultureInfo.InvariantCulture), string.Empty, false);
                    header.AddImageFITSKeyword(XISFImageProperty.Observation.Center.RA[2], Astrometry.HoursToFitsHMS(Telescope.RightAscension));

                    header.AddImageProperty(XISFImageProperty.Observation.Center.Dec, Telescope.Declination.ToString(CultureInfo.InvariantCulture), string.Empty, false);
                    header.AddImageFITSKeyword(XISFImageProperty.Observation.Center.Dec[2], Astrometry.DegreesToFitsDMS(Telescope.Declination));
                }

                if (Cam != null) {
                    header.AddImageProperty(XISFImageProperty.Instrument.Camera.Name, Cam.Name);

                    if (Cam.Gain > 0) {
                        /* Add offset as a comment. There is no dedicated keyword for this */
                        string offset = string.Empty;
                        if (Cam.Offset > 0) {
                            offset = Cam.Offset.ToString(CultureInfo.InvariantCulture);
                        }
                        header.AddImageProperty(XISFImageProperty.Instrument.Camera.Gain, Cam.Gain.ToString(CultureInfo.InvariantCulture), offset);
                    }

                    if (Cam.BinX > 0) {
                        header.AddImageProperty(XISFImageProperty.Instrument.Camera.XBinning, Cam.BinX.ToString(CultureInfo.InvariantCulture));
                    }
                    if (Cam.BinY > 0) {
                        header.AddImageProperty(XISFImageProperty.Instrument.Camera.YBinning, Cam.BinY.ToString(CultureInfo.InvariantCulture));
                    }

                    var temp = Cam.Temperature;
                    if (!double.IsNaN(temp)) {
                        header.AddImageProperty(XISFImageProperty.Instrument.Sensor.Temperature, temp.ToString(CultureInfo.InvariantCulture));
                    }
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
    }

    public class ImageParameters {
        public string FilterName { get; internal set; }
        public int ExposureNumber { get; internal set; }
        public string ImageType { get; internal set; }
        public string Binning { get; internal set; }
        public double ExposureTime { get; internal set; }
        public string TargetName { get; internal set; }
    }
}