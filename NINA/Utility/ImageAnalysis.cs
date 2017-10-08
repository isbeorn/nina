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
        private static System.Drawing.FontFamily FONTFAMILY = new System.Drawing.FontFamily("Times New Roman");
        private static Font FONT = new Font(FONTFAMILY, 32, System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel);

        private ImageAnalysis() {

        }

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
        }

        public class PixelData {
            public int PosX;
            public int PosY;
            public ushort value;

            public override string ToString() {
                return value.ToString();
            }
        }





        static double CalculateHfr(Star s) {
            double hfr = 0.0d;
            double outerRadius = s.radius;            
            double sum = 0, sumDist = 0;

            int centerX = (int)Math.Floor(s.Position.X);
            int centerY = (int)Math.Floor(s.Position.Y);

            foreach (PixelData data in s.Pixeldata) {
                if (InsideCircle(data.PosX, data.PosY, s.Position.X, s.Position.Y, outerRadius)) {                    
                    if (data.value < 0) data.value = 0;

                    sum += data.value;
                    sumDist += data.value * Math.Sqrt(Math.Pow((double)data.PosX - (double)centerX, 2.0d) + Math.Pow((double)data.PosY - (double)centerY, 2.0d));
                }
            }

            if (sum > 0) {
                hfr = sumDist / sum;
            }
            else {
                hfr = Math.Sqrt(2) * outerRadius;
            }


            return hfr;
        }

        static bool InsideCircle(double x, double y, double centerX, double centerY, double radius) {
            return (Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2) <= Math.Pow(radius, 2));
        }

        public static async Task<BitmapSource> DetectStarsAsync(BitmapSource source, ImageArray iarr, IProgress<string> progress, CancellationTokenSource canceltoken) {
            return await Task.Run<BitmapSource>(() => DetectStars(source, iarr, progress, canceltoken));
        }



        public static ColorRemapping16bpp GetColorRemappingFilter(double mean, double targetHistogramMeanPct) {                        

            ushort[] map = GetStretchMap(mean, targetHistogramMeanPct);

            var filter = new ColorRemapping16bpp(map);            

            return filter;
        }

        private static ushort[] GetStretchMap(double mean, double targetHistogramMeanPct) {
            double power;
            if (mean <= 1) {
                power = Math.Log(ushort.MaxValue * targetHistogramMeanPct, 2);
            }
            else {
                power = Math.Log(ushort.MaxValue * targetHistogramMeanPct, mean);
            }
            
            ushort[] map = new ushort[ushort.MaxValue + 1];

            for (int i = 2; i < map.Length; i++) {
                map[i] = (ushort)Math.Min(ushort.MaxValue , Math.Pow(i, power));
            }
            map[0] = 0;
            map[1] = (ushort)(map[2] / 2);

            return map;
        }
        

        public static BitmapSource DetectStars(BitmapSource source, ImageArray iarr, IProgress<string> progress, CancellationTokenSource canceltoken) {
            BitmapSource result = null;
            try {

                Stopwatch overall = Stopwatch.StartNew();
                progress?.Report("Preparing image for star detection");

                Stopwatch sw = Stopwatch.StartNew();                

                Debug.Print("Time to convert to 8bit Image: " + sw.Elapsed);

                sw.Restart();
                
                Bitmap orig = BitmapFromSource(source);
                var filter = GetColorRemappingFilter(iarr.Statistics.Mean, 0.20);
                filter.ApplyInPlace(orig);

                orig = Convert16BppTo8Bpp(orig);

                Bitmap bmp = orig.Clone(new Rectangle(0, 0, orig.Width, orig.Height), System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                
                canceltoken?.Token.ThrowIfCancellationRequested();

                /* Resize to speed up manipulation */
                double resizefactor = Resize(ref bmp);
                double inverseresizefactor = 1.0d / resizefactor;

                /* prepare image for structure detection */
                PrepareForStructureDetection(bmp, canceltoken?.Token);
                
                progress?.Report("Detecting structures");

                /* get structure info */
                BlobCounter blobCounter = DetectStructures(bmp,resizefactor,canceltoken?.Token);
                                
                progress?.Report("Analyzing stars");

                int minStarSize = (int)Math.Floor(5 * resizefactor);
                //Prevent Hotpixels to be detected
                if (minStarSize < 2) minStarSize = 2;
                int maxStarSize = (int)Math.Ceiling(150 * resizefactor);

                List<Star> starlist = IdentifyStars(blobCounter,iarr,minStarSize,maxStarSize,inverseresizefactor,canceltoken?.Token);
                                
                canceltoken?.Token.ThrowIfCancellationRequested();
                progress?.Report("Annotating image");
                                
                if (starlist.Count > 0) {
                    var m = (from star in starlist select star.HFR).Average();
                    Debug.Print("Mean HFR: " + m);
                    iarr.Statistics.HFR = m;
                    iarr.Statistics.DetectedStars = starlist.Count;
                }

                var newBitmap = AnnotateImage(orig, starlist, canceltoken?.Token);
                
                result = ConvertBitmap(newBitmap, System.Windows.Media.PixelFormats.Bgr24);

                Debug.Print("Time to annotate image: " + sw.Elapsed);
                sw.Stop();
                sw = null;
                
                blobCounter = null;
                orig.Dispose();
                bmp.Dispose();
                newBitmap.Dispose();
                overall.Stop();
                Debug.Print("Overall star detection: " + overall.Elapsed);
                overall = null;

            }
            catch (OperationCanceledException) {
                progress?.Report("Operation cancelled");
            }
            result.Freeze();
            return result;
        }

        private static List<Star> IdentifyStars(
                BlobCounter blobCounter, 
                ImageArray iarr,
                double minStarSize, 
                double maxStarSize, 
                double inverseresizefactor, 
                CancellationToken? token) {
            Blob[] blobs = blobCounter.GetObjectsInformation();
            SimpleShapeChecker checker = new SimpleShapeChecker();
            List<Star> starlist = new List<Star>();

            foreach (Blob blob in blobs) {
                token?.ThrowIfCancellationRequested();

                if (blob.Rectangle.Width > maxStarSize || blob.Rectangle.Height > maxStarSize || blob.Rectangle.Width < minStarSize || blob.Rectangle.Height < minStarSize) {
                    continue;
                }
                var points = blobCounter.GetBlobsEdgePoints(blob);
                AForge.Point centerpoint; float radius;
                var rect = new Rectangle((int)Math.Floor(blob.Rectangle.X * inverseresizefactor),(int)Math.Floor(blob.Rectangle.Y * inverseresizefactor),(int)Math.Ceiling(blob.Rectangle.Width * inverseresizefactor),(int)Math.Ceiling(blob.Rectangle.Height * inverseresizefactor));
                //Star is circle
                Star s;
                if (checker.IsCircle(points,out centerpoint,out radius)) {
                    s = new Star { Position = new AForge.Point(centerpoint.X * (float)inverseresizefactor,centerpoint.Y * (float)inverseresizefactor),radius = radius * inverseresizefactor,Rectangle = rect };
                }
                else { //Star is elongated
                    s = new Star { Position = new AForge.Point(centerpoint.X * (float)inverseresizefactor,centerpoint.Y * (float)inverseresizefactor),radius = Math.Max(rect.Width,rect.Height) / 2,Rectangle = rect };
                }
                /* get pixeldata */                
                for (int x = s.Rectangle.X;x < s.Rectangle.X + s.Rectangle.Width;x++) {
                    for (int y = s.Rectangle.Y;y < s.Rectangle.Y + s.Rectangle.Height;y++) {
                        var value = iarr.FlatArray[x + (iarr.Statistics.Width * y)] - iarr.Statistics.Mean;
                        if (value < 0) { value = 0; }
                        PixelData pd = new PixelData { PosX = x,PosY = y,value = (ushort)value };
                        s.Pixeldata.Add(pd);
                    }
                }
                s.HFR = CalculateHfr(s);
                starlist.Add(s);

            }

            return starlist;
        }

        private static Bitmap AnnotateImage(Bitmap bmp, List<Star> starlist, CancellationToken? token) {
            Bitmap newBitmap = new Bitmap(bmp.Width,bmp.Height,System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            Graphics graphics = Graphics.FromImage(newBitmap);
            graphics.DrawImage(bmp,0,0);

            if (starlist.Count > 0) {
                int r, offset = 10;
                float textposx, textposy;                
                
                var threshhold = 200;
                if (starlist.Count > threshhold) {
                    starlist.Sort((item1,item2) => item2.Average.CompareTo(item1.Average));
                    starlist = starlist.GetRange(0,threshhold);
                }

                foreach (Star star in starlist) {
                    token?.ThrowIfCancellationRequested();
                    r = (int)Math.Ceiling(star.radius);
                    textposx = star.Position.X - offset;
                    textposy = star.Position.Y - offset;
                    graphics.DrawEllipse(ELLIPSEPEN,new RectangleF(star.Rectangle.X,star.Rectangle.Y,star.Rectangle.Width,star.Rectangle.Height));
                    graphics.DrawString(star.HFR.ToString("##.##"),FONT,TEXTBRUSH,new PointF(Convert.ToSingle(textposx - 1.5 * offset),Convert.ToSingle(textposy + 2.5 * offset)));
                }
            }

            return newBitmap;
        }

        private static BlobCounter DetectStructures(Bitmap bmp, double resizefactor, CancellationToken? token) {
            var sw = Stopwatch.StartNew();

            /* detect structures */            
            BlobCounter blobCounter = new BlobCounter();
            blobCounter.ProcessImage(bmp);

            token?.ThrowIfCancellationRequested();
            
            sw.Stop();
            Debug.Print("Time for structure detection: " + sw.Elapsed);
            sw = null;
            
            return blobCounter;
        }

        private static void PrepareForStructureDetection(Bitmap bmp, CancellationToken? token) {
            var sw = Stopwatch.StartNew();

            new CannyEdgeDetector().ApplyInPlace(bmp);
            token?.ThrowIfCancellationRequested();
            new SISThreshold().ApplyInPlace(bmp);
            token?.ThrowIfCancellationRequested();
            new BinaryDilatation3x3().ApplyInPlace(bmp);
            token?.ThrowIfCancellationRequested();

            sw.Stop();
            Debug.Print("Time for image preparation: " + sw.Elapsed);
            sw = null;
        }

        public static double Resize(ref Bitmap bmp) {
            int targetWidth = 1552;
            double resizefactor = 1.0;
            if (bmp.Width > targetWidth) {
                resizefactor = (double)targetWidth / bmp.Width;

                bmp = new ResizeBicubic(targetWidth,(int)Math.Floor(bmp.Height * resizefactor)).Apply(bmp);
            }            

            return resizefactor;
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
