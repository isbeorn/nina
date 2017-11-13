using nom.tam.fits;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Utility {
    //Way too slow to use reflection and dynamic types

    //public class ImageArray<T> {
    //    private const double HISTOGRAMRESOLUTION = 1000;

    //            public Array SourceArray { get; set; }
    //    public T[] FlatArray { get; set; }
    //    public int X { get; set; }
    //    public int Y { get; set; }
    //    public double StDev { get; set; }
    //    public double Mean { get; set; }
    //    public T MinStDev { get; set; }
    //    public T MaxStDev { get; set; }
    //    public SortedDictionary<T, int> Histogram { get; set; }

    //    private ImageArray() {}

    //    public static ImageArray<T> createInstance(Array input) {
    //        Int32[,] arr = (Int32[,])input;
    //        ImageArray<T> iarr = new ImageArray<T>();
    //        iarr.SourceArray = arr;
    //        int width = arr.GetLength(0);
    //        int height = arr.GetLength(1);
    //        //iarr.Y = width;
    //        //iarr.X = height;
    //        iarr.X = width;
    //        iarr.Y = height;
    //        dynamic[] flatArray = new dynamic[arr.Length];
    //        double histogramtmpkey;
    //        dynamic value, histogramkey;

    //        FieldInfo maxValueField = typeof(T).GetField("MaxValue", BindingFlags.Public | BindingFlags.Static);
    //        if (maxValueField == null)
    //            throw new NotSupportedException(typeof(T).Name);
    //        dynamic maxValue = (T)maxValueField.GetValue(null);

    //        SortedDictionary<T, int> histogram = new SortedDictionary<T, int>();
    //        unsafe
    //        {
    //            fixed (Int32* ptr = arr) {
    //                int idx = 0, row = 0;
    //                for (int i = 0; i < arr.Length; i++) {
    //                    value = ptr[i];
    //                    if(ptr[i] > maxValue) {
    //                        value = maxValue;
    //                    }
    //                    value = (T)Convert.ChangeType(value, typeof(T));




    //                    idx = ((i % height) * width) + row;
    //                    if ((i % (height)) == (height - 1)) row++;

    //                    histogramtmpkey = (HISTOGRAMRESOLUTION / maxValue) * value;
    //                    if(histogramtmpkey > maxValue) {
    //                        histogramtmpkey = maxValue;
    //                    }
    //                    histogramkey = (T)Convert.ChangeType(Math.Round(histogramtmpkey), typeof(T));
    //                    if (histogram.ContainsKey(histogramkey)) {
    //                        histogram[histogramkey] += 1;
    //                    }
    //                    else {
    //                        histogram.Add(histogramkey, 1);
    //                    }

    //                    T b = value;
    //                    flatArray[idx] = b;
    //                }
    //            }
    //        }

    //        /*Calculate StDev and Min/Max Values for Stretch */
    //        double average = flatArray.Average(x => x);
    //        double sumOfSquaresOfDifferences = flatArray.Select(val => (val - average) * (val - average)).Aggregate((a,b) => a+b);
    //        double sd = Math.Sqrt(sumOfSquaresOfDifferences / flatArray.Length);
    //        dynamic min = (T)Convert.ChangeType(0, typeof(T)), max = (T)Convert.ChangeType(0, typeof(T));
    //        double factor = 2.5;

    //        if (average - factor * sd < 0) {
    //            min = (T)Convert.ChangeType(0, typeof(T)); 
    //        }
    //        else {
    //            min = (T)Convert.ChangeType(average - factor * sd, typeof(T));
    //        }

    //        if (average + factor * sd > maxValue) {
    //            max = maxValue;
    //        }
    //        else {
    //            max = (T)Convert.ChangeType(average + factor * sd, typeof(T)); 
    //        }



    //        iarr.FlatArray = flatArray.Cast<T>().ToArray();
    //        iarr.StDev = sd;
    //        iarr.Mean = average;
    //        iarr.MinStDev = min;
    //        iarr.MaxStDev = max;
    //        iarr.Histogram = histogram;
    //        return iarr;
    //    }

    //    public async static Task<ImageArray<T>> createInstanceAsync(Array input) {
    //        return await Task<ImageArray<T>>.Run(() => createInstance(input));
    //    }

    //    public T[] stretchArray(ImageArray<T> source) {
    //        dynamic maxVal = source.MaxStDev;
    //        dynamic minVal = source.MinStDev;
    //        dynamic d = maxVal - minVal;

    //        FieldInfo maxValueField = typeof(T).GetField("MaxValue", BindingFlags.Public | BindingFlags.Static);
    //        if (maxValueField == null)
    //            throw new NotSupportedException(typeof(T).Name);
    //        dynamic maxValue = (T)maxValueField.GetValue(null);


    //        T[] stretchedArr = new T[source.FlatArray.Length];

    //        for (int i = 0; i < source.FlatArray.Length; i++) {

    //            dynamic val = (((float)(source.FlatArray[i] - minVal) / d) * (maxValue));
    //            if (val > maxValue) {
    //                val = maxValue;
    //            }
    //            stretchedArr[i] = (T)Convert.ChangeType(val, typeof(T));

    //        }
    //        return stretchedArr;
    //    }

    //    public async Task<T[]> stretchArrayAsync(ImageArray<T> source) {
    //        return await Task<T[]>.Run(() => stretchArrayAsync(source));
    //    }

    //    public bool saveFITS(string path, string imagetype, double duration, string filter, Model.MyCamera.BinningMode binning, double temp) {
    //        bool bSuccess = false;
    //        try {
    //            int bitpix , bzero, minvalue;

    //            Type targetType;
    //            if (typeof(T) == typeof(ushort)) {
    //                targetType = typeof(short);
    //                bitpix = 16;
    //                bzero = 32768;
    //                minvalue = short.MinValue;
    //            }
    //            else if (typeof(T) == typeof(byte)) {
    //                targetType = typeof(sbyte);
    //                bitpix = 8;
    //                bzero = 256;
    //                minvalue = sbyte.MinValue;
    //            }
    //            else {
    //                targetType = typeof(short);
    //                bitpix = 16;
    //                bzero = 32768;
    //                minvalue = short.MinValue;
    //            }

    //            Header h = new Header();
    //            h.AddValue("SIMPLE", "T", "C# FITS");
    //            h.AddValue("BITPIX", bitpix, "");
    //            h.AddValue("NAXIS", 2, "Dimensionality");
    //            h.AddValue("NAXIS1", this.X, "");
    //            h.AddValue("NAXIS2", this.Y, "");
    //            h.AddValue("BZERO", bzero, "");
    //            h.AddValue("EXTEND", "T", "Extensions are permitted");

    //            if (!string.IsNullOrEmpty(filter)) {
    //                h.AddValue("FILTER", filter, "");
    //            }

    //            if (binning != null) {
    //                h.AddValue("CCDXBIN", binning.X, "");
    //                h.AddValue("CCDYBIN", binning.Y, "");
    //                h.AddValue("XBINNING", binning.X, "");
    //                h.AddValue("YBINNING", binning.Y, "");
    //            }
    //            h.AddValue("TEMPERAT", temp, "");

    //            h.AddValue("IMAGETYP", imagetype, "");

    //            h.AddValue("EXPOSURE", duration, "");
    //            /*

    //             h.AddValue("OBJECT", 32768, "");
    //             */




    //            dynamic[][] curl = new dynamic[this.Y][];
    //            dynamic val;
    //            int idx = 0;
    //            for (int i = 0; i < this.Y; i++) {
    //                curl[i] = new dynamic[this.X];
    //                for (int j = 0; j < this.X; j++) {
    //                    val = this.FlatArray[idx];
    //                    curl[i][j] = Convert.ChangeType(minvalue + val, targetType.GetType());  

    //                    idx++;
    //                }
    //            }
    //            ImageData d = new ImageData(curl);

    //            Fits fits = new Fits();
    //            BasicHDU hdu = FitsFactory.HDUFactory(h, d);
    //            fits.AddHDU(hdu);

    //            Directory.CreateDirectory(Path.GetDirectoryName(path));
    //            using (FileStream fs = new FileStream(path + ".fits", FileMode.Create)) {
    //                fits.Write(fs);
    //            }
    //            bSuccess = true;
    //        }
    //        catch (Exception ex) {
    //            Notification.ShowError("Image file error: " + ex.Message);
    //            Logger.error(ex.Message);

    //        }
    //        return bSuccess;
    //    }

    //    public async Task<bool> saveFITSAsync(string path, string imagetype, double duration, string filter, Model.MyCamera.BinningMode binning, double temp) {
    //        return await Task<T[]>.Run(() => saveFITSAsync(path, imagetype, duration, filter, binning, temp));
    //    }

    //    public bool saveTIFF(string path) {
    //        bool bSuccess = false;
    //        try {

    //            System.Windows.Media.PixelFormat pf = determinePixelFormat();

    //            BitmapSource bmpSource = createBitmapSource(pf);

    //            Directory.CreateDirectory(Path.GetDirectoryName(path));

    //            using (FileStream fs = new FileStream(path + ".tif", FileMode.Create)) {
    //                TiffBitmapEncoder encoder = new TiffBitmapEncoder();
    //                encoder.Compression = TiffCompressOption.None;
    //                encoder.Frames.Add(BitmapFrame.Create(bmpSource));
    //                encoder.Save(fs);
    //            }
    //            bSuccess = true;
    //        }
    //        catch (Exception ex) {
    //            Notification.ShowError("Image file error: " + ex.Message);
    //            Logger.error(ex.Message);

    //        }
    //        return bSuccess;
    //    }

    //    public async Task<bool> saveTIFFAsync(string path) {
    //        return await Task<T[]>.Run(() => saveTIFFAsync(path));
    //    }

    //    public BitmapSource createBitmapSource(System.Windows.Media.PixelFormat pf) {

    //        //int stride = C.CameraYSize * ((Convert.ToString(C.MaxADU, 2)).Length + 7) / 8;
    //        int stride = (this.X * pf.BitsPerPixel + 7) / 8;
    //        double dpi = 96;

    //        BitmapSource source = BitmapSource.Create(this.X, this.Y, dpi, dpi, pf, null, this.FlatArray, stride);
    //        return source;
    //    }

    //    private System.Windows.Media.PixelFormat determinePixelFormat() {
    //        System.Windows.Media.PixelFormat pf;


    //        if (typeof(T) == typeof(ushort)){
    //            pf = System.Windows.Media.PixelFormats.Gray16;
    //        } else if (typeof(T) == typeof(byte)) {
    //            pf = System.Windows.Media.PixelFormats.Gray8;
    //        } else { 
    //            pf = System.Windows.Media.PixelFormats.Gray16;
    //        }

    //        return pf;
    //    }

    //}    
}
