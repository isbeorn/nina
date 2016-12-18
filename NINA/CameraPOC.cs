using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASCOM.DeviceInterface;
using ASCOM.Utilities;
using ASCOM.DriverAccess;
using System.IO;
using System.Drawing.Imaging;

using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;


namespace NINA {
//    class CameraControllerPOC {
       

//        public Bitmap b;
//        public BitmapSource source;
//        private Array iarr;         // array for the image
//        // bayer offsets
//        int x0;
//        int x1;
//        int x2;
//        int x3;
//        int y0;
//        int y1;
//        int y2;
//        int y3;
//        int[] gamma;
//        int blackLevel;
//        int scale = 1;
//        int stride;




//        private T[] flattenArray<T>(Array arr) {
//            int width = arr.GetLength(0);
//            int height = arr.GetLength(1);
//            T[] flatArray = new T[width * height];
//            T val;
//            int idx = 0;
//            for (int i = 0; i < height; i++)
//            {
//                for (int j = 0; j < width; j++)
//                {
//                    val = (T)Convert.ChangeType(arr.GetValue(j, i), typeof(T));
                    
//                    flatArray[idx] = val;
//                    idx++;
//                }
//            }
//            return flatArray;
//        }


//        public CameraControllerPOC() {
//            string progID;
//            Util U = new Util();
            
//            Console.WriteLine("\r\nCamera:");
//            progID = Camera.Choose("ASCOM.Simulator.Camera");
//            if (progID != "") {
//                Camera C = new Camera(progID);
//                C.Connected = true;
//                Console.WriteLine("  Connected to " + progID);
//                Console.WriteLine("  Description = " + C.Description);
//                Console.WriteLine("  Pixel size = " + C.PixelSizeX + " * " + C.PixelSizeY);
//                Console.WriteLine("  Camera size = " + C.CameraXSize + " * " + C.CameraYSize);
//                Console.WriteLine("  Max Bin = " + C.MaxBinX + " * " + C.MaxBinY);
//                Console.WriteLine("  Bin = " + C.BinX + " * " + C.BinY);
//                Console.WriteLine("  MaxADU = " + C.MaxADU);
//                Console.WriteLine("  CameraState = " + C.CameraState.ToString());
//                Console.WriteLine("  CanAbortExposure = " + C.CanAbortExposure);
//                Console.WriteLine("  CanAsymmetricBin = " + C.CanAsymmetricBin);
//                Console.WriteLine("  CanGetCoolerPower = " + C.CanGetCoolerPower);
//                Console.WriteLine("  CanPulseGuide = " + C.CanPulseGuide);
//                Console.WriteLine("  CanSetCCDTemperature = " + C.CanSetCCDTemperature);
//                Console.WriteLine("  CanStopExposure = " + C.CanStopExposure);
//                //Console.WriteLine("  CCDTemperature = " + C.CCDTemperature);
//                if (C.CanGetCoolerPower)
//                    Console.WriteLine("  CoolerPower = " + C.CoolerPower);
//                Console.WriteLine("  ElectronsPerADU = " + C.ElectronsPerADU);
//                Console.WriteLine("  FullWellCapacity = " + C.FullWellCapacity);
//                Console.WriteLine("  HasShutter = " + C.HasShutter);
                
//                //Console.WriteLine("  HeatSinkTemperature = " + C.HeatSinkTemperature);
//                if (C.CanPulseGuide)
//                    Console.WriteLine("  IsPulseGuiding = " + C.IsPulseGuiding);
//                Console.Write("  Take 15 second image");
                

//                C.StartExposure(0.151, true);
//                while (!C.ImageReady) {
//                    Console.Write(".");
//                    U.WaitForMilliseconds(300);
//                }
//                Console.WriteLine("\r\n  Exposure complete, ready for download.");
//                Console.WriteLine("  CameraState = " + C.CameraState.ToString());
//                //Console.WriteLine("  LastExposureDuration = " + C.LastExposureDuration);
//                //Console.WriteLine("  LastExposureStartTime = " + C.LastExposureStartTime);

//                // int[,] iarr = (int[,])C.ImageArray;
//                //object[,] iarr = (object[,])C.ImageArrayVariant;


//                //  int[] oResult;// = new int[iarr.GetUpperBound(0)];


//                /*
//                for (var r = 0; r < iarr.Rank; r++ ) {
//                    oResult = new int[iarr.GetUpperBound(r)];
//                    for (var i = 0; i < iarr.GetUpperBound(r); i++) {
//                        oResult[i] = iarr[r, i];
//                    }
//                    row[r] = oResult;

//                }*/
//                /*
//                for (var i = 0; i < iarr.GetUpperBound(0); i++) {
//                    oResult[i] = iarr[0, i];
//                }

//                oResult = new int[iarr.GetUpperBound(0)];
//                for (var i = 0; i < iarr.GetUpperBound(1); i++) {
//                    oResult[i] = iarr[1, i];
//                }*/
//                /*
//                Array abc = ArrayUtil.ToJaggedArray(iarr);


//                Array arra = new Array[iarr.GetUpperBound(1) + 1];
//                int[] arrb = new int[iarr.GetUpperBound(0) + 1];

//                for (int i = 0; i <= iarr.GetUpperBound(1); i++) {

//                    for (int j = 0; j <= iarr.GetUpperBound(0); j++) {
//                        arrb[j] = ushort.MinValue + Convert.Toushort(iarr[j, i]);
//                    }
//                    arra.SetValue(arrb, i);

//                }*/

//                /*Array arra = new Array[iarr.GetUpperBound(1)+1];
//                int[] arrb = new int[iarr.GetUpperBound(0)+1];

//                for(int i = 0; i<=iarr.GetUpperBound(1); i++) {

//                    for (int j = 0; j <= iarr.GetUpperBound(0); j++ ) {
//                        arrb[j] = (int)iarr[j, i];
//                    }
//                    arra.SetValue(arrb, i);

//                }*/

//                /* object[] row = new object[iarr.Rank];
//                 for (var i = 0; i < abc.Length; i++) {
//                     row[i] = abc[i];
//                 }*/


//                //AsciiTable a = new AsciiTable();


                             

//                Array camArray = (Array)C.ImageArray;


//                //Array flatArray2 = (Array)nom.tam.util.ArrayFuncs.Flatten(camArray);

//                /*
//                int width = camArray.GetLength(0);
//                int height = camArray.GetLength(1);
//                ushort[] flatArray = new ushort[width * height];
//                ushort val;
//                int idx = 0;
//                for (int i = 0; i < height; i++)
//                {
//                    for (int j = 0; j < width; j++)
//                    {
//                        val = Convert.Toushort(camArray[j, i]);
//                        flatArray[idx] = val;
//                        idx++;
//                    }
//                }*/

//                byte[] flatArray = flattenArray<byte>(camArray);
                            
//                System.Windows.Media.PixelFormat pf = System.Windows.Media.PixelFormats.Gray8;

//                List<System.Windows.Media.Color> colors = new List<System.Windows.Media.Color>();
//                colors.Add(System.Windows.Media.Colors.Gray);
//                BitmapPalette pallet = new BitmapPalette(colors);
//                //int stride = C.CameraYSize * ((Convert.ToString(C.MaxADU, 2)).Length + 7) / 8;
//                int stride = (C.CameraXSize * pf.BitsPerPixel +7) / 8 ;
//                double dpi = 96;

                               
//                BitmapSource bmpSource = BitmapSource.Create(C.CameraXSize, C.CameraYSize, dpi, dpi, pf, null, flatArray, stride);
                
                
//                using (FileStream fs = new FileStream("test.tif", FileMode.Create))
//                {
//                    TiffBitmapEncoder encoder = new TiffBitmapEncoder();                    
//                    encoder.Compression = TiffCompressOption.None;                    
//                    encoder.Frames.Add(BitmapFrame.Create(bmpSource));
//                    encoder.Save(fs);
//                }

//                source = bmpSource;





//                //row[0] = oResult;
//                //row[1] = oResult;
//                //int[][] tttt = new int[1][];
//                //tttt[0] = (int[])nom.tam.util.ArrayFuncs.Flatten(iarr);
//                //a.AddRow(abc);
//                // a.AddColumn(iarr);

//                /*a.AddRow(oResult.ToArray());

//                int[]  oResult2 = new int[iarr.GetUpperBound(1)];
//                    System.Buffer.BlockCopy(iarr, 8, oResult2, 0, 8);
//                    a.AddRow(oResult2);
//                    */
//                //header: http://heasarc.gsfc.nasa.gov/docs/fcg/standard_dict.html 

//                //Fits t = new Fits(@"C:\Users\Isbeorn\Desktop\20-55-04-793.FIT");
//                /*
//                Array imageData = (Array)ArrayFuncs.Flatten(C.ImageArray);
                
//                int[] dims = ArrayFuncs.GetDimensions(C.ImageArray);

//                ushort[] test2 = new ushort[imageData.Length];
//                ushort val2;
//                for (int i = 0; i < imageData.Length; i++)
//                {
//                    val2 = Convert.Toushort(imageData.GetValue(i));
//                    test2.SetValue(val2, i);
//                }

//                BasicHDU imageHdu = FitsFactory.HDUFactory(ArrayFuncs.Curl(test2, dims));
//                //BasicHDU imageHdu = FitsFactory.HDUFactory(t.GetHDU(0).Data.DataArray);
//                const double bZero = 0;
//                const double bScale = 1.0;
//                imageHdu.Header.InsertCard(new HeaderCard("SIMPLE", true, ""), 0);
//                //imageHdu.AddValue("EXTEND", true, "");
//                //imageHdu.AddValue("SIMPLE", true, "");
//               // imageHdu.AddValue("BITPIX", 16, "");
//                imageHdu.AddValue("BZERO", bZero, "");
//                imageHdu.AddValue("BSCALE", bScale, "");
//                imageHdu.AddValue("DATAMIN", 0.0, "");      // should this reflect the actual data values
//                imageHdu.AddValue("DATAMAX", C.MaxADU, "pixel values above this level are considered saturated.");
//                imageHdu.AddValue("INSTRUME", C.Description, "");
//                //imageHdu.AddValue("EXPTIME", oCamera.LastExposureDuration, "duration of exposure in seconds.");
//                imageHdu.AddValue("DATE-OBS", C.LastExposureStartTime, "");
//                imageHdu.AddValue("XPIXSZ", C.PixelSizeX * C.BinX, "physical X dimension of the sensor's pixels in microns"); //  (present only if the information is provided by the camera driver). Includes binning.
//                imageHdu.AddValue("YPIXSZ", C.PixelSizeY * C.BinY, "physical Y dimension of the sensor's pixels in microns"); //  (present only if the information is provided by the camera driver). Includes binning.
//                imageHdu.AddValue("XBINNING", C.BinX, "");
//                imageHdu.AddValue("YBINNING", C.BinY, "");
//                imageHdu.AddValue("XORGSUBF", C.StartX, "subframe origin on X axis in binned pixels");
//                imageHdu.AddValue("YORGSUBF", C.StartY, "subframe origin on Y axis in binned pixels");
//                //imageHdu.AddValue("XPOSSUBF", oCamera.StartX, "");
//                //imageHdu.AddValue("YPOSSUBF", oCamera.StartY, "");
//                //imageHdu.AddValue("CBLACK", (double)imageControl.Minimum, "");
//                //imageHdu.AddValue("CWHITE", (double)imageControl.Maximum, "");
               
//                imageHdu.AddValue("SWCREATE", "ASCOM Camera Test", "string indicating the software used to create the file");

//                //imageHdu.AddValue("COLORSPC", "Grayscale", "");
//                imageHdu.AddValue("END", null,null);



//                Fits f = new Fits();
//                f.AddHDU(imageHdu);
//                FileStream fs2 = File.Create("test.fit");
//                f.Write(fs2);
//                fs2.Close();
                
//                */
//                /*
//                ImageHDU hdu = (ImageHDU)Fits.MakeHDU(ArrayFuncs.Curl(imageData, dims));
//                //HeaderCard c = new HeaderCard()
//                //c.Key = "abc";
//               // hdu.Header.DeleteKey("BITPIX");
//                //hdu.Header.DeleteKey("NAXIS");
//                hdu.Header.AddCard(new HeaderCard("AUTHOR", "Stefan", ""));
//                //hdu.Header.AddCard(new HeaderCard("BITPIX", Convert.ToString(C.MaxADU, 2).Length, ""));
//                //hdu.Header.AddCard(new HeaderCard("BITPIX", 16, ""));
//                hdu.Header.AddCard(new HeaderCard("DATE", "2016-05-21", ""));
//                //hdu.Header.AddCard(new HeaderCard("NAXIS", 2, ""));
//                hdu.Header.AddCard(new HeaderCard("OBJECT", "M42", ""));
//                hdu.Header.AddCard(new HeaderCard("TELESCOP", "SIMULATOR", ""));
//                hdu.Header.AddCard(new HeaderCard("BSCALE", C.ElectronsPerADU, ""));
//                //hdu.Header.AddCard(new HeaderCard("BSCALE", 1, ""));
//                hdu.Header.AddCard(new HeaderCard("COLORSPC", "Grayscale", ""));                 //Grayscale
//                //hdu.Header.AddCard(new HeaderCard("BZERO", Convert.ToInt64(Convert.ToString(C.MaxADU, 2), 2) + 1, ""));
//               // hdu.Header.AddCard(new HeaderCard("BZERO", Convert.ToInt64(Convert.ToString(int.MaxValue, 2), 2) + 1, ""));
                			

//                hdu.Header.AddCard(new HeaderCard("BZERO", 0, ""));
//                //hdu.Header.AddCard(new HeaderCard("BZERO", 0, ""));
//                */







//                /* AsciiTableHDU bla = (AsciiTableHDU)Fits.MakeHDU(a);



//                 nom.tam.fits.ImageData img = new ImageData(abc);

//                 ImageHDU hdu = (ImageHDU)Fits.MakeHDU(img);


//                 Fits f = new Fits();
//                 f.AddHDU(hdu);

//                 FileStream fs = File.Create("test.fits");
//                 f.Write(fs);*/

//                //Console.WriteLine("  Array is " + (imgArray.GetUpperBound(0) + 1) + " by " + (imgArray.GetUpperBound(1) + 1));


//                //b = ToBitmap(imgArray);

//                /*
//                unsafe {


//                    // generate gamma LUT
//                    gamma = new int[256];
//                    var g = 255;
//                    for (int i = 0; i < 256; i++) {
//                        gamma[i] = (byte)(Math.Pow(i / 256.0, g) * 256.0);
//                    }
                    

                

//                    DisplayProcess displayProcess = MonochromeProcess;
//                    int width = iarr.GetLength(0);
//                    int height = iarr.GetLength(1);
//                    int stepH = 1;
//                    int stepW = 1;
//                    int stepX = 1;
//                    int stepY = 1;


//                    switch (C.SensorType) {
//                        case ASCOM.DeviceInterface.SensorType.Monochrome:
//                            x0 = 0;
//                            y0 = 0;
//                            break;
//                        case ASCOM.DeviceInterface.SensorType.RGGB:
//                            displayProcess = RggbProcess;
//                            stepX = 2;
//                            stepY = 2;
//                            SetBayerOffsets(2, 2, C, C.BayerOffsetX, C.BayerOffsetY);
//                            break;
//                        case ASCOM.DeviceInterface.SensorType.CMYG:
//                            displayProcess = CmygProcess;
//                            stepX = 2;
//                            stepY = 2;
//                            SetBayerOffsets(2, 2, C, C.BayerOffsetX, C.BayerOffsetY);
//                            break;
//                        case ASCOM.DeviceInterface.SensorType.LRGB:
//                            displayProcess = LrgbProcess;
//                            x0 = (C.BayerOffsetX + C.StartX * C.BinX) & (stepX - 1);
//                            y0 = (C.BayerOffsetY + C.StartY * C.BinY) & (stepY - 1);
//                            stepX = 4;
//                            stepY = 4;
//                            stepH = 2;
//                            stepW = 2;
//                            SetBayerOffsets(4, 4, C, C.BayerOffsetX, C.BayerOffsetY);
//                            break;
//                        case ASCOM.DeviceInterface.SensorType.CMYG2:
//                            displayProcess = Cmyg2Process;
//                            stepX = 2;
//                            stepY = 4;
//                            stepH = 2;
//                            SetBayerOffsets(2, 4, C, C.BayerOffsetX, C.BayerOffsetY);
//                            break;
//                        case ASCOM.DeviceInterface.SensorType.Color:
//                            displayProcess = ColourProcess;
//                            break;
//                    }
//                    width /= (stepX / stepW);
//                    height /= (stepY / stepH);
              

//                b = new Bitmap(width, height, PixelFormat.Format24bppRgb);
//                BitmapData data = b.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
//                try {
//                    // pointer to locked bitmap data
//                    var imgPtr = (byte*)(data.Scan0);
//                    // black level
//                    //blackLevel = (int)imageControl.MinValue;
//                    // scale, white-black
//                    //scale = (int)imageControl.MaxValue - blackLevel;
//                    stride = data.Stride;

//                    int yy = 0;
//                    for (int y = 0; y < height; y += stepH) {
//                        int xx = 0;
//                        for (int x = 0; x < width; x += stepW) {
//                            displayProcess(xx, yy, imgPtr);
//                            xx += stepX;
//                            imgPtr += (3 * stepW);
//                        }
//                        imgPtr += data.Stride - data.Width * 3 + (stepH - 1) * data.Stride;
//                        yy += stepY;
//                    }
//                } finally {
//                    b.UnlockBits(data);
//                }
//                }*/








//                /*
//                int width = iarr.GetUpperBound(1);
//                int height = iarr.GetUpperBound(0);

//                System.Windows.Media.PixelFormat pf = new System.Windows.Media.PixelFormat();
//                System.Windows.Media.
                
//                int rawStride = (width * pf.BitsPerPixel + 7) / 8;
                
//               // BitmapSource.Create(width,height, 9, 9, pf, null, arra, stride);

//                BitmapSource bild = BitmapSource.Create(width, height, C.PixelSizeX, C.PixelSizeY, pf, null, iarr, rawStride);
//                */

//                C.Connected = false;
//                C.Dispose();
//            }
//        }

        

//        unsafe private void SetBayerOffsets(int stepX, int stepY, Camera C, int bayerOffsetX, int bayerOffsetY) {
//            // set the bayer offsets
//            x0 = (bayerOffsetX + C.StartX * C.BinX) & (stepX - 1);
//            y0 = (bayerOffsetY + C.StartY * C.BinY) & (stepY - 1);
//            x1 = (x0 + 1) & (stepX - 1);
//            x2 = (x0 + 2) & (stepX - 1);
//            x3 = (x0 + 3) & (stepX - 1);
//            y1 = (y0 + 1) & (stepY - 1);
//            y2 = (y0 + 2) & (stepY - 1);
//            y3 = (y0 + 3) & (stepY - 1);
//        }

//        // use delegates to select display process
//        private unsafe delegate void DisplayProcess(int x, int y, byte* imgPtr);
//        // these processes take one cell of the image and generate the rgb values from the contents of the cell
//        // then use loadRGB to put the RGB values in the image

//        private unsafe void MonochromeProcess(int x, int y, byte* imgPtr)
//        {
//            int k = Convert.ToInt32(iarr.GetValue(x, y), CultureInfo.InvariantCulture);
//            LoadRgb(k, k, k, imgPtr);
//        }

//        private unsafe void RggbProcess(int x, int y, byte* imgPtr)
//        {
//            int r = Convert.ToInt32(iarr.GetValue(x + x0, y + y0), CultureInfo.InvariantCulture);
//            int g = Convert.ToInt32(iarr.GetValue(x + x0, y + y1), CultureInfo.InvariantCulture);
//            int b = Convert.ToInt32(iarr.GetValue(x + x1, y + y1), CultureInfo.InvariantCulture);
//            g += Convert.ToInt32(iarr.GetValue(x + x1, y + y0), CultureInfo.InvariantCulture);
//            g /= 2;
//            LoadRgb(r, g, b, imgPtr);
//        }

//        private unsafe void CmygProcess(int x, int h, byte* imgPtr)
//        {
//            // get the cmyg values
//            int y = Convert.ToInt32(iarr.GetValue(x + x0, h + y0), CultureInfo.InvariantCulture);
//            int c = Convert.ToInt32(iarr.GetValue(x + x1, h + y0), CultureInfo.InvariantCulture);
//            int g = Convert.ToInt32(iarr.GetValue(x + x0, h + y1), CultureInfo.InvariantCulture);
//            int m = Convert.ToInt32(iarr.GetValue(x + x1, h + y1), CultureInfo.InvariantCulture);
//            // convert to rgb, c = g + b, y = r + g, m = r + b
//            int r = y + m - c;
//            int b = c + m - y;
//            g += (c + y - m);
//            LoadRgb(r, g/2, b, imgPtr);
//        }

//        private unsafe void Cmyg2Process(int x, int h, byte* imgPtr)
//        {
//            // get the cmyg values for the top pixel
//            int g = Convert.ToInt32(iarr.GetValue(x + x0, h + y0), CultureInfo.InvariantCulture);
//            int m = Convert.ToInt32(iarr.GetValue(x + x1, h + y0), CultureInfo.InvariantCulture);
//            int c = Convert.ToInt32(iarr.GetValue(x + x0, h + y1), CultureInfo.InvariantCulture);
//            int y = Convert.ToInt32(iarr.GetValue(x + x1, h + y1), CultureInfo.InvariantCulture);
//            // convert to rgb, c = g + b, y = r + g, m = r + b
//            int r = y + m - c;
//            int b = c + m - y;
//            g += (c + y - m);
//            LoadRgb(r, g/2, b, imgPtr);
//            // and the bottom pixel
//            m = Convert.ToInt32(iarr.GetValue(x + x0, h + y2), CultureInfo.InvariantCulture);
//            g = Convert.ToInt32(iarr.GetValue(x + x1, h + y2), CultureInfo.InvariantCulture);
//            c = Convert.ToInt32(iarr.GetValue(x + x0, h + y3), CultureInfo.InvariantCulture);
//            y = Convert.ToInt32(iarr.GetValue(x + x1, h + y3), CultureInfo.InvariantCulture);
//            // convert to rgb, c = g + b, y = r + g, m = r + b
//            r = y + m - c;
//            b = c + m - y;
//            g += (c + y - m);
//            LoadRgb(r, g/2, b, imgPtr + stride);
//        }

//        private unsafe void LrgbProcess(int x, int y, byte* imgPtr)
//        {
//            // convert a 4 x 4 grid of input pixels to a 2 x2 grid of output pixels
//            // get the lrgb values
//            int l = Convert.ToInt32(iarr.GetValue(x + x0, y + y0), CultureInfo.InvariantCulture);
//            l += Convert.ToInt32(iarr.GetValue(x + x1, y + y1), CultureInfo.InvariantCulture);
//            int r = Convert.ToInt32(iarr.GetValue(x + x1, y + y0), CultureInfo.InvariantCulture);
//            r += Convert.ToInt32(iarr.GetValue(x + x0, y + y1), CultureInfo.InvariantCulture);
//            int g = Convert.ToInt32(iarr.GetValue(x + x0, y + y3), CultureInfo.InvariantCulture);
//            g += Convert.ToInt32(iarr.GetValue(x + x2, y + y1), CultureInfo.InvariantCulture);
//            int b = l - r - g;
//            LoadRgb(r/2, g/2, b/2, imgPtr);     // top left
//            l = Convert.ToInt32(iarr.GetValue(x + x2, y + y0), CultureInfo.InvariantCulture);
//            l += Convert.ToInt32(iarr.GetValue(x + x3, y + y1), CultureInfo.InvariantCulture);
//            b = l - r - g;
//            LoadRgb(r/2, g/2, b/2, imgPtr+3);     // top right
//            l = Convert.ToInt32(iarr.GetValue(x + x0, y + y2), CultureInfo.InvariantCulture);
//            l += Convert.ToInt32(iarr.GetValue(x + x1, y + y3), CultureInfo.InvariantCulture);
//            g = Convert.ToInt32(iarr.GetValue(x + x1, y + y2), CultureInfo.InvariantCulture);
//            g += Convert.ToInt32(iarr.GetValue(x + x0, y + y3), CultureInfo.InvariantCulture);
//            b = Convert.ToInt32(iarr.GetValue(x + x3, y + y2), CultureInfo.InvariantCulture);
//            b += Convert.ToInt32(iarr.GetValue(x + x2, y + y3), CultureInfo.InvariantCulture);
//            r = l - g - b;
//            LoadRgb(r/2, g/2, b/2, imgPtr+stride);     // bottom left
//            l = Convert.ToInt32(iarr.GetValue(x + x2, y + y2), CultureInfo.InvariantCulture);
//            l += Convert.ToInt32(iarr.GetValue(x + x3, y + y3), CultureInfo.InvariantCulture);
//            r = l - b - g;
//            LoadRgb(r/2, g/2, b/2, imgPtr+stride+3);     // bottom right
//        }

//        private unsafe void ColourProcess(int w, int h, byte* imgPtr)
//        {
//            // get the rgb values from the three image planes
//            int r = Convert.ToInt32(iarr.GetValue(w, h, 0), CultureInfo.InvariantCulture);
//            int g = Convert.ToInt32(iarr.GetValue(w, h, 1), CultureInfo.InvariantCulture);
//            int b = Convert.ToInt32(iarr.GetValue(w, h, 2), CultureInfo.InvariantCulture);
//            LoadRgb(r, g, b, imgPtr);
//        }

//        private unsafe void LoadRgb(int r, int g, int b, byte *imgPtr)
//        {
//            // convert 16 bit signed to 16 bit unsigned
//            if (r < 0) r += 65535;
//            if (g < 0) g += 65535;
//            if (b < 0) b += 65535;
//            // scale to range 0 to scale
//            r = r - blackLevel;
//            g = g - blackLevel;
//            b = b - blackLevel;
//            // scale to 0 to 255
//            r = (int)(r * 255.0 / scale);
//            g = (int)(g * 255.0 / scale);
//            b = (int)(b * 255.0 / scale);
//            // truncate to byte range, apply gamma and put into the image
//            *imgPtr = (byte) gamma[Math.Min(Math.Max(b, 0), 255)];
//            imgPtr++;
//            *imgPtr = (byte) gamma[Math.Min(Math.Max(g, 0), 255)];
//            imgPtr++;
//            *imgPtr = (byte) gamma[Math.Min(Math.Max(r, 0), 255)];
//        }

//       private unsafe Bitmap ToBitmap(int[,] rawImage) {
//            int width = rawImage.GetLength(1);
//            int height = rawImage.GetLength(0);

//            Bitmap Image = new Bitmap(width, height);            
//            BitmapData bitmapData = Image.LockBits(
//                new Rectangle(0, 0, width, height),
//                ImageLockMode.ReadWrite,
//                PixelFormat.Format32bppArgb
//                //Image.PixelFormat
//            );
//            ColorARGB* startingPosition = (ColorARGB*)bitmapData.Scan0;


//            for (int i = 0; i < height; i++)
//                for (int j = 0; j < width; j++) {
//                    double color = rawImage[i, j];
//                    byte rgb = (byte)(color *255 );

//                    ColorARGB* position = startingPosition + j + i * width;
//                    position->A = 255;
//                    position->R = rgb;
//                    position->G = rgb;
//                    position->B = rgb;
//                }

//            Image.UnlockBits(bitmapData);
//            return Image;
//        }

//        public struct ColorARGB {
//            public byte B;
//            public byte G;
//            public byte R;
//            public byte A;

//            /*public ColorARGB(Color color) {
               
//                A = color.A;
//                R = color.R;
//                G = color.G;
//                B = color.B;
//            }

//            public ColorARGB(byte a, byte r, byte g, byte b) {
//                A = a;
//                R = r;
//                G = g;
//                B = b;
//            }

//            public Color ToColor() {
//                return Color.FromArgb(A, R, G, B);
//            }*/
//        }


//    }





//}





//static class ArrayUtil {
//    internal static T[][] ToJaggedArray<T>(this T[,] twoDimensionalArray) {
//        int rowsFirstIndex = twoDimensionalArray.GetLowerBound(1);
//        int rowsLastIndex = twoDimensionalArray.GetUpperBound(1);
//        int numberOfRows = rowsLastIndex + 1;

//        int columnsFirstIndex = twoDimensionalArray.GetLowerBound(0);
//        int columnsLastIndex = twoDimensionalArray.GetUpperBound(0);
//        int numberOfColumns = columnsLastIndex + 1;

//        T[][] jaggedArray = new T[numberOfRows][];
//        for (int i = rowsFirstIndex; i <= rowsLastIndex; i++) {
//            jaggedArray[i] = new T[numberOfColumns];

//            for (int j = columnsFirstIndex; j <= columnsLastIndex; j++) {
//                jaggedArray[i][j] = twoDimensionalArray[j, i];
//            }
//        }
//        return jaggedArray;
//    }

}