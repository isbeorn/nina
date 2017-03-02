using NINA.Utility;
using NINA.ViewModel;
using nom.tam.fits;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.ViewModel {
    public class ImageControlVM : BaseVM {
        private ImageArray _imgArr;
        public ImageArray ImgArr {
            get { return _imgArr; }
            private set { _imgArr = value; RaisePropertyChanged(); }
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
            }
        }

        private double _zoomFactor;
        public double ZoomFactor {
            get {
                return _zoomFactor;
            }
            set {
                _zoomFactor = value;
                RaisePropertyChanged();
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


        public ImageControlVM() {
            AutoStretch = false;
            DetectStars = false;
            ZoomFactor = 1;
        }

        public async Task PrepareArray(Array input) {
            ImgArr = null;
            GC.Collect();
            this.ImgArr = await ImageArray.CreateInstance(input);
        }

        public async Task PrepareImage(IProgress<string> progress, CancellationTokenSource canceltoken) {
            Image = null;
            GC.Collect();
            BitmapSource source = ImageAnalysis.CreateSourceFromArray(ImgArr, System.Windows.Media.PixelFormats.Gray16);

            source.Freeze();
            if (DetectStars) {
                source = await ImageAnalysis.DetectStarsAsync(source, ImgArr, progress, canceltoken);
            } else if (AutoStretch) {
                var img = ImageAnalysis.BitmapFromSource(source);

                var filter = ImageAnalysis.GetColorRemappingFilter(ImgArr.Statistics.Mean, 0.25);
                filter.ApplyInPlace(img);

                source = null;

                source = ImageAnalysis.ConvertBitmap(img, System.Windows.Media.PixelFormats.Gray16);
            }

            source.Freeze();
            Image = source;
        }

        private async Task<BitmapSource> Stretch(BitmapSource source) {
            return await Task<BitmapSource>.Run(() => {
                var img = ImageAnalysis.BitmapFromSource(source);

                var filter = ImageAnalysis.GetColorRemappingFilter(ImgArr.Statistics.Mean, 0.25);
                filter.ApplyInPlace(img);

                source = null;

                source = ImageAnalysis.ConvertBitmap(img, System.Windows.Media.PixelFormats.Gray16);
                return source;
            });
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
                if (Settings.FileType == FileTypeEnum.FITS) {
                    string imagetype = imageType;
                    if (imagetype == "SNAP") imagetype = "LIGHT";
                    SaveFits(completefilename, imageType, exposuretime, filter, binning, ccdtemp);
                }
                else if (Settings.FileType == FileTypeEnum.TIFF) {
                    SaveTiff(completefilename);
                }
                else {
                    SaveTiff(completefilename);
                }


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
                Logger.Error(ex.Message);

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
                Logger.Error(ex.Message);

            }
        }


    }

    public class ImageArray {
        public const ushort HistogramResolution = 1000;

        public ushort[] FlatArray;
        public ImageStatistics Statistics { get; set; }

        private ImageArray() {
            Statistics = new ImageStatistics();
        }


        public static async Task<ImageArray> CreateInstance(Array input) {
            ImageArray imgArray = new ImageArray();

            await Task.Run(() => imgArray.FlipAndConvert(input));
            await Task.Run(() => imgArray.CalculateStatistics());

            return imgArray;
        }

        private void CalculateStatistics() {
            
            /*Calculate StDev and Min/Max Values for Stretch */
            double average = this.FlatArray.Average(x => x);
            double sumOfSquaresOfDifferences = this.FlatArray.Select(val => (val - average) * (val - average)).Sum();
            double sd = Math.Sqrt(sumOfSquaresOfDifferences / this.FlatArray.Length);
            ushort min = 0, max = 0;
            double factor = 2.5;

            if (average - factor * sd < 0) {
                min = 0;
            }
            else {
                min = (ushort)(average - factor * sd);
            }

            if (average + factor * sd > ushort.MaxValue) {
                max = ushort.MaxValue;
            }
            else {
                max = (ushort)(average + factor * sd);
            }

            this.Statistics.StDev = sd;
            this.Statistics.Mean = average;
            this.Statistics.MinNormalizationValue = min;
            this.Statistics.MaxNormalizationValue = max;
        }

        private void FlipAndConvert(Array input) {
            Int32[,] arr = (Int32[,])input;            
            int width = arr.GetLength(0);
            int height = arr.GetLength(1);

            this.Statistics.Width = width;
            this.Statistics.Height = height;
            ushort[] flatArray = new ushort[arr.Length];
            ushort value, histogramkey;
            SortedDictionary<ushort, int> histogram = new SortedDictionary<ushort, int>();
            unsafe
            {
                fixed (Int32* ptr = arr) {
                    int idx = 0, row = 0;
                    for (int i = 0; i < arr.Length; i++) {
                        value = (ushort)ptr[i];




                        idx = ((i % height) * width) + row;
                        if ((i % (height)) == (height - 1)) row++;

                        histogramkey = Convert.ToUInt16(Math.Round(((double)ImageArray.HistogramResolution / ushort.MaxValue) * value));
                        if (histogram.ContainsKey(histogramkey)) {
                            histogram[histogramkey] += 1;
                        }
                        else {
                            histogram.Add(histogramkey, 1);
                        }

                        ushort b = value;
                        flatArray[idx] = b;


                    }
                }
            }

            this.Statistics.Histogram = histogram;
            this.FlatArray = flatArray;
        }
   
    }

    public class ImageStatistics : BaseINPC {
        public int Width { get; set; }
        public int Height { get; set; }
        public double StDev { get; set; }
        public double Mean { get; set; }
        double _hFR;
        public double HFR {
            get {
                return _hFR;
            }
            set {
                _hFR = value;
                RaisePropertyChanged();
            }
        }

        int _detectedStars;
        public int DetectedStars {
            get {
                return _detectedStars;
            }
            set {
                _detectedStars = value;
                RaisePropertyChanged();
            }
        }

        public ushort MinNormalizationValue { get; set; }
        public ushort MaxNormalizationValue { get; set; }
        public SortedDictionary<ushort, int> Histogram { get; set; }
    }

}
