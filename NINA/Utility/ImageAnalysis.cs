using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using NINA.Model.MyCamera;
using NINA.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINA.Utility {
    class ImageAnalysis {

        private static System.Drawing.Pen ELLIPSEPEN = new System.Drawing.Pen(System.Drawing.Brushes.LightYellow, 1);
        private static SolidBrush TEXTBRUSH = new SolidBrush(System.Drawing.Color.Yellow);
        private static System.Drawing.FontFamily FONTFAMILY = new System.Drawing.FontFamily("Arial");
        private static Font FONT = new Font(FONTFAMILY, 32, System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel);

        public ImageAnalysis(BitmapSource source, ImageArray iarr) {
            _iarr = iarr;
            _originalBitmapSource = source;
                        
            _resizefactor = 1.0;
            if (iarr.Statistics.Width > _maxWidth) {
                _resizefactor = (double)_maxWidth / iarr.Statistics.Width;
            }
            _inverseResizefactor = 1.0 / _resizefactor;

            _minStarSize = (int)Math.Floor(5 * _resizefactor);
            //Prevent Hotpixels to be detected
            if (_minStarSize < 2) _minStarSize = 2;
            _maxStarSize = (int)Math.Ceiling(150 * _resizefactor);
        }

        private int _maxWidth = 1552;
        private int _minStarSize;
        private int _maxStarSize;
        private double _resizefactor;
        private double _inverseResizefactor;
        private ImageArray _iarr;
        private BitmapSource _originalBitmapSource;
        private BlobCounter _blobCounter;
        private Bitmap _bitmapToAnalyze;
        private CancellationToken _token;
        private List<Star> _starlist = new List<Star>();

        public int DetectedStars { get; private set; }
        public double AverageHFR { get; private set; }

        class Star {
            public double radius;
            public double HFR;
            public AForge.Point Position;
            public List<PixelData> Pixeldata;
            public double Average {
                get {
                    return Pixeldata.Average((x) => x.value);
                }
            }
            public Rectangle Rectangle;
            public Star() {
                Pixeldata = new List<PixelData>();
            }

            public void CalculateHfr() {
                double hfr = 0.0d;
                if (this.Pixeldata.Count > 0) {                                    
                    double outerRadius = this.radius;
                    double sum = 0, sumDist = 0;

                    int centerX = (int)Math.Floor(this.Position.X);
                    int centerY = (int)Math.Floor(this.Position.Y);

                    foreach (PixelData data in this.Pixeldata) {
                        if (InsideCircle(data.PosX,data.PosY,this.Position.X,this.Position.Y,outerRadius)) {
                            if (data.value < 0) data.value = 0;

                            sum += data.value;
                            sumDist += data.value * Math.Sqrt(Math.Pow((double)data.PosX - (double)centerX,2.0d) + Math.Pow((double)data.PosY - (double)centerY,2.0d));
                        }
                    }

                    if (sum > 0) {
                        hfr = sumDist / sum;
                    }
                    else {
                        hfr = Math.Sqrt(2) * outerRadius;
                    }
                }
                this.HFR = hfr;
            }

            private bool InsideCircle(double x,double y,double centerX,double centerY,double radius) {
                return (Math.Pow(x - centerX,2) + Math.Pow(y - centerY,2) <= Math.Pow(radius,2));
            }
        }

        class PixelData {
            public int PosX;
            public int PosY;
            public ushort value;

            public override string ToString() {
                return value.ToString();
            }
        }
        
        

        public async Task DetectStarsAsync(IProgress<string> progress, CancellationToken token) {
            _token = token;
            await Task.Run(() => DetectStars(progress));
        }

        public void DetectStars(IProgress<string> progress) {            
            try {

                Stopwatch overall = Stopwatch.StartNew();
                progress?.Report("Preparing image for star detection");

                Stopwatch sw = Stopwatch.StartNew();

                _bitmapToAnalyze = Convert16BppTo8Bpp(_originalBitmapSource);

                Debug.Print("Time to convert to 8bit Image: " + sw.Elapsed);

                sw.Restart();

                _token.ThrowIfCancellationRequested();

                /* Resize to speed up manipulation */
                ResizeBitmapToAnalyze();
                

                /* prepare image for structure detection */
                PrepareForStructureDetection(_bitmapToAnalyze);
                
                progress?.Report("Detecting structures");

                /* get structure info */
                _blobCounter = DetectStructures(_bitmapToAnalyze);
                                
                progress?.Report("Analyzing stars");

                _starlist = IdentifyStars();
                                
                _token.ThrowIfCancellationRequested();                
                                
                if (_starlist.Count > 0) {
                    var m = (from star in _starlist select star.HFR).Average();
                    Debug.Print("Mean HFR: " + m);
                    //todo change
                    AverageHFR = m;
                    DetectedStars = _starlist.Count;
                }
                
                sw.Stop();
                sw = null;
                
                _blobCounter = null;                
                _bitmapToAnalyze.Dispose();                
                overall.Stop();
                Debug.Print("Overall star detection: " + overall.Elapsed);
                overall = null;

            }
            catch (OperationCanceledException) {
                progress?.Report("Operation cancelled");
            }
            return;
        }

        private List<Star> IdentifyStars() {
            Blob[] blobs = _blobCounter.GetObjectsInformation();
            SimpleShapeChecker checker = new SimpleShapeChecker();
            List<Star> starlist = new List<Star>();

            foreach (Blob blob in blobs) {
                _token.ThrowIfCancellationRequested();

                if (blob.Rectangle.Width > _maxStarSize || blob.Rectangle.Height > _maxStarSize || blob.Rectangle.Width < _minStarSize || blob.Rectangle.Height < _minStarSize) {
                    continue;
                }
                var points = _blobCounter.GetBlobsEdgePoints(blob);
                AForge.Point centerpoint; float radius;
                var rect = new Rectangle((int)Math.Floor(blob.Rectangle.X * _inverseResizefactor),(int)Math.Floor(blob.Rectangle.Y * _inverseResizefactor),(int)Math.Ceiling(blob.Rectangle.Width * _inverseResizefactor),(int)Math.Ceiling(blob.Rectangle.Height * _inverseResizefactor));
                //Star is circle
                Star s;
                if (checker.IsCircle(points,out centerpoint,out radius)) {
                    s = new Star { Position = new AForge.Point(centerpoint.X * (float)_inverseResizefactor,centerpoint.Y * (float)_inverseResizefactor),radius = radius * _inverseResizefactor,Rectangle = rect };
                }
                else { //Star is elongated
                    s = new Star { Position = new AForge.Point(centerpoint.X * (float)_inverseResizefactor,centerpoint.Y * (float)_inverseResizefactor),radius = Math.Max(rect.Width,rect.Height) / 2,Rectangle = rect };
                }
                /* get pixeldata */                
                for (int x = s.Rectangle.X;x < s.Rectangle.X + s.Rectangle.Width;x++) {
                    for (int y = s.Rectangle.Y;y < s.Rectangle.Y + s.Rectangle.Height;y++) {
                        var value = _iarr.FlatArray[x + (_iarr.Statistics.Width * y)] - _iarr.Statistics.Mean;
                        if (value < 0) { value = 0; }
                        PixelData pd = new PixelData { PosX = x,PosY = y,value = (ushort)value };                        
                        s.Pixeldata.Add(pd);
                    }
                }
                s.CalculateHfr();
                starlist.Add(s);

            }

            return starlist;
        }

        public BitmapSource GetAnnotatedImage() {
            Bitmap bmp = Convert16BppTo8Bpp(_originalBitmapSource);

            Bitmap newBitmap = new Bitmap(bmp.Width,bmp.Height,System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            Graphics graphics = Graphics.FromImage(newBitmap);
            graphics.DrawImage(bmp,0,0);

            if (_starlist.Count > 0) {
                int r, offset = 10;
                float textposx, textposy;

                var threshhold = 200;
                if (_starlist.Count > threshhold) {
                    _starlist.Sort((item1,item2) => item2.Average.CompareTo(item1.Average));
                    _starlist = _starlist.GetRange(0,threshhold);
                }

                foreach (Star star in _starlist) {
                    _token.ThrowIfCancellationRequested();
                    r = (int)Math.Ceiling(star.radius);
                    textposx = star.Position.X - offset;
                    textposy = star.Position.Y - offset;
                    graphics.DrawEllipse(ELLIPSEPEN,new RectangleF(star.Rectangle.X,star.Rectangle.Y,star.Rectangle.Width,star.Rectangle.Height));
                    graphics.DrawString(star.HFR.ToString("##.##"),FONT,TEXTBRUSH,new PointF(Convert.ToSingle(textposx - 1.5 * offset),Convert.ToSingle(textposy + 2.5 * offset)));
                }
            }
            var img = ConvertBitmap(newBitmap,System.Windows.Media.PixelFormats.Bgr24);
            newBitmap.Dispose();
            img.Freeze();
            return img;
        }

        private BlobCounter DetectStructures(Bitmap bmp) {
            var sw = Stopwatch.StartNew();

            /* detect structures */            
            BlobCounter blobCounter = new BlobCounter();
            blobCounter.ProcessImage(bmp);

            _token.ThrowIfCancellationRequested();
            
            sw.Stop();
            Debug.Print("Time for structure detection: " + sw.Elapsed);
            sw = null;
            
            return blobCounter;
        }

        private void PrepareForStructureDetection(Bitmap bmp) {
            var sw = Stopwatch.StartNew();

            new CannyEdgeDetector().ApplyInPlace(bmp);
            _token.ThrowIfCancellationRequested();
            new SISThreshold().ApplyInPlace(bmp);
            _token.ThrowIfCancellationRequested();
            new BinaryDilatation3x3().ApplyInPlace(bmp);
            _token.ThrowIfCancellationRequested();

            sw.Stop();
            Debug.Print("Time for image preparation: " + sw.Elapsed);
            sw = null;
        }

        private void ResizeBitmapToAnalyze() {            
            if (_bitmapToAnalyze.Width > _maxWidth) {
                _bitmapToAnalyze = new ResizeBicubic(_maxWidth,(int)Math.Floor(_bitmapToAnalyze.Height * _resizefactor)).Apply(_bitmapToAnalyze);
            }            
        }


        public static ColorRemapping16bpp GetColorRemappingFilter(double mean,double targetHistogramMeanPct) {

            ushort[] map = GetStretchMap(mean,targetHistogramMeanPct);

            var filter = new ColorRemapping16bpp(map);

            return filter;
        }

        private static ushort[] GetStretchMap(double mean,double targetHistogramMeanPct) {
            double power;
            if (mean <= 1) {
                power = Math.Log(ushort.MaxValue * targetHistogramMeanPct,2);
            }
            else {
                power = Math.Log(ushort.MaxValue * targetHistogramMeanPct,mean);
            }

            ushort[] map = new ushort[ushort.MaxValue + 1];

            for (int i = 2;i < map.Length;i++) {
                map[i] = (ushort)Math.Min(ushort.MaxValue,Math.Pow(i,power));
            }
            map[0] = 0;
            map[1] = (ushort)(map[2] / 2);

            return map;
        }

        public static BitmapSource ConvertBitmap(System.Drawing.Bitmap bitmap, System.Windows.Media.PixelFormat pf) {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height, 96, 96, pf, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }

        public static Bitmap BitmapFromSource(BitmapSource source) {
            return BitmapFromSource(source, System.Drawing.Imaging.PixelFormat.Format16bppGrayScale);
        }

        public static Bitmap BitmapFromSource(BitmapSource source, System.Drawing.Imaging.PixelFormat pf) {
            Bitmap bmp = new Bitmap(
                    source.PixelWidth,
                    source.PixelHeight,
                    pf);
            BitmapData data = bmp.LockBits(
                    new Rectangle(System.Drawing.Point.Empty, bmp.Size),
                    ImageLockMode.WriteOnly,
                    pf);
            source.CopyPixels(
                    Int32Rect.Empty,
                    data.Scan0,
                    data.Height * data.Stride,
                    data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }

        public static Bitmap Convert16BppTo8Bpp(Bitmap bmp) {
            return AForge.Imaging.Image.Convert16bppTo8bpp(bmp);
        }

        public static Bitmap Convert16BppTo8Bpp(BitmapSource source) {
            return AForge.Imaging.Image.Convert16bppTo8bpp(BitmapFromSource(source));
        }

        public static BitmapSource Convert16BppTo8BppSource(BitmapSource source) {
            FormatConvertedBitmap s = new FormatConvertedBitmap();
            s.BeginInit();
            s.Source = source;
            s.DestinationFormat = System.Windows.Media.PixelFormats.Gray8;
            s.EndInit();
            s.Freeze();
            return s;
        }

        public static BitmapSource CreateSourceFromArray(ImageArray arr ,System.Windows.Media.PixelFormat pf) {

            //int stride = C.CameraYSize * ((Convert.ToString(C.MaxADU, 2)).Length + 7) / 8;
            int stride = (arr.Statistics.Width * pf.BitsPerPixel + 7) / 8;
            double dpi = 96;

            BitmapSource source = BitmapSource.Create(arr.Statistics.Width, arr.Statistics.Height, dpi, dpi, pf, null, arr.FlatArray, stride);
            source.Freeze();
            return source;
        }

        public static BitmapSource Debayer(BitmapSource source, System.Drawing.Imaging.PixelFormat pf) {            
            if (pf == System.Drawing.Imaging.PixelFormat.Format16bppGrayScale) {
                source = Convert16BppTo8BppSource(source);
            } else if (pf == System.Drawing.Imaging.PixelFormat.Format8bppIndexed) {
                
            } else {
                throw new NotSupportedException();
            }
            var bmp = BitmapFromSource(source, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            bmp = Debayer(bmp);
            var newSource = ConvertBitmap(bmp, PixelFormats.Rgb24);
            newSource.Freeze();
            return newSource;
        }

        public static Bitmap Debayer(Bitmap bmp) {
            var filter = new BayerFilterOptimized();
            filter.Pattern = BayerPattern.BGGR;
            var debayered = filter.Apply(bmp);
            return debayered;
        }
    }



    public class ColorRemapping16bpp : ColorRemapping {
        private ushort[] _grayMap16;
        public ushort[] GrayMap16 {
            get { return _grayMap16; }
            set {
                // check the map
                if ((value == null) || (value.Length != 65536))
                    throw new ArgumentException("A map should be array with 65536 value.");

                _grayMap16 = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorRemapping16bpp"/> class.
        /// </summary>
        /// 
        /// <param name="grayMap">Gray map.</param>
        /// 
        /// <remarks>This constructor is supposed for 16bit grayscale images.</remarks>
        /// 
        public ColorRemapping16bpp(ushort[] grayMap) : base() {
            FormatTranslations[System.Drawing.Imaging.PixelFormat.Format16bppGrayScale] = System.Drawing.Imaging.PixelFormat.Format16bppGrayScale;
            GrayMap16 = grayMap;
        }

        /// <summary>
        /// Process the filter on the specified image.
        /// </summary>
        /// 
        /// <param name="image">Source image data.</param>
        /// <param name="rect">Image rectangle for processing by the filter.</param>
        ///
        protected override unsafe void ProcessFilter(UnmanagedImage image, Rectangle rect) {
            if (image.PixelFormat != System.Drawing.Imaging.PixelFormat.Format16bppGrayScale) { 
                throw new UnsupportedImageFormatException("Source pixel format is not supported by the routine.");
            }

            int pixelSize = System.Drawing.Image.GetPixelFormatSize(image.PixelFormat) / 8;

            // processing start and stop X,Y positions
            int startX = rect.Left;
            int startY = rect.Top;
            int stopX = startX + rect.Width;
            int stopY = startY + rect.Height;
            int offset = image.Stride - rect.Width * pixelSize;

            // do the job
            ushort* ptr = (ushort*)image.ImageData.ToPointer();

            // allign pointer to the first pixel to process
            ptr += (startY * image.Stride + startX * pixelSize);

            
            // grayscale image
            for (int y = startY; y < stopY; y++) {
                for (int x = startX; x < stopX; x++, ptr++) {
                    // gray
                    *ptr = GrayMap16[*ptr];
                }
                ptr += offset;
            }
            
        }
    }

    
}
