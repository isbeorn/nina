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
            ZoomFactor = 1;

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


        /*public async Task PrepareArray(Array input) {
            ImgArr = null;
            GC.Collect();
            this.ImgArr = await ImageArray.CreateInstance(input);
        }*/

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

            var filter = ImageAnalysis.GetColorRemappingFilter(ImgArr.Statistics.Mean, 0.25);
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

                
                if (Settings.FileType == FileTypeEnum.FITS) {
                    string imagetype = imageType;
                    if (imagetype == "SNAP") imagetype = "LIGHT";
                    SaveFits(completefilename, imageType, exposuretime, filter, binning, ccdtemp);
                }
                else if (Settings.FileType == FileTypeEnum.TIFF) {
                    SaveTiff(completefilename);
                }
                else if (Settings.FileType == FileTypeEnum.XISF) {
                    SaveXisf(completefilename, imageType, exposuretime, filter, binning, ccdtemp);
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

        private void SaveXisf(String path, string imagetype, double duration, string filter, string binning, double temp) {
            try {
                Stopwatch sw = Stopwatch.StartNew();
                                
                var header = new XISFHeader();

                header.AddEmbeddedImage(ImgArr, imagetype);

                header.AddMetaDataProperty("Instrument:Camera:XBinning", "Int", binning.Substring(0,1));
                header.AddMetaDataProperty("Instrument:Camera:YBinning", "Int", binning.Substring(2,1));
                header.AddMetaDataProperty("Instrument:Filter:Name", "String", filter);

                if(!double.IsNaN(temp)) {
                    header.AddMetaDataProperty("Instrument:Sensor:Temperature", "Float", temp.ToString());
                }                

                header.AddMetaDataProperty("Instrument:ExposureTime", "Float", duration.ToString());

                /*
                 Instrument:Camera:Gain Float
                 Instrument:Camera:ISOSpeed Int
                 Instrument:Camera:Name String
                 Instrument:Camera:XBinning Int
                 Instrument:Camera:YBinning Int
                 Instrument:ExposureTime Float
                 Instrument:Filter:Name String
                 Instrument:Sensor:Temperature Float
                 Instrument:Sensor:XPixelSize Float
                 Instrument:Sensor:YPixelSize Float
                 Instrument:Telescope:Aperture Float
                 Instrument:Telescope:FocalLength Float
                 Instrument:Telescope:Name String

                */

                //var data = new XISFData(ImgArr.FlatArray);
                XISF img = new XISF(header);

                img.Save(path);

                sw.Stop();
                Debug.Print("Time to save XISF: " + sw.Elapsed);
                sw = null;

                } catch (Exception ex) {
                Notification.ShowError("Image file error: " + ex.Message);
                Logger.Error(ex.Message);

            }
        }


    }

    

    class XISF {
        XISFHeader Header { get; set; }

        public XISF(XISFHeader header) {
            this.Header = header;
        }
        
        public bool Save(string path) {
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            
            using (FileStream fs = new FileStream(path + ".xisf", FileMode.Create)) {
                /* Header */
                Header.Save(fs);                
            }

            return true;
        }
    }

    /**
     * Specifications: http://pixinsight.com/doc/docs/XISF-1.0-spec/XISF-1.0-spec.html#xisf_header
     */
    class XISFHeader {
        public XDocument Header { get; set; }
                
        public XElement MetaData { get; set; }
        public XElement Image { get; set; }
        private XElement Xisf;

        XNamespace xmlns = XNamespace.Get("http://www.pixinsight.com/xisf");
        XNamespace xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
        XNamespace propertyns = "XISF";

        /* Create Header with embedded Image */
        public XISFHeader() {
            Xisf = new XElement(xmlns + "xisf",
                    new XAttribute("version", "1.0"),
                    new XAttribute("xmlns", "http://www.pixinsight.com/xisf"),
                    new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                    new XAttribute(xsi + "schemaLocation", "http://www.pixinsight.com/xisf http://pixinsight.com/xisf/xisf-1.0.xsd")
            );            

            MetaData = new XElement("Metadata");

            AddMetaDataProperty("CreationTime", "String", DateTime.UtcNow.ToString("o"));
            AddMetaDataProperty("CreatorApplication", "String", "Nighttime Imaging 'N' Astronomy");

            Xisf.Add(MetaData);

            Header = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                Xisf
            );
        }

        public void AddMetaDataProperty(string id, string type, string value) {
            XElement xelem;
            if(type == "String") {
                xelem = new XElement("Property",
                    new XAttribute("id", "XISF:" + id),
                    new XAttribute("type", type),
                    value
                );
            } else {
                xelem = new XElement("Property",
                    new XAttribute("id", "XISF:" + id),
                    new XAttribute("type", type),
                    new XAttribute("value", value)
                );
            }

            MetaData.Add(xelem);
        }

        public void AddEmbeddedImage(ImageArray arr, string imageType) {

            var image = new XElement("Image",
                    new XAttribute("geometry", arr.Statistics.Width + ":" + arr.Statistics.Height + ":" + "1"),
                    new XAttribute("sampleFormat", "UInt16"),
                    new XAttribute("imageType", imageType),
                    new XAttribute("location", "embedded"),
                    new XAttribute("colorSpace", "Gray")
                    );
                        
            byte[] result = new byte[arr.FlatArray.Length * sizeof(ushort)];
            Buffer.BlockCopy(arr.FlatArray, 0, result, 0, result.Length);            
            var s = Convert.ToBase64String(result);

            var data = new XElement("Data", new XAttribute("encoding", "base64"), s);

            image.Add(data);
            Xisf.Add(image);
        }

        public void Save(Stream s) {
            /*XISF0100*/
            byte[] monolithicsignature = new byte[] { 88, 73, 83, 70, 48, 49, 48, 48 };
            s.Write(monolithicsignature, 0, monolithicsignature.Length);

            /*Xml header length */
            var headerlength = BitConverter.GetBytes(System.Text.ASCIIEncoding.UTF8.GetByteCount(Header.ToString()));
            s.Write(headerlength, 0, headerlength.Length);

            /*reserved space 4 byte must be 0 */
            var reserved = new byte[] { 0, 0, 0, 0 };
            s.Write(reserved, 0, reserved.Length);

            using (StreamWriter sw = new StreamWriter(s, Encoding.UTF8)) {                
                sw.Write(Header.ToString());
            }
        }
    }

    class XISFData {
        public ushort[] Data;
        public XISFData(ushort[] data) {
            this.Data = data;
        }
    }


}
