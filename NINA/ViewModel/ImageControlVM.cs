#region "copyright"

/*
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

/*
 * Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>
 * Copyright 2019 Dale Ghent <daleg@elemental.org>
 */

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFocuser;
using NINA.Model.MyRotator;
using NINA.Model.MyTelescope;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Behaviors;
using NINA.Utility.Enum;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Profile;
using NINA.Utility.WindowService;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace NINA.ViewModel {

    internal class ImageControlVM : DockableVM, ICameraConsumer, ITelescopeConsumer, IFilterWheelConsumer, IFocuserConsumer, IRotatorConsumer {

        public ImageControlVM(IProfileService profileService, ICameraMediator cameraMediator, ITelescopeMediator telescopeMediator, IFilterWheelMediator filterWheelMediator, IFocuserMediator focuserMediator, IRotatorMediator rotatorMediator, IImagingMediator imagingMediator, IApplicationStatusMediator applicationStatusMediator) : base(profileService) {
            Title = "LblImage";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["PictureSVG"];

            this.cameraMediator = cameraMediator;
            this.cameraMediator.RegisterConsumer(this);

            this.telescopeMediator = telescopeMediator;
            this.telescopeMediator = telescopeMediator;
            this.telescopeMediator.RegisterConsumer(this);

            this.filterWheelMediator = filterWheelMediator;
            this.filterWheelMediator.RegisterConsumer(this);

            this.focuserMediator = focuserMediator;
            this.focuserMediator.RegisterConsumer(this);

            this.rotatorMediator = rotatorMediator;
            this.rotatorMediator.RegisterConsumer(this);

            this.imagingMediator = imagingMediator;
            this.applicationStatusMediator = applicationStatusMediator;
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
            InspectAberrationCommand = new AsyncCommand<bool>(() => InspectAberration(), (object o) => Image != null);

            BahtinovRectangle = new ObservableRectangle(-1, -1, 200, 200);
            SubSampleRectangle = new ObservableRectangle(-1, -1, 600, 600);
            BahtinovRectangle.PropertyChanged += Rectangle_PropertyChanged;
            SubSampleRectangle.PropertyChanged += SubSampleRectangle_PropertyChanged;
        }

        private async Task<bool> InspectAberration() {
            try {
                var vm = new AberrationInspectorVM(profileService);
                await vm.Initialize(Image);
                var service = WindowServiceFactory.Create();
                service.Show(vm, Locale.Loc.Instance["LblAberrationInspector"], ResizeMode.CanResize, WindowStyle.ToolWindow);
                return true;
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
                return false;
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

        public IAsyncCommand InspectAberrationCommand { get; private set; }
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
                _plateSolveToken?.Dispose();
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

        private ImageArray _imgArr;

        public ImageArray ImgArr {
            get {
                return _imgArr;
            }
            set {
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
            set {
                _image = value;
                if (_image != null) {
                    if (ShowBahtinovAnalyzer) {
                        ResizeRectangleToImageSize(_image, BahtinovRectangle);
                    }
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
            _prepImageCancellationSource?.Dispose();
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
        private IFilterWheelMediator filterWheelMediator;
        private IFocuserMediator focuserMediator;
        private IRotatorMediator rotatorMediator;
        private IImagingMediator imagingMediator;
        private IApplicationStatusMediator applicationStatusMediator;
        private FilterWheelInfo filterWheelInfo = DeviceInfo.CreateDefaultInstance<FilterWheelInfo>();
        private FocuserInfo focuserInfo = DeviceInfo.CreateDefaultInstance<FocuserInfo>();
        private RotatorInfo rotatorInfo = DeviceInfo.CreateDefaultInstance<RotatorInfo>();

        public async Task<BitmapSource> PrepareImage(
                ImageArray iarr,
                CancellationToken token,
                bool saveImage = false,
                ImageParameters parameters = null,
                bool addToStatistics = true,
                bool addToHistory = true) {
            BitmapSource source = null;

            await ss.WaitAsync(token);

            try {
                if (iarr != null) {
                    _progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblPrepareImage"] });
                    source = ImageAnalysis.CreateSourceFromArray(iarr, System.Windows.Media.PixelFormats.Gray16);

                    if (AutoStretch) {
                        _progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblStretchImage"] });
                        source = await StretchAsync(iarr, source, profileService.ActiveProfile.ImageSettings.AutoStretchFactor, profileService.ActiveProfile.ImageSettings.BlackClipping);
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

                    if (iarr.Statistics.IsBayered && profileService.ActiveProfile.ImageSettings.DebayerImage) {
                        _progress.Report(new ApplicationStatus() { Status = Locale.Loc.Instance["LblDebayeringImage"] });
                        source = ImageAnalysis.Debayer(source, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale);
                    }

                    if (parameters != null) {
                        iarr.Statistics.ExposureTime = parameters.ExposureTime;
                    }

                    ImgArr = iarr;
                    Image = source;
                    GC.Collect();
                    if (addToStatistics)
                        ImgStatisticsVM.Add(ImgArr.Statistics);
                    if (addToHistory)
                        ImgHistoryVM.Add(iarr.Statistics);

                    if (ShowBahtinovAnalyzer) {
                        AnalyzeBahtinov();
                    }

                    if (saveImage) {
                        await SaveToDisk(parameters, token);
                    }
                }
            } finally {
                _progress.Report(new ApplicationStatus() { Status = string.Empty });
                ss.Release();
            }
            return source;
        }

        public static async Task<BitmapSource> StretchAsync(ImageArray iarr, BitmapSource source, double factor, double blackClipping) {
            return await Task<BitmapSource>.Run(() => Stretch(iarr.Statistics, source, System.Windows.Media.PixelFormats.Gray16, factor, blackClipping));
        }

        public static async Task<BitmapSource> StretchAsync(IImageStatistics statistics, BitmapSource source, double factor, double blackClipping) {
            return await Task<BitmapSource>.Run(() => Stretch(statistics, source, System.Windows.Media.PixelFormats.Gray16, factor, blackClipping));
        }

        public static BitmapSource Stretch(IImageStatistics statistics, BitmapSource source, System.Windows.Media.PixelFormat pf, double factor, double blackClipping) {
            using (var img = ImageAnalysis.BitmapFromSource(source)) {
                return Stretch(statistics, img, pf, factor, blackClipping);
            }
        }

        public static BitmapSource Stretch(IImageStatistics statistics, System.Drawing.Bitmap img, System.Windows.Media.PixelFormat pf, double factor, double blackClipping) {
            using (MyStopWatch.Measure()) {
                var filter = ImageAnalysis.GetColorRemappingFilter(statistics, factor, blackClipping);
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
                p.Set(ImagePatternKeys.ApplicationStartDate, Utility.Utility.ApplicationStartDate.ToString("yyyy-MM-dd"));
                p.Set(ImagePatternKeys.Date, parameters.ExposureStart.ToString("yyyy-MM-dd"));
                p.Set(ImagePatternKeys.Time, parameters.ExposureStart.ToString("HH-mm-ss"));
                p.Set(ImagePatternKeys.DateTime, parameters.ExposureStart.ToString("yyyy-MM-dd_HH-mm-ss"));
                p.Set(ImagePatternKeys.FrameNr, parameters.ExposureNumber);
                p.Set(ImagePatternKeys.ImageType, parameters.ImageType);

                if (focuserInfo.Connected) {
                    p.Set(ImagePatternKeys.FocuserPosition, focuserInfo.Position);
                }

                if (parameters.Binning == string.Empty) {
                    p.Set(ImagePatternKeys.Binning, "1x1");
                } else {
                    p.Set(ImagePatternKeys.Binning, parameters.Binning);
                }

                p.Set(ImagePatternKeys.SensorTemp, cameraInfo.Temperature);
                p.Set(ImagePatternKeys.TargetName, parameters.TargetName);
                p.Set(ImagePatternKeys.Gain, cameraInfo.Gain);
                p.Set(ImagePatternKeys.Offset, cameraInfo.Offset);
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
                        IsBayered = ImgArr.Statistics.IsBayered,
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

                /*
                 * First we see if the user manually gave their telescope a name. If not, we default to using the ASCOM-provided driver name.
                 */
                string telescopeName = string.Empty;
                if (!string.IsNullOrEmpty(profileService.ActiveProfile.TelescopeSettings.Name) || !string.IsNullOrWhiteSpace(profileService.ActiveProfile.TelescopeSettings.Name)) {
                    telescopeName = profileService.ActiveProfile.TelescopeSettings.Name;
                } else if (telescopeInfo.Connected) {
                    telescopeName = telescopeInfo.Name;
                }

                if (telescopeName != string.Empty) {
                    f.AddHeaderCard("TELESCOP", telescopeName, "Name of telescope");
                }

                f.AddHeaderCard("FOCALLEN", profileService.ActiveProfile.TelescopeSettings.FocalLength, "[mm] Focal length");

                if (!double.IsNaN(profileService.ActiveProfile.TelescopeSettings.FocalRatio) && profileService.ActiveProfile.TelescopeSettings.FocalRatio > 0) {
                    f.AddHeaderCard("FOCRATIO", profileService.ActiveProfile.TelescopeSettings.FocalRatio, "Focal ratio");
                }

                f.AddHeaderCard("XPIXSZ", profileService.ActiveProfile.CameraSettings.PixelSize, "[um] Pixel X axis size");
                f.AddHeaderCard("YPIXSZ", profileService.ActiveProfile.CameraSettings.PixelSize, "[um] Pixel Y axis size");
                f.AddHeaderCard("SITELAT", profileService.ActiveProfile.AstrometrySettings.Latitude, "[deg] Observation site latitude");
                f.AddHeaderCard("SITELONG", profileService.ActiveProfile.AstrometrySettings.Longitude, "[deg] Observation site longitude");

                if (!double.IsNaN(telescopeInfo.SiteElevation)) {
                    f.AddHeaderCard("SITEELEV", telescopeInfo.SiteElevation, "[m] Observation site elevation");
                }

                if (filterWheelInfo.Connected) {
                    if (!string.IsNullOrEmpty(filterWheelInfo.Name) && !string.IsNullOrWhiteSpace(filterWheelInfo.Name)) {
                        /* fits4win */
                        f.AddHeaderCard("FWHEEL", filterWheelInfo.Name, "Filter Wheel name");

                        if (!string.IsNullOrEmpty(parameters.FilterName)) {
                            f.AddHeaderCard("FILTER", parameters.FilterName, "Active filter name");
                        }
                    }
                }

                if (!string.IsNullOrEmpty(cameraInfo.Name)) {
                    f.AddHeaderCard("INSTRUME", cameraInfo.Name, "Imaging instrument name");
                }

                if (cameraInfo.BinX > 0) {
                    f.AddHeaderCard("XBINNING", cameraInfo.BinX, "X axis binning factor");
                }
                if (cameraInfo.BinY > 0) {
                    f.AddHeaderCard("YBINNING", cameraInfo.BinY, "Y axis binning factor");
                }

                f.AddHeaderCard("GAIN", cameraInfo.Gain, "Sensor gain");

                if (cameraInfo.Offset >= 0) {
                    f.AddHeaderCard("OFFSET", cameraInfo.Offset, "Sensor gain offset");
                }

                if (!double.IsNaN(cameraInfo.ElectronsPerADU)) {
                    f.AddHeaderCard("EGAIN", cameraInfo.ElectronsPerADU, "[e-/ADU] Electrons per A/D unit");
                }

                if (!string.IsNullOrEmpty(parameters.TargetName) || !string.IsNullOrWhiteSpace(parameters.TargetName)) {
                    f.AddHeaderCard("OBJECT", parameters.TargetName, "Name of the object of interest");
                }

                if (telescopeInfo.Connected) {
                    f.AddHeaderCard("RA", Astrometry.HoursToDegrees(telescopeInfo.RightAscension), "[deg] RA of telescope");
                    f.AddHeaderCard("DEC", telescopeInfo.Declination, "[deg] Declination of telescope");
                    f.AddHeaderCard("OBJCTRA", Astrometry.HoursToFitsHMS(telescopeInfo.RightAscension), "[H M S] RA of imaged object");
                    f.AddHeaderCard("OBJCTDEC", Astrometry.DegreesToFitsDMS(telescopeInfo.Declination), "[D M S] Declination of imaged object");
                }

                if (!double.IsNaN(cameraInfo.TemperatureSetPoint)) {
                    f.AddHeaderCard("SET-TEMP", cameraInfo.TemperatureSetPoint, "[C] CCD temperature setpoint");
                }

                if (!double.IsNaN(cameraInfo.Temperature)) {
                    f.AddHeaderCard("CCD-TEMP", cameraInfo.Temperature, "[C] CCD temperature");
                }

                if (focuserInfo.Connected) {
                    if (!string.IsNullOrEmpty(focuserInfo.Name) && !string.IsNullOrWhiteSpace(focuserInfo.Name)) {
                        /* fits4win, SGP */
                        f.AddHeaderCard("FOCNAME", focuserInfo.Name, "Focusing equipment name");
                    }

                    if (focuserInfo.Position != -1) {
                        /* fits4win, SGP */
                        f.AddHeaderCard("FOCPOS", focuserInfo.Position, "[step] Focuser position");

                        /* MaximDL, several observatories */
                        f.AddHeaderCard("FOCUSPOS", focuserInfo.Position, "[step] Focuser position");
                    }

                    if (!double.IsNaN(focuserInfo.StepSize)) {
                        /* MaximDL */
                        f.AddHeaderCard("FOCUSSZ", focuserInfo.StepSize, "[um] Focuser step size");
                    }

                    if (!double.IsNaN(focuserInfo.Temperature)) {
                        /* fits4win, SGP */
                        f.AddHeaderCard("FOCTEMP", focuserInfo.Temperature, "[C] Focuser temperature");

                        /* MaximDL, several observatories */
                        f.AddHeaderCard("FOCUSTEM", focuserInfo.Temperature, "[C] Focuser temperature");
                    }
                }

                if (rotatorInfo.Connected) {
                    if (!string.IsNullOrEmpty(focuserInfo.Name) && !string.IsNullOrWhiteSpace(focuserInfo.Name)) {
                        /* NINA */
                        f.AddHeaderCard("ROTNAME", rotatorInfo.Name, "Rotator equipment name");
                    }

                    if (!float.IsNaN(rotatorInfo.Position)) {
                        /* fits4win */
                        f.AddHeaderCard("ROTATOR", rotatorInfo.Position, "[deg] Rotator angle");

                        /* MaximDL, several observatories */
                        f.AddHeaderCard("ROTATANG", rotatorInfo.Position, "[deg] Rotator angle");
                    }

                    if (!float.IsNaN(rotatorInfo.StepSize)) {
                        /* NINA */
                        f.AddHeaderCard("ROTSTPSZ", rotatorInfo.StepSize, "[deg] Rotator step size");
                    }
                }

                f.AddHeaderCard("SWCREATE", string.Format("N.I.N.A. {0} ({1})", Utility.Utility.Version, DllLoader.IsX86() ? "x86" : "x64"), "Software that created this file");

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

        private string SaveTiff(string path, TiffCompressOption c) {
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

        private string SaveXisf(string path, ImageParameters parameters) {
            try {
                var header = new XISFHeader();
                DateTime now = DateTime.Now;

                header.AddImageMetaData(ImgArr, parameters.ImageType);

                header.AddImageProperty(XISFImageProperty.Observation.Time.Start, now.ToUniversalTime().ToString("yyyy-MM-ddTHH\\:mm\\:ss.fff", CultureInfo.InvariantCulture), "Time of observation (UTC)");
                header.AddImageFITSKeyword("DATE-LOC", now.ToLocalTime().ToString("yyyy-MM-ddTHH\\:mm\\:ss.fff", CultureInfo.InvariantCulture), "Time of observation (local)");
                header.AddImageProperty(XISFImageProperty.Observation.Location.Latitude, profileService.ActiveProfile.AstrometrySettings.Latitude.ToString(CultureInfo.InvariantCulture), "[deg] Observation site latitude");
                header.AddImageProperty(XISFImageProperty.Observation.Location.Longitude, profileService.ActiveProfile.AstrometrySettings.Longitude.ToString(CultureInfo.InvariantCulture), "[deg] Observation site longitude");

                if (!double.IsNaN(telescopeInfo.SiteElevation)) {
                    header.AddImageProperty(XISFImageProperty.Observation.Location.Elevation, telescopeInfo.SiteElevation.ToString(CultureInfo.InvariantCulture), "[m] Observation site elevation");
                }

                /*
                 * First we see if the user manually gave their telescope a name. If not, we default to using the ASCOM-provided driver name.
                 */
                string telescopeName = string.Empty;
                if (!string.IsNullOrEmpty(profileService.ActiveProfile.TelescopeSettings.Name) || !string.IsNullOrWhiteSpace(profileService.ActiveProfile.TelescopeSettings.Name)) {
                    telescopeName = profileService.ActiveProfile.TelescopeSettings.Name;
                } else if (telescopeInfo.Connected) {
                    telescopeName = telescopeInfo.Name;
                }

                if (telescopeName != string.Empty) {
                    header.AddImageProperty(XISFImageProperty.Instrument.Telescope.Name, telescopeName.ToString(CultureInfo.InvariantCulture), "Name of telescope");
                }

                header.AddImageProperty(XISFImageProperty.Instrument.Telescope.FocalLength, profileService.ActiveProfile.TelescopeSettings.FocalLength.ToString(CultureInfo.InvariantCulture), "[mm] Focal length");

                if (!double.IsNaN(profileService.ActiveProfile.TelescopeSettings.FocalRatio) && profileService.ActiveProfile.TelescopeSettings.FocalRatio > 0) {
                    header.AddImageFITSKeyword("FOCRATIO", profileService.ActiveProfile.TelescopeSettings.FocalRatio.ToString(CultureInfo.InvariantCulture), "Focal ratio");
                }

                header.AddImageProperty(XISFImageProperty.Instrument.Sensor.XPixelSize, profileService.ActiveProfile.CameraSettings.PixelSize.ToString(CultureInfo.InvariantCulture), "[um] Pixel X axis size");
                header.AddImageProperty(XISFImageProperty.Instrument.Sensor.YPixelSize, profileService.ActiveProfile.CameraSettings.PixelSize.ToString(CultureInfo.InvariantCulture), "[um] Pixel Y axis size");

                if (!string.IsNullOrEmpty(parameters.TargetName) || !string.IsNullOrWhiteSpace(parameters.TargetName)) {
                    header.AddImageProperty(XISFImageProperty.Observation.Object.Name, parameters.TargetName, "Name of the object of interest");
                }

                if (telescopeInfo.Connected) {
                    /* convert to degrees */
                    var RA = Astrometry.HoursToDegrees(telescopeInfo.RightAscension);
                    header.AddImageProperty(XISFImageProperty.Observation.Center.RA, RA.ToString(CultureInfo.InvariantCulture), string.Empty, false);
                    header.AddImageFITSKeyword(XISFImageProperty.Observation.Center.RA[2], Astrometry.HoursToFitsHMS(telescopeInfo.RightAscension), "[H M S] RA of imaged object");
                    header.AddImageFITSKeyword(XISFImageProperty.Observation.Center.RA[3], RA.ToString(CultureInfo.InvariantCulture), "[deg] RA of telescope");

                    header.AddImageProperty(XISFImageProperty.Observation.Center.Dec, telescopeInfo.Declination.ToString(CultureInfo.InvariantCulture), string.Empty, false);
                    header.AddImageFITSKeyword(XISFImageProperty.Observation.Center.Dec[2], Astrometry.DegreesToFitsDMS(telescopeInfo.Declination), "[D M S] Declination of imaged object");
                    header.AddImageFITSKeyword(XISFImageProperty.Observation.Center.Dec[3], telescopeInfo.Declination.ToString(CultureInfo.InvariantCulture), "[deg] Declination of telescope");
                }

                header.AddImageProperty(XISFImageProperty.Instrument.Camera.Name, cameraInfo.Name, "Imaging instrument name");

                header.AddImageFITSKeyword("GAIN", cameraInfo.Gain.ToString(CultureInfo.InvariantCulture), "Sensor gain");

                if (cameraInfo.Offset >= 0) {
                    header.AddImageFITSKeyword("OFFSET", cameraInfo.Offset.ToString(CultureInfo.InvariantCulture), "Sensor gain offset");
                }

                if (!double.IsNaN(cameraInfo.ElectronsPerADU)) {
                    header.AddImageProperty(XISFImageProperty.Instrument.Camera.Gain, cameraInfo.ElectronsPerADU.ToString(CultureInfo.InvariantCulture), "[e-/ADU] Electrons per A/D unit");
                }

                if (cameraInfo.BinX > 0) {
                    header.AddImageProperty(XISFImageProperty.Instrument.Camera.XBinning, cameraInfo.BinX.ToString(CultureInfo.InvariantCulture), "X axis binning factor");
                }
                if (cameraInfo.BinY > 0) {
                    header.AddImageProperty(XISFImageProperty.Instrument.Camera.YBinning, cameraInfo.BinY.ToString(CultureInfo.InvariantCulture), "Y axis binning factor");
                }

                if (!double.IsNaN(cameraInfo.TemperatureSetPoint)) {
                    header.AddImageFITSKeyword("SET-TEMP", cameraInfo.TemperatureSetPoint.ToString(CultureInfo.InvariantCulture), "[C] CCD temperature setpoint");
                }

                if (!double.IsNaN(cameraInfo.Temperature)) {
                    header.AddImageProperty(XISFImageProperty.Instrument.Sensor.Temperature, cameraInfo.Temperature.ToString(CultureInfo.InvariantCulture), "[C] CCD temperature");
                }

                if (focuserInfo.Connected) {
                    if (!string.IsNullOrEmpty(focuserInfo.Name) && !string.IsNullOrWhiteSpace(focuserInfo.Name)) {
                        /* fits4win, SGP */
                        header.AddImageFITSKeyword("FOCNAME", focuserInfo.Name, "Focusing equipment name");
                    }

                    /*
                     * XISF 1.0 defines Instrument:Focuser:Position as the only focuser-related image property.
                     * This image property is: "(Float32) Estimated position of the focuser in millimetres, measured with respect to a device-dependent origin."
                     * This unit is different from FOCUSPOS FITSKeyword, so we must do two separate actions: calculate distance from origin in millimetres and insert
                     * that as the XISF Instrument:Focuser:Position property, and then insert the separate FOCUSPOS FITSKeyword (measured in steps).
                     */
                    if (focuserInfo.Position != -1) {
                        if (!double.IsNaN(focuserInfo.StepSize)) {
                            /* steps * step size (microns) converted to millimetres, single-precision float */
                            float focusDistance = (focuserInfo.Position * (float)focuserInfo.StepSize) / 1000;
                            header.AddImageProperty(XISFImageProperty.Instrument.Focuser.Position, focusDistance.ToString(CultureInfo.InvariantCulture));
                        }

                        /* fits4win, SGP */
                        header.AddImageFITSKeyword("FOCPOS", focuserInfo.Position.ToString(CultureInfo.InvariantCulture), "[step] Focuser position");

                        /* MaximDL, several observatories */
                        header.AddImageFITSKeyword("FOCUSPOS", focuserInfo.Position.ToString(CultureInfo.InvariantCulture), "[step] Focuser position");
                    }

                    if (!double.IsNaN(focuserInfo.StepSize)) {
                        /* MaximDL */
                        header.AddImageFITSKeyword("FOCUSSZ", focuserInfo.StepSize.ToString(CultureInfo.InvariantCulture), "[um] Focuser step size");
                    }

                    if (!double.IsNaN(focuserInfo.Temperature)) {
                        /* fits4win, SGP */
                        header.AddImageFITSKeyword("FOCTEMP", focuserInfo.Temperature.ToString(CultureInfo.InvariantCulture), "[C] Focuser temperature");

                        /* MaximDL, several observatories */
                        header.AddImageFITSKeyword("FOCUSTEM", focuserInfo.Temperature.ToString(CultureInfo.InvariantCulture), "[C] Focuser temperature");
                    }
                }

                if (rotatorInfo.Connected) {
                    if (!string.IsNullOrEmpty(focuserInfo.Name) && !string.IsNullOrWhiteSpace(focuserInfo.Name)) {
                        /* NINA */
                        header.AddImageFITSKeyword("ROTNAME", rotatorInfo.Name, "Rotator equipment name");
                    }

                    if (!float.IsNaN(rotatorInfo.Position)) {
                        /* fits4win */
                        header.AddImageFITSKeyword("ROTATOR", rotatorInfo.Position.ToString(CultureInfo.InvariantCulture), "[deg] Rotator angle");

                        /* MaximDL, several observatories */
                        header.AddImageFITSKeyword("ROTATANG", rotatorInfo.Position.ToString(CultureInfo.InvariantCulture), "[deg] Rotator angle");
                    }

                    if (!float.IsNaN(rotatorInfo.StepSize)) {
                        /* NINA */
                        header.AddImageFITSKeyword("ROTSTPSZ", rotatorInfo.StepSize.ToString(CultureInfo.InvariantCulture), "[deg] Rotator step size");
                    }
                }

                if (filterWheelInfo.Connected) {
                    if (!string.IsNullOrEmpty(filterWheelInfo.Name) && !string.IsNullOrWhiteSpace(filterWheelInfo.Name)) {
                        /* fits4win */
                        header.AddImageFITSKeyword("FWHEEL", filterWheelInfo.Name, "Filter Wheel name");

                        if (!string.IsNullOrEmpty(parameters.FilterName)) {
                            header.AddImageProperty(XISFImageProperty.Instrument.Filter.Name, parameters.FilterName, "Active filter name");
                        }
                    }
                }

                header.AddImageProperty(XISFImageProperty.Instrument.ExposureTime, parameters.ExposureTime.ToString(System.Globalization.CultureInfo.InvariantCulture), "[s] Exposure duration");
                header.AddImageFITSKeyword("SWCREATE", string.Format("N.I.N.A. {0} ({1})", Utility.Utility.Version, DllLoader.IsX86() ? "x86" : "x64"), "Software that created this file");

                XISF img = new XISF(header);

                img.AddAttachedImage(ImgArr, parameters.ImageType);

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

        public void UpdateDeviceInfo(FilterWheelInfo deviceInfo) {
            this.filterWheelInfo = deviceInfo;
        }

        public void UpdateDeviceInfo(FocuserInfo deviceInfo) {
            this.focuserInfo = deviceInfo;
        }

        public void UpdateDeviceInfo(RotatorInfo deviceInfo) {
            this.rotatorInfo = deviceInfo;
        }
    }

    public class ImageParameters {
        public DateTime ExposureStart { get; internal set; }
        public string FilterName { get; internal set; }
        public int ExposureNumber { get; internal set; }
        public string ImageType { get; internal set; }
        public string Binning { get; internal set; }
        public double ExposureTime { get; internal set; }
        public string TargetName { get; internal set; }
        public RMS RecordedRMS { get; internal set; }
    }
}