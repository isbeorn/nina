using Microsoft.Win32;
using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFocuser;
using NINA.Model.MyTelescope;
using NINA.Utility;
using NINA.Utility.Mediator;
using NINA.Utility.Notification;
using NINA.ViewModel;
using nom.tam.fits;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;

namespace NINA.ViewModel {
    public class ImageControlVM : DockableVM {

        public ImageControlVM() {
            Title = "LblImage";

            ContentId = nameof(ImageControlVM);
            CanClose = false;
            AutoStretch = false;
            DetectStars = false;
            ShowCrossHair = false;

            _progress = new Progress<ApplicationStatus>(p => Status = p);
            
            PrepareImageCommand = new AsyncCommand<bool>(() => PrepareImageHelper());
            PlateSolveImageCommand = new AsyncCommand<bool>(() => PlateSolveImage());
            CancelPlateSolveImageCommand = new RelayCommand(CancelPlateSolveImage);

            RegisterMediatorMessages();
        }

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
                RaisePropertyChanged();
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

                        if (Settings.AnnotateImage) {
                            source = analysis.GetAnnotatedImage();
                        }

                        iarr.Statistics.HFR = analysis.AverageHFR;
                        iarr.Statistics.DetectedStars = analysis.DetectedStars;
                    }

                    if (iarr.IsBayered) {
                        _progress.Report(new ApplicationStatus() { Status = "Debayer image" });
                        source = ImageAnalysis.Debayer(source, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale);
                    }

                    await _dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                        Image = null;
                        ImgArr = null;
                        GC.Collect();
                        ImgArr = iarr;
                        Image = source;
                        ImgStatisticsVM.Add(ImgArr.Statistics);
                        ImgHistoryVM.Add(iarr.Statistics);
                    }));

                    if(bSave) {
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

            var filter = ImageAnalysis.GetColorRemappingFilter(mean, Settings.AutoStretchFactor);
            filter.ApplyInPlace(img);

            var source = ImageAnalysis.ConvertBitmap(img, pf);
            source.Freeze();
            return source;
        }

        public async Task<bool> SaveToDisk(ImageParameters parameters, CancellationToken token) {

            var filter = parameters.FilterName;
            var framenr = parameters.ExposureNumber;
            var success = false;
            try {
                success = await SaveToDiskAsync(parameters, token);
            } catch(OperationCanceledException ex) {
                throw new OperationCanceledException(ex.Message);
            } catch(Exception ex) {                                
                Notification.ShowError(ex.Message);
                Logger.Error(ex.Message, ex.StackTrace);
            }
            finally {
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
                p.Add(new OptionsVM.ImagePattern("$$DATETIME$$", "Date with format YYYY-MM-DD_HH-mm-ss", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")));
                p.Add(new OptionsVM.ImagePattern("$$FRAMENR$$", "# of the Frame with format ####", string.Format("{0:0000}", parameters.ExposureNumber)));
                p.Add(new OptionsVM.ImagePattern("$$IMAGETYPE$$", "Light, Flat, Dark, Bias", parameters.ImageType));

                if (parameters.Binning == string.Empty) {
                    p.Add(new OptionsVM.ImagePattern("$$BINNING$$", "Binning of the camera", "1x1"));
                } else {
                    p.Add(new OptionsVM.ImagePattern("$$BINNING$$", "Binning of the camera", parameters.Binning));
                }

                p.Add(new OptionsVM.ImagePattern("$$SENSORTEMP$$", "Temperature of the Camera", string.Format("{0:00}", Cam?.CCDTemperature)));

                p.Add(new OptionsVM.ImagePattern("$$TARGETNAME$$", "Target Name if available", parameters.TargetName));

                p.Add(new OptionsVM.ImagePattern("$$GAIN$$", "Camera Gain", Cam?.Gain.ToString() ?? string.Empty));

                string filename = Utility.Utility.GetImageFileString(p);
                string completefilename = Settings.ImageFilePath + filename;

                Stopwatch sw = Stopwatch.StartNew();
                if (Settings.FileType == FileTypeEnum.FITS) {
                    if (parameters.ImageType == "SNAP") parameters.ImageType = "LIGHT";
                    completefilename = SaveFits(completefilename, parameters);
                } else if (Settings.FileType == FileTypeEnum.TIFF) {
                    completefilename = SaveTiff(completefilename);
                } else if (Settings.FileType == FileTypeEnum.XISF) {
                    if (parameters.ImageType == "SNAP") parameters.ImageType = "LIGHT";
                    completefilename = SaveXisf(completefilename, parameters);
                } else {
                    completefilename = SaveTiff(completefilename);
                }

                await Mediator.Instance.RequestAsync(
                    new AddThumbnailMessage() {
                        PathToImage = new Uri(completefilename),
                        Image = Image,
                        FileType = Settings.FileType,
                        Mean = ImgArr.Statistics.Mean,
                        HFR = ImgArr.Statistics.HFR,
                        Duration = parameters.ExposureTime,
                        IsBayered = ImgArr.IsBayered,
                        Filter = parameters.FilterName
                    }
                );

                sw.Stop();
                Debug.Print("Time to save: " + sw.Elapsed);
                sw = null;


            });

            token.ThrowIfCancellationRequested();
            return true;
        }

        private string SaveFits(string path, ImageParameters parameters) {
            try {
                Header h = new Header();
                h.AddValue("SIMPLE", "T", "C# FITS");
                h.AddValue("BITPIX", 16, "");
                h.AddValue("NAXIS", 2, "Dimensionality");
                h.AddValue("NAXIS1", this.ImgArr.Statistics.Width, "");
                h.AddValue("NAXIS2", this.ImgArr.Statistics.Height, "");
                h.AddValue("BZERO", 32768, "");
                h.AddValue("EXTEND", "T", "Extensions are permitted");
                                
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
                }

                var temp = Cam.CCDTemperature;
                if (!double.IsNaN(temp)) {
                    h.AddValue("TEMPERAT", temp, "");
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
                var uniquePath = GetUniqueFilePath(path + ".fits");

                using (FileStream fs = new FileStream(uniquePath, FileMode.Create)) {
                    fits.Write(fs);
                }
                return uniquePath;
            } catch (Exception ex) {
                Notification.ShowError("Image file error: " + ex.Message);
                Logger.Error(ex.Message, ex.StackTrace);
                return string.Empty;
            }
        }

        private string GetUniqueFilePath(string fullPath) {
            int count = 1;

            string fileNameOnly = Path.GetFileNameWithoutExtension(fullPath);
            string extension = Path.GetExtension(fullPath);
            string path = Path.GetDirectoryName(fullPath);
            string newFullPath = fullPath;

            while (File.Exists(newFullPath)) {
                string tempFileName = string.Format("{0}({1})", fileNameOnly, count++);
                newFullPath = Path.Combine(path, tempFileName + extension);
            }
            return newFullPath;
        }

        private string SaveTiff(String path) {

            try {
                BitmapSource bmpSource = ImageAnalysis.CreateSourceFromArray(ImgArr, System.Windows.Media.PixelFormats.Gray16);

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var uniquePath = GetUniqueFilePath(path + ".tif");

                using (FileStream fs = new FileStream(uniquePath, FileMode.Create)) {
                    TiffBitmapEncoder encoder = new TiffBitmapEncoder();
                    encoder.Compression = TiffCompressOption.None;
                    encoder.Frames.Add(BitmapFrame.Create(bmpSource));
                    encoder.Save(fs);
                }
                return uniquePath;
            } catch (Exception ex) {
                Notification.ShowError("Image file error: " + ex.Message);
                Logger.Error(ex.Message, ex.StackTrace);
                return string.Empty;
            }
        }

        private string SaveXisf(String path, ImageParameters parameters) {
            try {


                var header = new XISFHeader();

                header.AddEmbeddedImage(ImgArr, parameters.ImageType);

                header.AddImageProperty(XISFImageProperty.Observation.Time.Start, DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture));

                if (Telescope != null) {
                    header.AddImageProperty(XISFImageProperty.Instrument.Telescope.Name, Telescope.Name);

                    /* Location */
                    header.AddImageProperty(XISFImageProperty.Observation.Location.Latitude, Telescope.SiteLatitude.ToString(CultureInfo.InvariantCulture));
                    header.AddImageProperty(XISFImageProperty.Observation.Location.Longitude, Telescope.SiteLongitude.ToString(CultureInfo.InvariantCulture));
                    header.AddImageProperty(XISFImageProperty.Observation.Location.Elevation, Telescope.SiteElevation.ToString(CultureInfo.InvariantCulture));
                    /* convert to degrees */
                    var RA = Telescope.RightAscension * 360 / 24;
                    header.AddImageProperty(XISFImageProperty.Observation.Center.RA, RA.ToString(CultureInfo.InvariantCulture), string.Empty, false);
                    header.AddImageFITSKeyword(XISFImageProperty.Observation.Center.RA[2], Telescope.RightAscensionString);

                    header.AddImageProperty(XISFImageProperty.Observation.Center.Dec, Telescope.Declination.ToString(CultureInfo.InvariantCulture), string.Empty, false);
                    header.AddImageFITSKeyword(XISFImageProperty.Observation.Center.Dec[2], Telescope.DeclinationString);
                }

                if (Cam != null) {
                    header.AddImageProperty(XISFImageProperty.Instrument.Camera.Name, Cam.Name);

                    if (Cam.Gain > 0) {
                        /* Add offset as a comment. There is no dedicated keyword for this */
                        string offset = string.Empty;
                        if (Cam.Offset > 0) {
                            offset = Cam.Offset.ToString();
                        }
                        header.AddImageProperty(XISFImageProperty.Instrument.Camera.Gain, Cam.Gain.ToString(), offset);
                    }

                    if (Cam.BinX > 0) {
                        header.AddImageProperty(XISFImageProperty.Instrument.Camera.XBinning, Cam.BinX.ToString());
                    }
                    if (Cam.BinY > 0) {
                        header.AddImageProperty(XISFImageProperty.Instrument.Camera.YBinning, Cam.BinY.ToString());
                    }

                    var temp = Cam.CCDTemperature;
                    if (!double.IsNaN(temp)) {
                        header.AddImageProperty(XISFImageProperty.Instrument.Sensor.Temperature, temp.ToString(CultureInfo.InvariantCulture));
                    }

                    if (Cam.PixelSizeX > 0) {
                        header.AddImageProperty(XISFImageProperty.Instrument.Sensor.XPixelSize, Cam.PixelSizeX.ToString(CultureInfo.InvariantCulture));
                    }

                    if (Cam.PixelSizeY > 0) {
                        header.AddImageProperty(XISFImageProperty.Instrument.Sensor.YPixelSize, Cam.PixelSizeY.ToString(CultureInfo.InvariantCulture));
                    }
                }

                
                if (!string.IsNullOrEmpty(parameters.FilterName)) {
                    header.AddImageProperty(XISFImageProperty.Instrument.Filter.Name, parameters.FilterName);
                }

                header.AddImageProperty(XISFImageProperty.Instrument.ExposureTime, parameters.ExposureTime.ToString(System.Globalization.CultureInfo.InvariantCulture));

                XISF img = new XISF(header);

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var uniquePath = GetUniqueFilePath(path + ".xisf");

                using (FileStream fs = new FileStream(uniquePath, FileMode.Create)) {
                    img.Save(fs);
                }
                return uniquePath;

            } catch (Exception ex) {
                Notification.ShowError("Image file error: " + ex.Message);
                Logger.Error(ex.Message, ex.StackTrace);
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
