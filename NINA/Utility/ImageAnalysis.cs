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

        private static System.Drawing.Pen ELLIPSEPEN = new System.Drawing.Pen(System.Drawing.Brushes.LightYellow, 3);
        private static SolidBrush TEXTBRUSH = new SolidBrush(System.Drawing.Color.Yellow);
        private static System.Drawing.FontFamily FONTFAMILY = new System.Drawing.FontFamily("Times New Roman");
        private static Font FONT = new Font(FONTFAMILY, 32, System.Drawing.FontStyle.Regular, GraphicsUnit.Pixel);


        public ImageAnalysis() {

        }

        class Star {
            public double radius;
            public double HFR;
            public double FWHM;
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





        double calculateHfr(Star s) {
            double hfr = 0.0d;
            double outerRadius = s.radius;
            double sum = 0, sumDist = 0;

            int centerX = (int)Math.Floor(s.Position.X);
            int centerY = (int)Math.Floor(s.Position.Y);

            foreach(PixelData data in s.Pixeldata) {
                if (insideCircle(data.PosX, data.PosY, s.Position.X, s.Position.Y, s.radius)) {
                    sum += data.value;
                    sumDist += data.value * Math.Sqrt(Math.Pow((double)data.PosX - (double)centerX, 2.0d) + Math.Pow((double)data.PosY - (double)centerY, 2.0d));
                }
            }

            if(sum > 0) {
                hfr = sumDist / sum;
            } else {
                hfr = Math.Sqrt(2) * outerRadius;
            }
            

            return hfr;
        }

        bool insideCircle(double x, double y, double centerX, double centerY, double radius) {
            return (Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2) <= Math.Pow(radius, 2));
        }

        /*
             bool insideCircle(float inX, float inY , float inCenterX, float inCenterY, float inRadius) {
            return (pow(inX - inCenterX, 2.0) + pow(inY - inCenterY, 2.0) <= pow(inRadius, 2.0));
            }
         */

        /*
        float calcHfd(const CImg<float> & inImage, unsigned int inOuterDiameter) {
              // Sum up all pixel values in whole circle
              float outerRadius = inOuterDiameter / 2;
                    float sum = 0, sumDist = 0;
                    int centerX = ceil(inImage.width() / 2.0);
                    int centerY = ceil(inImage.height() / 2.0);


              cimg_forXY(inImage, x, y) {
                        if (insideCircle(x, y, centerX, centerY, outerRadius)) {
                            sum += inImage(x, y);
                            sumDist += inImage(x, y) * sqrt(pow((float)x - (float)centerX, 2.0f) + pow((float)y - (float)centerY, 2.0f));
                        }
                    }
              // NOTE: Multiplying with 2 is required since actually just the HFR is calculated above
              return (sum? 2.0 * sumDist / sum : sqrt(2.0) * outerRadius);
            }
            */

    public async Task<BitmapSource> detectStarsAsync(Utility.ImageArray iarr, IProgress<string> progress, CancellationTokenSource canceltoken) {
            return await Task.Run<BitmapSource>(() => detectStars(iarr, progress, canceltoken));
        }
    public BitmapSource detectStars(Utility.ImageArray iarr, IProgress<string> progress, CancellationTokenSource canceltoken) {
            BitmapSource result = null;
            try {
                
                progress.Report("Preparing image for star detection");
                var bmpsource = NormalizeTiffTo8BitImage(ViewModel.ImagingVM.stretch(iarr).Result);

                Stopwatch sw = Stopwatch.StartNew();
                Bitmap orig = BitmapFromSource(bmpsource);
                Bitmap bmp = BitmapFromSource(bmpsource);
                var a = new AForge.Imaging.ImageStatistics(bmp);

                Debug.Print("Time to convert Image: " + sw.Elapsed);
                sw.Restart();
                canceltoken.Token.ThrowIfCancellationRequested();

                

                /* stretch image*/
                /*IntRange inputRange = new IntRange(a.GrayWithoutBlack.Median - (int)(a.GrayWithoutBlack.StdDev * 0.5), a.GrayWithoutBlack.Median + (int)(a.GrayWithoutBlack.StdDev * 1.5));
                IntRange outputRange = new IntRange(0, byte.MaxValue);
                new LevelsLinear { InGray = inputRange, OutGray = outputRange }.ApplyInPlace(bmp);*/

                new BinaryErosion3x3().ApplyInPlace(bmp);
                new Mean().ApplyInPlace(bmp);

                Debug.Print("Time for stretch: " + sw.Elapsed);
                sw.Restart();

                /* prepare image for structure detection */
                var edgedetector = new CannyEdgeDetector();
                edgedetector.ApplyInPlace(bmp);
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
                int maxStarSize = minStarSize * 5;
                BlobCounter blobCounter = new BlobCounter{ MinWidth = minStarSize, MinHeight = maxStarSize, MaxWidth = maxStarSize, MaxHeight = maxStarSize };
                blobCounter.ProcessImage(bmp);

                canceltoken.Token.ThrowIfCancellationRequested();
                Debug.Print("Time for structure detection: " + sw.Elapsed);
                sw.Restart();

                /* get structure info */
                Blob[] blobs = blobCounter.GetObjectsInformation();
            
                // create convex hull searching algorithm
                GrahamConvexHull hullFinder = new GrahamConvexHull();

                // lock image to draw on it
            
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
                     if(checker.IsCircle(points, out centerpoint, out radius)) {
                        s = new Star { Position = centerpoint, radius = radius, Rectangle = blob.Rectangle };
                    } else { //Star is elongated
                        s = new Star { Position = centerpoint, radius = Math.Max(blob.Rectangle.Width, blob.Rectangle.Height) / 2, Rectangle = blob.Rectangle };
                    }
                     /* get pixeldata */
                     for(int x = s.Rectangle.X; x < s.Rectangle.X + s.Rectangle.Width; x++) {
                        for(int y = s.Rectangle.Y; y < s.Rectangle.Y  + s.Rectangle.Height; y++) {

                            PixelData pd = new PixelData { PosX = x, PosY = y, value = iarr.FlatArray[x + (iarr.X * y)] } ;
                            s.Pixeldata.Add(pd);
                        }
                    }
                    s.HFR = calculateHfr(s);
                    starlist.Add(s);
                
                }
                canceltoken.Token.ThrowIfCancellationRequested();
                progress.Report("Annotating image");
                Bitmap newBitmap = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                Graphics graphics = Graphics.FromImage(newBitmap);
                
                graphics.DrawImage(orig, 0, 0);

                
                

                /*BitmapData data = newBitmap.LockBits(
                    new Rectangle(0, 0, newBitmap.Width, newBitmap.Height),
                        ImageLockMode.ReadWrite, newBitmap.PixelFormat);*/
                int r, posx, posy, offset = 10;

                var threshhold = 100;
                if(starlist.Count > threshhold) {
                    starlist.Sort((item1, item2) => item2.Average.CompareTo(item1.Average));
                    starlist = starlist.GetRange(0, threshhold);
                }

                foreach (Star star in starlist) {
                    canceltoken.Token.ThrowIfCancellationRequested();
                    r = (int)Math.Ceiling(star.radius);
                    posx = star.Rectangle.X - offset;
                    posy = star.Rectangle.Y - offset;
                    graphics.DrawEllipse(ELLIPSEPEN, new RectangleF(star.Rectangle.X - offset, star.Rectangle.Y - offset, star.Rectangle.Width + 2*offset, star.Rectangle.Height + 2*offset));
                    graphics.DrawString(star.HFR.ToString("##.##"), FONT, TEXTBRUSH, new PointF(Convert.ToSingle(posx - 1.5*offset), Convert.ToSingle(posy + 2.5*offset)));
                }
            
                //newBitmap.UnlockBits(data);


                Debug.Print("Time for retrieving star data: " + sw.Elapsed);
                sw.Restart();

                result = ConvertBitmap(newBitmap);
                //BitmapSource result = ConvertBitmap(bmp);
            
                result.Freeze();

                orig.Dispose();
                bmp.Dispose();
                newBitmap.Dispose();

            }
            catch (OperationCanceledException ex) {
                progress.Report("Operation cancelled");
            }

            return result;
            //Bitmap bmp = BitmapFromSource(bmpsource);

            //sw.Stop();
            //Debug.Print("Convert Bitmap From Source: " + sw.ElapsedMilliseconds.ToString());
            //sw = Stopwatch.StartNew();

            //Image<Gray, ushort> img = new Image<Gray, ushort>(bmp);

            //Load the image from file and resize it for display
            //Image <Bgr, ushort> img =
            //new Image<Bgr, ushort>(bmp);
            //.Resize(1920, 1080, Emgu.CV.CvEnum.Inter.Linear, true)
            //.ConvertScale<byte>(0, 0);

            // sw.Stop();
            //Debug.Print("Convert Bitmap to OpenCVImage: " + sw.ElapsedMilliseconds.ToString());
            //sw = Stopwatch.StartNew();


            //Mat m = ToMat(bmpsource);

            //sw.Stop();
            //Debug.Print("Convert to mat: " + sw.ElapsedMilliseconds.ToString());
            //sw = Stopwatch.StartNew();



            //Image<Gray, ushort> img = m.ToImage<Gray, ushort>();

            //sw.Stop();
            //Debug.Print("Convert mat to OpenCVImage: " + sw.ElapsedMilliseconds.ToString());
            //sw = Stopwatch.StartNew();

            ///*Matrix<float> kernel = new Matrix<float>(
            //new float[,] {
            //        {1.0f, 2.0f, 1.0f},
            //        {2.0f, 4.0f, 2.0f},
            //        {1.0f, 2.0f, 1.0f}
            //    }
            //);*/

            ////Image<Gray, ushort> tmp2 = img.MorphologyEx(Emgu.CV.CvEnum.MorphOp.Close, kernel, new System.Drawing.Point(-1, -1), 1, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar());
            ///*try {
            //    img._EqualizeHist();
            //} catch (Exception ex) {

            //}*/



            //var sd = new Emgu.CV.XFeatures2D.StarDetector();

            //var points = sd.Detect(m);



            //sw.Stop();
            //Debug.Print("Time to determine Stars: " + sw.ElapsedMilliseconds.ToString());
            //sw = Stopwatch.StartNew();

            //Image<Gray, ushort> circleImage = img.Copy();
            ///*foreach (MKeyPoint p in points) {
            //    CircleF c = new CircleF(p.Point, 10);
            //    circleImage.Draw(c, new Emgu.CV.Structure.Gray(ushort.MaxValue), 5);
            //}*/

            //sw.Stop();
            //Debug.Print("Time to copy img and draw circles: " + sw.ElapsedMilliseconds.ToString());


            //return ToBitmapSource(circleImage);

        }

        public static BitmapSource ConvertBitmap(Bitmap source) {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                          source.GetHbitmap(),
                          IntPtr.Zero,
                          System.Windows.Int32Rect.Empty,
                          BitmapSizeOptions.FromEmptyOptions());
        }

        public static Bitmap BitmapFromSource(BitmapSource bitmapsource) {
            Bitmap bitmap;
            using (var outStream = new MemoryStream()) {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream);
                bitmap = new Bitmap(outStream);
            }
            return bitmap;
        }



        public static BitmapSource NormalizeTiffTo8BitImage(BitmapSource source) {
            // allocate buffer & copy image bytes.
            var rawStride = source.PixelWidth * source.Format.BitsPerPixel / 8;
            var rawImage = new byte[rawStride * source.PixelHeight];
            source.CopyPixels(rawImage, rawStride, 0);

            // get both max values of first & second byte of pixel as scaling bounds.
            var max1 = 0;
            int max2 = 1;
            for (int i = 0; i < rawImage.Length; i++) {
                if ((i & 1) == 0) {
                    if (rawImage[i] > max1)
                        max1 = rawImage[i];
                }
                else if (rawImage[i] > max2)
                    max2 = rawImage[i];
            }

            // determine normalization factors.
            var normFactor = max2 == 0 ? 0.0d : 128.0d / max2;
            var factor = max1 > 0 ? 255.0d / max1 : 0.0d;
            max2 = Math.Max(max2, 1);

            // normalize each pixel to output buffer.
            var buffer8Bit = new byte[rawImage.Length / 2];
            for (int src = 0, dst = 0; src < rawImage.Length; dst++) {
                int value16 = rawImage[src++];
                double value8 = ((value16 * factor) / max2) - normFactor;

                if (rawImage[src] > 0) {
                    int b = rawImage[src] << 8;
                    value8 = ((value16 + b) / max2) - normFactor;
                }
                buffer8Bit[dst] = (byte)Math.Min(255, Math.Max(value8, 0));
                src++;
            }

            // return new bitmap source.
            return BitmapSource.Create(
                source.PixelWidth, source.PixelHeight,
                source.DpiX, source.DpiY,
                PixelFormats.Gray8, BitmapPalettes.Gray256,
                buffer8Bit, rawStride / 2);
        }

        //public static BitmapSource ToBitmapSource(IImage image) {
        //    using (System.Drawing.Bitmap source = image.Bitmap) {
        //        IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap

        //        BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
        //        ptr,
        //        IntPtr.Zero,
        //        Int32Rect.Empty,
        //        System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

        //        DeleteObject(ptr); //release the HBitmap
        //        return bs;
        //    }
        //}

        //public static Mat ToMat(BitmapSource source) {

        //    if (source.Format == PixelFormats.Bgra32) {
        //        Mat result = new Mat();
        //        result.Create(source.PixelHeight, source.PixelWidth, DepthType.Cv8U, 4);
        //        source.CopyPixels(Int32Rect.Empty, result.DataPointer, result.Step * result.Rows, result.Step);
        //        return result;
        //    }
        //    else if (source.Format == PixelFormats.Bgr24) {
        //        Mat result = new Mat();
        //        result.Create(source.PixelHeight, source.PixelWidth, DepthType.Cv8U, 3);
        //        source.CopyPixels(Int32Rect.Empty, result.DataPointer, result.Step * result.Rows, result.Step);
        //        return result;
        //    }
        //    else if (source.Format == PixelFormats.Gray16) {
        //        Mat result = new Mat();
        //        result.Create(source.PixelHeight, source.PixelWidth, DepthType.Cv16U, 1);
        //        source.CopyPixels(Int32Rect.Empty, result.DataPointer, result.Step * result.Rows, result.Step);
        //        return result;
        //    }

        //    else {
        //        throw new Exception(String.Format("Convertion from BitmapSource of format {0} is not supported.", source.Format));
        //    }
        //}

        ///// <summary>
        ///// Delete a GDI object
        ///// </summary>
        ///// <param name="o">The poniter to the GDI object to be deleted</param>
        ///// <returns></returns>
        //[DllImport("gdi32")]
        //private static extern int DeleteObject(IntPtr o);
    }

    
}
