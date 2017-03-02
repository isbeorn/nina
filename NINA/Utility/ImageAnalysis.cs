using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
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
        //private static System.Drawing.Pen HFRELLIPSEPEN = new System.Drawing.Pen(System.Drawing.Brushes.Pink, 1);
        private static SolidBrush TEXTBRUSH = new SolidBrush(System.Drawing.Color.Yellow);
        private static System.Drawing.FontFamily FONTFAMILY = new System.Drawing.FontFamily("Times New Roman");
        private static Font FONT = new Font(FONTFAMILY, 32, System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel);

        const int OUTERRADIUS = 21;

        public ImageAnalysis() {

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





        double CalculateHfr(Star s, double mean) {
            double hfr = 0.0d;
            double outerRadius = OUTERRADIUS;
            double sum = 0, sumDist = 0;

            int centerX = (int)Math.Floor(s.Position.X);
            int centerY = (int)Math.Floor(s.Position.Y);

            foreach (PixelData data in s.Pixeldata) {
                if (InsideCircle(data.PosX, data.PosY, s.Position.X, s.Position.Y, outerRadius)) {
                    data.value = (ushort)(data.value - mean);
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

        bool InsideCircle(double x, double y, double centerX, double centerY, double radius) {
            return (Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2) <= Math.Pow(radius, 2));
        }

        public async Task<BitmapSource> DetectStarsAsync(Utility.ImageArray iarr, IProgress<string> progress, CancellationTokenSource canceltoken) {
            return await Task.Run<BitmapSource>(() => DetectStars(iarr, progress, canceltoken));
        }



        private static ColorRemapping GetColorRemappingFilter(double mean, double targetHistogramMeanPct) {            
            double power;
            if (mean <= 1) {
                power = Math.Log(ushort.MaxValue * targetHistogramMeanPct, 2);
            }
            else {
                power = Math.Log(ushort.MaxValue * targetHistogramMeanPct, mean);
            }

            byte[] map = new byte[256];

            for (int i = 2; i < 256; i++) {
                map[i] = (byte)Math.Min(byte.MaxValue, Math.Pow(i, power));
            }
            map[0] = 0;
            map[1] = (byte)(map[2] / 2);

            var filter = new AForge.Imaging.Filters.ColorRemapping();
            filter.GrayMap = map;

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

            ushort[] map = new ushort[65536];

            for (int i = 2; i < 65536; i++) {
                map[i] = (ushort)Math.Min(ushort.MaxValue , Math.Pow(i, power));
            }
            map[0] = 0;
            map[1] = (ushort)(map[2] / 2);

            

            return map;
        }

        /// <summary>
        /// Rewritten ColorRemappingFilter to work in 16bit Grayscale pictues - 8bit loses too much precision
        /// </summary>
        /// <param name="source"></param>
        /// <param name="mean"></param>
        /// <param name="targetHistogramMeanPct"></param>
        /// <returns></returns>
        public static unsafe Bitmap LinearStretch(Bitmap source, double mean, double targetHistogramMeanPct) {

            // lock source bitmap data
            BitmapData data = source.LockBits(
                new Rectangle(0, 0, source.Width, source.Height),
                ImageLockMode.ReadWrite, source.PixelFormat);

            Bitmap result;

            try {
                UnmanagedImage image = new UnmanagedImage(data);
            
            
               var grayMap = GetStretchMap(mean, targetHistogramMeanPct);

                int pixelSize = System.Drawing.Image.GetPixelFormatSize(image.PixelFormat) / 8;

                // processing start and stop X,Y positions
                int startX = 0;
                int startY = 0;
                int stopX = image.Width;
                int stopY = image.Height;
                int offset = image.Stride - image.Width * pixelSize;

                // do the job
                ushort* ptr = (ushort*)image.ImageData.ToPointer();

                // allign pointer to the first pixel to process
                ptr += (startY * image.Stride + startX * pixelSize);

            
                for (int y = startY; y < stopY; y++) {
                    for (int x = startX; x < stopX; x++, ptr++) {
                        // gray
                        *ptr = grayMap[*ptr];
                    }
                    ptr += offset;
                }
                result = image.ToManagedImage();
            }
            finally {
                source.UnlockBits(data);
            }
            return result;
            
        }

        public BitmapSource DetectStars(Utility.ImageArray iarr, IProgress<string> progress, CancellationTokenSource canceltoken) {
            BitmapSource result = null;
            try {

                Stopwatch overall = Stopwatch.StartNew();
                progress.Report("Preparing image for star detection");

                Stopwatch sw = Stopwatch.StartNew();
                var tmpsrc = ViewModel.ImagingVM.Prepare(iarr.FlatArray, iarr.X, iarr.Y).Result;

                Debug.Print("Time to convert to 8bit Image: " + sw.Elapsed);

                sw.Restart();

                //Bitmap orig = Convert16BppTo8Bpp(tmpsrc);
                Bitmap orig = BitmapFromSource(tmpsrc);
                orig = LinearStretch(orig, iarr.Mean, 0.15);
                orig = Convert16BppTo8Bpp(orig);

                Bitmap bmp = orig.Clone(new Rectangle(0, 0, orig.Width, orig.Height), System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                
                canceltoken.Token.ThrowIfCancellationRequested();


                Debug.Print("Time for image conversion and stretch: " + sw.Elapsed);
                sw.Restart();



                /* prepare image for structure detection */

                //Noise Reduction and Binarization
                new Median().ApplyInPlace(bmp);
                new OtsuThreshold().ApplyInPlace(bmp);

                
                new CannyEdgeDetector().ApplyInPlace(bmp);
                canceltoken.Token.ThrowIfCancellationRequested();
                new SISThreshold().ApplyInPlace(bmp);
                canceltoken.Token.ThrowIfCancellationRequested();
                new BinaryDilatation3x3().ApplyInPlace(bmp);
                canceltoken.Token.ThrowIfCancellationRequested();
                

                Debug.Print("Time for image preparation: " + sw.Elapsed);
                sw.Restart();

                progress.Report("Detecting structures");

                /* detect structures */
                int minStarSize = 5;
                int maxStarSize = 150;
                BlobCounter blobCounter = new BlobCounter();
                blobCounter.ProcessImage(bmp);

                canceltoken.Token.ThrowIfCancellationRequested();
                Debug.Print("Time for structure detection: " + sw.Elapsed);
                sw.Restart();

                /* get structure info */
                Blob[] blobs = blobCounter.GetObjectsInformation();

                // process each blob

                SimpleShapeChecker checker = new SimpleShapeChecker();
                Star s;
                List<Star> starlist = new List<Star>();


                progress.Report("Analyzing stars");

                foreach (Blob blob in blobs) {
                    canceltoken.Token.ThrowIfCancellationRequested();

                    if (blob.Rectangle.Width > maxStarSize || blob.Rectangle.Height > maxStarSize || blob.Rectangle.Width < minStarSize || blob.Rectangle.Height < minStarSize) {
                        continue;
                    }
                    var points = blobCounter.GetBlobsEdgePoints(blob);
                    AForge.Point centerpoint; float radius;
                    //Star is circle
                    if (checker.IsCircle(points, out centerpoint, out radius)) {
                        s = new Star { Position = centerpoint, radius = radius, Rectangle = blob.Rectangle };
                    }
                    else { //Star is elongated
                        s = new Star { Position = centerpoint, radius = Math.Max(blob.Rectangle.Width, blob.Rectangle.Height) / 2, Rectangle = blob.Rectangle };
                    }
                    /* get pixeldata */
                    for (int x = s.Rectangle.X; x < s.Rectangle.X + s.Rectangle.Width; x++) {
                        for (int y = s.Rectangle.Y; y < s.Rectangle.Y + s.Rectangle.Height; y++) {

                            PixelData pd = new PixelData { PosX = x, PosY = y, value = iarr.FlatArray[x + (iarr.X * y)] };
                            s.Pixeldata.Add(pd);
                        }
                    }
                    s.HFR = CalculateHfr(s, iarr.Mean);
                    starlist.Add(s);

                }
                canceltoken.Token.ThrowIfCancellationRequested();
                progress.Report("Annotating image");
                Bitmap newBitmap = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                Graphics graphics = Graphics.FromImage(newBitmap);

                graphics.DrawImage(orig, 0, 0);


                int r, offset = 10;
                float textposx, textposy;
                var m = (from star in starlist select star.HFR).Average();
                Debug.Print("Mean HFR: " + m);
                var threshhold = 300;
                if (starlist.Count > threshhold) {
                    starlist.Sort((item1, item2) => item2.Average.CompareTo(item1.Average));
                    starlist = starlist.GetRange(0, threshhold);
                }

                foreach (Star star in starlist) {
                    canceltoken.Token.ThrowIfCancellationRequested();
                    r = (int)Math.Ceiling(star.radius);
                    textposx = star.Position.X - offset;
                    textposy = star.Position.Y - offset;
                    //graphics.DrawEllipse(ELLIPSEPEN, new RectangleF(star.Rectangle.X - offset, star.Rectangle.Y - offset, star.Rectangle.Width + 2*offset, star.Rectangle.Height + 2*offset));
                    //graphics.DrawEllipse(ELLIPSEPEN, new RectangleF(star.Rectangle.X, star.Rectangle.Y, star.Rectangle.Width , star.Rectangle.Height ));
                    graphics.DrawEllipse(ELLIPSEPEN, new RectangleF(star.Position.X - OUTERRADIUS, star.Position.Y - OUTERRADIUS, OUTERRADIUS * 2, OUTERRADIUS * 2));
                    //graphics.DrawEllipse(HFRELLIPSEPEN, new RectangleF(star.Position.X - (float)star.HFR, star.Position.Y - (float)star.HFR, (float)star.HFR * 2, (float)star.HFR * 2));
                    graphics.DrawString(star.HFR.ToString("##.##"), FONT, TEXTBRUSH, new PointF(Convert.ToSingle(textposx - 1.5 * offset), Convert.ToSingle(textposy + 2.5 * offset)));
                }

                //newBitmap.UnlockBits(data);


                Debug.Print("Time for retrieving star data: " + sw.Elapsed);
                sw.Restart();

                result = ConvertBitmap(newBitmap);

                Debug.Print("Time to create bitmapsource: " + sw.Elapsed);
                sw.Stop();
                sw = null;
                //result = ConvertBitmap(bmp);

                result.Freeze();
                blobCounter = null;
                orig.Dispose();
                bmp.Dispose();
                newBitmap.Dispose();
                overall.Stop();
                Debug.Print("Overall star detection: " + overall.Elapsed);
                overall = null;

            }
            catch (OperationCanceledException ex) {
                progress.Report("Operation cancelled");
            }

            return result;
        }


        [DllImport("gdi32")]
        static extern int DeleteObject(IntPtr o);

        public static BitmapSource ConvertBitmap(System.Drawing.Bitmap source) {
            IntPtr ip = source.GetHbitmap();
            BitmapSource bs = null;
            try {
                bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip,
                   IntPtr.Zero, Int32Rect.Empty,
                   System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            finally {
                DeleteObject(ip);
            }

            return bs;
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
    }


}
