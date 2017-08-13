using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.ViewModel;
using nom.tam.fits;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
            Title = "Image Area";
            ContentId = nameof(ImageControlVM);
            CanClose = false;
            AutoStretch = false;
            DetectStars = false;
            ShowCrossHair = false;
            AutoStretchFactor = 0.25;


            RegisterMediatorMessages();
        }

        private void RegisterMediatorMessages() {
            Mediator.Instance.Register((object o) => {
                AutoStretch = (bool)o;
            }, MediatorMessages.ChangeAutoStretch);
            Mediator.Instance.Register((object o) => {
                DetectStars = (bool)o;
            }, MediatorMessages.ChangeDetectStars);
        }

        private Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        private ImageArray _imgArr;
        public ImageArray ImgArr {
            get {
                return _imgArr;
            }
            set {
                _imgArr = value;
                RaisePropertyChanged();
                ImgStatisticsVM.Add(ImgArr.Statistics);
            }
        }
        
        private ImageHistoryVM _imgHistoryVM;
        public ImageHistoryVM ImgHistoryVM {
            get {
                if(_imgHistoryVM == null) {
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
                if(_imgStatisticsVM == null) {
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
                Mediator.Instance.Notify(MediatorMessages.ImageChanged, _image);
            }
        }

        private bool _autoStretch;
        public bool AutoStretch {
            get {
                return _autoStretch;
            }
            set {
                _autoStretch = value;
                RaisePropertyChanged();
                Mediator.Instance.Notify(MediatorMessages.AutoStrechChanged, _autoStretch);
            }
        }

        private double _autoStretchFactor;
        public double AutoStretchFactor {
            get {
                return _autoStretchFactor;
            }
            set {
                _autoStretchFactor = value;
                RaisePropertyChanged();
            }
        }

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
                RaisePropertyChanged();
                Mediator.Instance.Notify(MediatorMessages.DetectStarsChanged, _detectStars);
            }
        }

        private string _status;
        public string Status {
            get {
                return _status;
            } 
            set {
                _status = value;
                RaisePropertyChanged();
            }
        }

        public async Task PrepareImage(IProgress<string> progress, CancellationTokenSource canceltoken) {
            Image = null;
            GC.Collect();
            BitmapSource source = ImageAnalysis.CreateSourceFromArray(ImgArr, System.Windows.Media.PixelFormats.Gray16);
            
            if (DetectStars) {
                source = await ImageAnalysis.DetectStarsAsync(source, ImgArr, progress, canceltoken);
            } else if (AutoStretch) {
                source = await StretchAsync(source);
            }
            
            await _dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                ImgHistoryVM.Add(ImgStatisticsVM.Statistics);                
                Image = source;
            }));

            
        }

        private async Task<BitmapSource> StretchAsync(BitmapSource source) {
            return await Task<BitmapSource>.Run(() => Stretch(source));
        }

        private BitmapSource Stretch(BitmapSource source) {
            var img = ImageAnalysis.BitmapFromSource(source);

            var filter = ImageAnalysis.GetColorRemappingFilter(ImgArr.Statistics.Mean, AutoStretchFactor);
            filter.ApplyInPlace(img);

            source = null;

            source = ImageAnalysis.ConvertBitmap(img, System.Windows.Media.PixelFormats.Gray16);
            source.Freeze();
            return source;
        }


        public async Task<bool> SaveToDisk(double exposuretime, string filter, string imageType, string binning, double ccdtemp, ushort framenr, CancellationTokenSource tokenSource, IProgress<string> progress) {
            progress.Report("Saving...");
            await Task.Run(() => {

                List<OptionsVM.ImagePattern> p = new List<OptionsVM.ImagePattern>();

                p.Add(new OptionsVM.ImagePattern("$$FILTER$$", "Filtername", filter));
               
                p.Add(new OptionsVM.ImagePattern("$$EXPOSURETIME$$", "Exposure Time in seconds", string.Format("{0:0.00}", exposuretime)));
                p.Add(new OptionsVM.ImagePattern("$$DATE$$", "Date with format YYYY-MM-DD", DateTime.Now.ToString("yyyy-MM-dd")));
                p.Add(new OptionsVM.ImagePattern("$$DATETIME$$", "Date with format YYYY-MM-DD_HH-mm-ss", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")));
                p.Add(new OptionsVM.ImagePattern("$$FRAMENR$$", "# of the Frame with format ####", string.Format("{0:0000}", framenr)));
                p.Add(new OptionsVM.ImagePattern("$$IMAGETYPE$$", "Light, Flat, Dark, Bias", imageType));

                if (binning == string.Empty) {
                    p.Add(new OptionsVM.ImagePattern("$$BINNING$$", "Binning of the camera", "1x1"));
                }
                else {
                    p.Add(new OptionsVM.ImagePattern("$$BINNING$$", "Binning of the camera", binning));
                }

                p.Add(new OptionsVM.ImagePattern("$$SENSORTEMP$$", "Temperature of the Camera", string.Format("{0:00}", ccdtemp)));

                string filename = Utility.Utility.GetImageFileString(p);
                string completefilename = Settings.ImageFilePath + filename;

                Stopwatch sw = Stopwatch.StartNew();
                if (Settings.FileType == FileTypeEnum.FITS) {                    
                    if (imageType == "SNAP") imageType = "LIGHT";
                    SaveFits(completefilename, imageType, exposuretime, filter, binning, ccdtemp);
                }
                else if (Settings.FileType == FileTypeEnum.TIFF) {
                    SaveTiff(completefilename);
                }
                else if (Settings.FileType == FileTypeEnum.XISF) {
                    if (imageType == "SNAP") imageType = "LIGHT";
                    SaveXisf(completefilename, imageType, exposuretime, filter, binning, ccdtemp);
                }
                else {
                    SaveTiff(completefilename);
                }
                sw.Stop();
                Debug.Print("Time to save: " + sw.Elapsed);
                sw = null;


            });

            tokenSource.Token.ThrowIfCancellationRequested();
            return true;
        }

        private void SaveFits(string path, string imagetype, double duration, string filter, string binning, double temp) {
            try {                
                Header h = new Header();
                h.AddValue("SIMPLE", "T", "C# FITS");
                h.AddValue("BITPIX", 16, "");
                h.AddValue("NAXIS", 2, "Dimensionality");
                h.AddValue("NAXIS1", this.ImgArr.Statistics.Width, "");
                h.AddValue("NAXIS2", this.ImgArr.Statistics.Height, "");
                h.AddValue("BZERO", 32768, "");
                h.AddValue("EXTEND", "T", "Extensions are permitted");

                if (!string.IsNullOrEmpty(filter)) {
                    h.AddValue("FILTER", filter, "");
                }

                if (binning != string.Empty && binning.Contains('x')) {
                    h.AddValue("CCDXBIN", binning.Split('x')[0], "");
                    h.AddValue("CCDYBIN", binning.Split('x')[1], "");
                    h.AddValue("XBINNING", binning.Split('x')[0], "");
                    h.AddValue("YBINNING", binning.Split('x')[1], "");
                }
                h.AddValue("TEMPERAT", temp, "");

                h.AddValue("IMAGETYP", imagetype, "");

                h.AddValue("EXPOSURE", duration, "");
                /*
                 
                 h.AddValue("OBJECT", 32768, "");
                 */

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
                using (FileStream fs = new FileStream(path + ".fits", FileMode.Create)) {
                    fits.Write(fs);
                }

            }
            catch (Exception ex) {
                Notification.ShowError("Image file error: " + ex.Message);
                Logger.Error(ex.Message, ex.StackTrace);

            }
        }

        private void SaveTiff(String path) {

            try {
                BitmapSource bmpSource = ImageAnalysis.CreateSourceFromArray(ImgArr, System.Windows.Media.PixelFormats.Gray16);

                Directory.CreateDirectory(Path.GetDirectoryName(path));

                using (FileStream fs = new FileStream(path + ".tif", FileMode.Create)) {
                    TiffBitmapEncoder encoder = new TiffBitmapEncoder();
                    encoder.Compression = TiffCompressOption.None;
                    encoder.Frames.Add(BitmapFrame.Create(bmpSource));
                    encoder.Save(fs);
                }
            }
            catch (Exception ex) {
                Notification.ShowError("Image file error: " + ex.Message);
                Logger.Error(ex.Message, ex.StackTrace);

            }
        }

        private void SaveXisf(String path, string imagetype, double duration, string filter, string binning, double temp) {
            try {
                
                                
                var header = new XISFHeader();

                header.AddEmbeddedImage(ImgArr, imagetype);

                if (binning != string.Empty && binning.Contains('x')) {
                    var xbin = binning.Substring(0, 1);
                    var ybin = binning.Substring(2, 1);
                    header.AddImageProperty(XISFImageProperty.Instrument.Camera.XBinning, xbin);                    
                    header.AddImageProperty(XISFImageProperty.Instrument.Camera.YBinning, ybin);                    
                }

                if (!string.IsNullOrEmpty(filter)) {
                    header.AddImageProperty(XISFImageProperty.Instrument.Filter.Name, filter);                    
                }

                if(!double.IsNaN(temp)) {
                    header.AddImageProperty(XISFImageProperty.Instrument.Sensor.Temperature, temp.ToString());                    
                }                

                header.AddImageProperty(XISFImageProperty.Instrument.ExposureTime, duration.ToString(System.Globalization.CultureInfo.InvariantCulture));

                XISF img = new XISF(header);

                img.Save(path);                

                } catch (Exception ex) {
                Notification.ShowError("Image file error: " + ex.Message);
                Logger.Error(ex.Message, ex.StackTrace);

            }
        }

    }
}
