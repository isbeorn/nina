using NINA.Model;
using NINA.Utility;
using nom.tam.fits;
using nom.tam.util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static NINA.Model.SequenceModel;

namespace NINA.ViewModel {
    class ImagingVM : BaseVM{

        public ImagingVM() {
            Name = "Imaging";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ImagingSVG"];

            SnapExposureDuration = 1;
            SnapCommand = new AsyncCommand<bool>(() => captureImage(new Progress<string>(p => ExpStatus = p)));
            CancelSnapCommand = new RelayCommand(cancelCaptureImage);
            StartSequenceCommand = new AsyncCommand<bool>(() => startSequence(new Progress<string>(p => ExpStatus = p)));
            CancelSequenceCommand = new RelayCommand(cancelSequence);
    }

        public PHD2Client PHD2Client {
            get {
                return Utility.Utility.PHDClient;
            }
        }

        private Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        private SequenceVM _seqVM;
        public SequenceVM SeqVM {
            get {
                if(_seqVM == null) {
                    _seqVM = new SequenceVM();
                }
                return _seqVM;
            }
            set {
                _seqVM = value;
                RaisePropertyChanged();
            }
        }

        private CameraModel _cam;
        public CameraModel Cam {
            get {
                return _cam;
            }
            set {
                _cam = value;
                RaisePropertyChanged();
            }
        }

        private FilterWheelModel _fW;
        public FilterWheelModel FW {
            get {
                return _fW;
            }
            set {
                _fW = value;
                RaisePropertyChanged();
            }
        }



        private double _snapExposureDuration;
        public double SnapExposureDuration {
            get {
                return _snapExposureDuration;
            }

            set {
                _snapExposureDuration = value;
                RaisePropertyChanged();
            }
        }

        private int _exposureSeconds; 
        public int ExposureSeconds {
            get {
                return _exposureSeconds;
            }
            set {
                _exposureSeconds = value;
                RaisePropertyChanged();
            }
        }

        private String _expStatus;
        public String ExpStatus {
            get {
                return _expStatus;
            }

            set {
                _expStatus = value;                
                RaisePropertyChanged();
            }
        }

        private bool _isExposing; 
        public bool IsExposing {
            get {
                return _isExposing;
            } set {
                _isExposing = value;
                RaisePropertyChanged();
            }
        }
        
        private IAsyncCommand _snapCommand;
        public IAsyncCommand SnapCommand {
            get {
                return _snapCommand;
            }
            set {
                _snapCommand = value;
                RaisePropertyChanged();
            }
        }

        private IAsyncCommand _startSequenceCommand;
        public IAsyncCommand StartSequenceCommand {
            get {
                return _startSequenceCommand;
            }
            set {
                _startSequenceCommand = value;
                RaisePropertyChanged();
            }
        }

        

        private async Task changeFilter(SequenceModel seq, CancellationTokenSource tokenSource, IProgress<string> progress) {
            if (seq.FilterType != null && FW.Connected && FW.Position != seq.FilterType.Position) {
                await dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                    FW.Position = seq.FilterType.Position;
                }));


                progress.Report(ExposureStatus.FILTERCHANGE);
                await Task.Run(() => {
                    while (FW.Position == -1) {
                        //Wait for filter change;                        
                        tokenSource.Token.ThrowIfCancellationRequested();
                    }
                });
                tokenSource.Token.ThrowIfCancellationRequested();                
            }
        }

        private void setBinning(SequenceModel seq) {
            if (seq.Binning == null) {
                Cam.setBinning(1, 1);
            }
            else {
                Cam.setBinning(seq.Binning.X, seq.Binning.Y);
            }
        }

        private async Task capture(SequenceModel seq, CancellationTokenSource tokenSource, IProgress<string> progress) {            
            double duration = seq.ExposureTime;
            progress.Report(string.Format(ExposureStatus.EXPOSING, 0, duration));
            bool isLight = false;
            if (Cam.HasShutter) {
                isLight = true;
            }
            Cam.startExposure(duration, isLight);
            ExposureSeconds = 1;
            progress.Report(string.Format(ExposureStatus.EXPOSING, 1, duration));
            /* Wait for Capture */
            if (duration >= 1) {
                await Task.Run(async () => {
                    do {
                        await Task.Delay(1000);
                        tokenSource.Token.ThrowIfCancellationRequested();
                        ExposureSeconds += 1;
                        progress.Report(string.Format(ExposureStatus.EXPOSING, ExposureSeconds, duration));
                    } while ((ExposureSeconds < duration) && Cam.Connected);
                });
            }
            tokenSource.Token.ThrowIfCancellationRequested();
        }

        private async Task<Array> download(CancellationTokenSource tokenSource, IProgress<string> progress) {
            progress.Report(ExposureStatus.DOWNLOADING);
            return await Cam.downloadExposure(tokenSource);
        }

        private async Task<Utility.Utility.ImageArray> convert(Array arr, CancellationTokenSource tokenSource, IProgress<string> progress) {
            progress.Report(ExposureStatus.PREPARING);
            Utility.Utility.ImageArray iarr;
            if (arr.GetType() == typeof(Int32[,])) {
                iarr = await Utility.Utility.convert2DArray(arr);
            } else {
                throw new NotImplementedException();
            }
            
            tokenSource.Token.ThrowIfCancellationRequested();
            return iarr;
        }

        public async Task<BitmapSource> prepare(ushort[] arr, int x, int y) {
            return await Task.Run<BitmapSource>(() => {
                BitmapSource src = Utility.Utility.createSourceFromArray(arr, x, y, System.Windows.Media.PixelFormats.Gray16);
                src.Freeze();
                return src;
            });                
        }

        private async Task<bool> save(SequenceModel seq, Utility.Utility.ImageArray iarr, ushort framenr,  CancellationTokenSource tokenSource, IProgress<string> progress) {
            progress.Report(ExposureStatus.SAVING);
            await Task.Run(() => {

                List<OptionsVM.ImagePattern> p = new List<OptionsVM.ImagePattern>();
                string filter = string.Empty;
                if (FW.Filters != null) {
                    filter = FW.Filters.ElementAt(FW.Position).Name;
                    p.Add(new OptionsVM.ImagePattern("$$FILTER$$", "Filtername", filter));
                }
                else {
                    p.Add(new OptionsVM.ImagePattern("$$FILTER$$", "Filtername", filter));
                }
                p.Add(new OptionsVM.ImagePattern("$$EXPOSURETIME$$", "Exposure Time in seconds", string.Format("{0:0.00}", seq.ExposureTime)));
                p.Add(new OptionsVM.ImagePattern("$$DATE$$", "Date with format YYYY-MM-DD", DateTime.Now.ToString("yyyy-MM-dd")));
                p.Add(new OptionsVM.ImagePattern("$$DATETIME$$", "Date with format YYYY-MM-DD_HH-mm-ss", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")));
                p.Add(new OptionsVM.ImagePattern("$$FRAMENR$$", "# of the Frame with format ####", string.Format("{0:0000}", framenr)));
                p.Add(new OptionsVM.ImagePattern("$$IMAGETYPE$$", "Light, Flat, Dark, Bias", seq.ImageType));
                
                if (seq.Binning == null) {
                    p.Add(new OptionsVM.ImagePattern("$$BINNING$$", "Binning of the camera", "1x1"));
                }
                else {
                    p.Add(new OptionsVM.ImagePattern("$$BINNING$$", "Binning of the camera", seq.Binning.Name));
                }

                p.Add(new OptionsVM.ImagePattern("$$SENSORTEMP$$", "Temperature of the Camera", string.Format("{0:00}", Cam.CCDTemperature)));

                string filename = Utility.Utility.getImageFileString(p);
                string completefilename = Settings.ImageFilePath + filename;
                if (Settings.FileType == FileTypeEnum.FITS) {                    
                    string imagetype = seq.ImageType;
                    if (imagetype == "SNAP") imagetype = "LIGHT";
                    Utility.Utility.saveFits(iarr, completefilename, seq.ImageType, seq.ExposureTime, filter, seq.Binning, Cam.CCDTemperature);
                } else if (Settings.FileType == FileTypeEnum.TIFF) {
                    Utility.Utility.saveTiff(iarr, completefilename);
                } else {
                    Utility.Utility.saveTiff(iarr, completefilename);
                }
                
                
            });

            tokenSource.Token.ThrowIfCancellationRequested();
            return true;
        }

        private async Task<bool> dither(SequenceModel seq, CancellationTokenSource tokenSource, IProgress<string> progress) {
            if (seq.Dither && ((seq.ExposureCount % seq.DitherAmount) == 0)) {
                progress.Report(ExposureStatus.DITHERING);
                await PHD2Client.dither();

                progress.Report(ExposureStatus.SETTLING);
                await Task.Run<bool>(async () => {
                    while (PHD2Client.IsDithering) {
                        await Task.Delay(100);
                        tokenSource.Token.ThrowIfCancellationRequested();
                    }
                    return true;
                });
            }
            tokenSource.Token.ThrowIfCancellationRequested();
            return true;
        }

        public  async Task<bool> startSequence(ICollection<SequenceModel> sequence, bool bSave, CancellationTokenSource tokenSource, IProgress<string> progress) {
            return await Task.Run<bool>(async () => {
                try {
                    IsExposing = true;

                    ushort framenr = 1;
                    foreach (SequenceModel seq in sequence) {
                        seq.Active = true;

                        while (seq.ExposureCount > 0) {
                            

                            /*Change Filter*/
                            await changeFilter(seq, tokenSource, progress);

                            if (!Cam.Connected) {
                                throw new OperationCanceledException();
                            }

                            /*Set Camera Binning*/
                            setBinning(seq);

                            if (!Cam.Connected) {
                                throw new OperationCanceledException();
                            }

                            /*Capture*/
                            await capture(seq, tokenSource, progress);

                            if(!Cam.Connected) {
                                throw new OperationCanceledException();
                            }

                            /*Download Image */
                            Array arr = await download(tokenSource, progress);
                            if (arr == null) {
                                throw new OperationCanceledException();
                            }

                            await dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                                /* Free Memory for new Image */
                                SourceArray = null;
                                System.GC.Collect();                                
                            }));

                            /*Convert Array to ushort*/
                            Utility.Utility.ImageArray iarr = await convert(arr, tokenSource, progress);

                            await dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                                /* Free Memory for new Image */
                                SourceArray = null;
                                System.GC.Collect();
                                SourceArray = iarr;
                            }));

                            /*Prepare Image for UI*/
                            progress.Report(ImagingVM.ExposureStatus.PREPARING);
                            BitmapSource tmp;
                            if (AutoStretch) {
                                tmp = await stretch(iarr);
                            }
                            else {
                                tmp = await prepare(iarr.FlatArray, iarr.X, iarr.Y);
                            }
                            await dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                                /* Free Memory for new Image */
                                Image = null;
                                System.GC.Collect();
                                Image = tmp;
                            }));


                            /*Save to disk*/
                            if (bSave) {
                                await save(seq, iarr, framenr, tokenSource, progress);
                            }

                            /*Dither*/
                            await dither(seq, tokenSource, progress);



                            seq.ExposureCount -= 1;
                            framenr++;
                        }
                        seq.Active = false;
                    }
                }
                catch (System.OperationCanceledException ex) {
                    Logger.trace(ex.Message);
                }
                finally {
                    progress.Report(ExposureStatus.IDLE);
                    Cam.stopExposure();
                    IsExposing = false;
                }
                return true;
            });
        }

        private async Task<bool> startSequence(IProgress<string> progress, CancellationToken token = new CancellationToken()) {
            _cancelSequenceToken = new CancellationTokenSource();
            return await startSequence(SeqVM.Sequence, true, _cancelSequenceToken, progress);           
        }


        private bool _autoStretch;
        public bool AutoStretch {
            get {
                return _autoStretch;
            }
            set {
                _autoStretch = value;
                RaisePropertyChanged();
            }

        }

        public async Task<BitmapSource> stretch(Utility.Utility.ImageArray sourceArray) {
            
            ushort[] arr = await Utility.Utility.stretchArray(sourceArray);
            BitmapSource bs = await prepare(arr, sourceArray.X, sourceArray.Y);
            bs.Freeze();
            return bs;
        }

        private Utility.Utility.ImageArray _sourceArray;
        public Utility.Utility.ImageArray SourceArray {
            get {
                return _sourceArray;
            }
            private set {
                _sourceArray = value;
                RaisePropertyChanged();
            }
        }
        BitmapSource _image;
        public BitmapSource Image {
            get {
                return _image;
            }
            set {
                _image = value;
                RaisePropertyChanged();
            }
        }

        public RelayCommand CancelSnapCommand {
            get {
                return _cancelSnapCommand;
            }

            set {
                _cancelSnapCommand = value;
                RaisePropertyChanged();
            }
        }

        public RelayCommand CancelSequenceCommand {
            get {
                return _cancelSequenceCommand;
            }

            set {
                _cancelSequenceCommand = value;
                RaisePropertyChanged();
            }
        }

        private void cancelCaptureImage(object o) {
            if (_captureImageToken != null) {
                _captureImageToken.Cancel();
            }
        }

        private void cancelSequence(object o) {
            if (_cancelSequenceToken != null) {
                _cancelSequenceToken.Cancel();
            }
        }

        CancellationTokenSource _cancelSequenceToken;
        private RelayCommand _cancelSequenceCommand;

        CancellationTokenSource _captureImageToken;
        private RelayCommand _cancelSnapCommand;

        private FilterWheelModel.FilterInfo _snapFilter;
        public FilterWheelModel.FilterInfo SnapFilter {
            get {
                return _snapFilter;
            }
            set {
                _snapFilter = value;
                RaisePropertyChanged();
            }
        }

        private CameraModel.BinningMode _snapBin;
        public CameraModel.BinningMode SnapBin {
            get {
                if(_snapBin == null) {
                    _snapBin = new CameraModel.BinningMode(1, 1);
                }
                return _snapBin;
            }
            set {
                _snapBin = value;
                RaisePropertyChanged();
            }
        }

        public async Task<bool> captureImage(IProgress<string> progress) {
            _captureImageToken = new CancellationTokenSource();
            if (IsExposing) {
                Notification.ShowWarning("Camera is busy");
                return true;
            } else { 
            
                List<SequenceModel> seq = new List<SequenceModel>();
                seq.Add(new SequenceModel(SnapExposureDuration, ImageTypes.SNAP, SnapFilter, SnapBin, 1));
                return await startSequence(seq, true, _captureImageToken, progress);
            }
        }

        public async Task<bool> captureImage(double duration, bool bsave, IProgress<string> progress, CancellationTokenSource token) {
            if (IsExposing) {
                Notification.ShowWarning("Camera is busy");
                return true;
            }
            else {
                List<SequenceModel> seq = new List<SequenceModel>();
                seq.Add(new SequenceModel(duration, ImageTypes.SNAP, null, null, 1));
                return await startSequence(seq, bsave, token, progress);
            }
        }

        public static class ExposureStatus {
            public const string EXPOSING = "Exposing {0}/{1}...";
            public const string DOWNLOADING = "Downloading...";
            public const string FILTERCHANGE = "Switching Filter...";
            public const string PREPARING = "Preparing...";
            public const string SAVING = "Saving...";
            public const string IDLE = "Idle";
            public const string DITHERING = "Dithering...";
            public const string SETTLING = "Settling...";
        }
    }
}
