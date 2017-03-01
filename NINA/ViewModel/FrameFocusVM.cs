using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Utility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace NINA.ViewModel {
    class FrameFocusVM : BaseVM {
        public FrameFocusVM() {
            Name = "Frame & Focus";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["FocusSVG"];
            CancelSnapCommand = new RelayCommand(CancelCaptureImage);
            SnapCommand = new AsyncCommand<bool>(() => Snap(new Progress<string>(p => ImagingVM.ExpStatus = p)));
            ApplyImageParamsCommand = new AsyncCommand<bool>(() => ApplyImageParams());            
            Gamma = 1;
            Contrast = 1;
            Brightness = 1;
            Zoom = 1;
            SnapExposureDuration = 1;

        }

        private ImagingVM _imagingVM;
        public ImagingVM ImagingVM {
            get {
                return _imagingVM;
            }
            set {
                _imagingVM = value;
                RaisePropertyChanged();
            }
        }
                
        private bool _loop;
        public bool Loop {
            get {
                return _loop;
            }
            set {
                _loop = value;
                RaisePropertyChanged();
            }

        }

        private bool _calcHFR;
        public bool CalcHFR {
            get {
                return _calcHFR;
            }
            set {
                _calcHFR = value;
                RaisePropertyChanged();
            }

        }

        private double _zoom;
        public double Zoom {
            get {
                return _zoom;
            }
            set {
                _zoom = value;
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

        private float _brightness;
        public float Brightness {
            get {
                return _brightness;
            }

            set {
                _brightness = value;
                RaisePropertyChanged();
            }
        }

        private float _contrast;
        public float Contrast {
            get {
                return _contrast;
            }

            set {
                _contrast = value;
                RaisePropertyChanged();
            }
        }

        private float _gamma;
        public float Gamma {
            get {
                return _gamma;
            }

            set {
                _gamma = value;
                RaisePropertyChanged();
            }
        }

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

        private BinningMode _snapBin;
        public BinningMode SnapBin {
            get {
                if (_snapBin == null) {
                    _snapBin = new BinningMode(1, 1);
                }
                return _snapBin;
            }
            set {
                _snapBin = value;
                RaisePropertyChanged();
            }
        }
        


        private async Task<bool> Snap(IProgress<string> progress) {
            if (ImagingVM.IsExposing) {
                Notification.ShowWarning("Camera is busy");
                return false;
            } else {
                do {
                    _captureImageToken = new CancellationTokenSource();
                    List<SequenceModel> seq = new List<SequenceModel>();
                    seq.Add(new SequenceModel(SnapExposureDuration, SequenceModel.ImageTypes.SNAP, SnapFilter, SnapBin, 1));
                    await ImagingVM.StartSequence(seq, CalcHFR, false, _captureImageToken, progress);
                    _captureImageToken.Token.ThrowIfCancellationRequested();
                } while (Loop);
                return true;
            }
            
        }



                
        private async Task<bool> ApplyImageParams() {            
            if(ImagingVM.Image != null) {

                Bitmap bmp = await Task.Run<Bitmap>(async () => {
                        BitmapSource bs = await ImagingVM.Prepare(ImagingVM.SourceArray.FlatArray, ImagingVM.SourceArray.X, ImagingVM.SourceArray.Y);
                        Bitmap b = ImageAnalysis.BitmapFromSource(bs);
                        b = AdjustImage(b);
                        return b;
                    });
               
                ImagingVM.Image = ImageAnalysis.ConvertBitmap(bmp);
                bmp.Dispose();                
                
            }
            bool run = await Task.Run<bool>(() => { return true; });
            return run ;            
        }


        private Bitmap AdjustImage(Bitmap source) {
            
            Bitmap adjustedImage = new Bitmap(source.Width, source.Height); ;
            float brightness = Brightness;// 1.0f; // no change in brightness
            float contrast = Contrast;//2.0f; // twice the contrast
            float gamma = Gamma; //1.0f; // no change in gamma

            float adjustedBrightness = brightness - 1.0f;
            // create matrix that will brighten and contrast the image
            float[][] ptsArray ={
        new float[] {contrast, 0, 0, 0, 0}, // scale red
        new float[] {0, contrast, 0, 0, 0}, // scale green
        new float[] {0, 0, contrast, 0, 0}, // scale blue
        new float[] {0, 0, 0, 1.0f, 0}, // don't scale alpha
        new float[] {adjustedBrightness, adjustedBrightness, adjustedBrightness, 0, 1}};

            ImageAttributes imageAttributes = new ImageAttributes();
            imageAttributes.ClearColorMatrix();
            imageAttributes.SetColorMatrix(new ColorMatrix(ptsArray), ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            imageAttributes.SetGamma(gamma, ColorAdjustType.Bitmap);
            Graphics g = Graphics.FromImage(adjustedImage);
            
            g.DrawImage(source, new Rectangle(0, 0, adjustedImage.Width, adjustedImage.Height)
                , 0, 0, source.Width, source.Height,
                GraphicsUnit.Pixel, imageAttributes);
            source.Dispose();
            return adjustedImage;
        }
        
        private AsyncCommand<bool> _snapCommand;
        public AsyncCommand<bool> SnapCommand {
            get {
                return _snapCommand;
            }
            set {
                _snapCommand = value;
                RaisePropertyChanged();
            }
        }

        private AsyncCommand<bool> _applyImageParamsCommand;
        private CancellationTokenSource _captureImageToken;

        public AsyncCommand<bool> ApplyImageParamsCommand {
            get {
                return _applyImageParamsCommand;
            }
            set {
                _applyImageParamsCommand = value;
                RaisePropertyChanged();
            }
        }

        private AsyncCommand<bool> _autoStretchCommand;
        public AsyncCommand<bool> AutoStretchCommand {
            get {
                return _autoStretchCommand;
            } set {
                _autoStretchCommand = value;
                RaisePropertyChanged();
            }
        }

        private RelayCommand _cancelSnapCommand;
        public RelayCommand CancelSnapCommand {
            get {
                return _cancelSnapCommand;
            }

            set {
                _cancelSnapCommand = value;
                RaisePropertyChanged();
            }
        }

        private void CancelCaptureImage(object o) {
            if (_captureImageToken != null) {
                _captureImageToken.Cancel();
            }
        }






        //    public static bool adjustGamma(Bitmap b, double red, double green, double blue) {
        //        if (red < .2 || red > 5) return false;
        //        if (green < .2 || green > 5) return false;
        //        if (blue < .2 || blue > 5) return false;

        //        byte[] redGamma = new byte[256];
        //        byte[] greenGamma = new byte[256];
        //        byte[] blueGamma = new byte[256];

        //        for (int i = 0; i < 256; ++i) {
        //            redGamma[i] = (byte)Math.Min(255, (int)((255.0 * Math.Pow(i / 255.0, 1.0 / red)) + 0.5));
        //            greenGamma[i] = (byte)Math.Min(255, (int)((255.0 * Math.Pow(i / 255.0, 1.0 / green)) + 0.5));
        //            blueGamma[i] = (byte)Math.Min(255, (int)((255.0 * Math.Pow(i / 255.0, 1.0 / blue)) + 0.5));
        //        }

        //        // GDI+ still lies to us - the return format is BGR, NOT RGB.
        //        BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

        //        int stride = bmData.Stride;
        //        System.IntPtr Scan0 = bmData.Scan0;

        //        unsafe
        //        {
        //            byte* p = (byte*)(void*)Scan0;

        //            int nOffset = stride - b.Width * 3;

        //            for (int y = 0; y < b.Height; ++y) {
        //                for (int x = 0; x < b.Width; ++x) {
        //                    p[2] = redGamma[p[2]];
        //                    p[1] = greenGamma[p[1]];
        //                    p[0] = blueGamma[p[0]];

        //                    p += 3;
        //                }
        //                p += nOffset;
        //            }
        //        }

        //        b.UnlockBits(bmData);

        //        return true;
        //    }

        //    public static bool adjustBrightness(Bitmap b, int nBrightness)
        //	{
        //		if (nBrightness< -255 || nBrightness> 255)
        //			return false;

        //		// GDI+ still lies to us - the return format is BGR, NOT RGB.
        //		BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format16bppGrayScale);

        //		int stride = bmData.Stride;
        //		System.IntPtr Scan0 = bmData.Scan0;

        //		int nVal = 0;

        //		unsafe
        //		{
        //			byte* p = (byte*)(void*)Scan0;

        //			int nOffset = stride - b.Width * 3;
        //			int nWidth = b.Width * 3;

        //			for(int y = 0; y<b.Height;++y)
        //			{
        //			for(int x = 0; x<nWidth; ++x )
        //			{
        //				nVal = (int) (p[0] + nBrightness);

        //				if (nVal< 0) nVal = 0;
        //				if (nVal > 255) nVal = 255;

        //				p[0] = (byte)nVal;

        //				++p;
        //			}
        //			p += nOffset;
        //		}
        //	}

        //	b.UnlockBits(bmData);

        //	return true;
        //}

        //public static bool adjustContrast(Bitmap b, sbyte nContrast)
        //{
        //	if (nContrast< -100) return false;
        //	if (nContrast >  100) return false;

        //	double pixel = 0, contrast = (100.0 + nContrast) / 100.0;

        //	contrast *= contrast;

        //	int red, green, blue;

        //	// GDI+ still lies to us - the return format is BGR, NOT RGB.
        //	BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format16bppGrayScale);

        //	int stride = bmData.Stride;
        //	System.IntPtr Scan0 = bmData.Scan0;

        //	unsafe
        //	{
        //		byte* p = (byte*)(void*)Scan0;

        //		int nOffset = stride - b.Width * 3;

        //		for(int y = 0; y<b.Height;++y)
        //		{
        //			for(int x = 0; x<b.Width; ++x )
        //			{
        //				blue = p[0];
        //				green = p[1];
        //				red = p[2];

        //				pixel = red/255.0;
        //				pixel -= 0.5;
        //				pixel *= contrast;
        //				pixel += 0.5;
        //				pixel *= 255;
        //				if (pixel< 0) pixel = 0;
        //				if (pixel > 255) pixel = 255;
        //				p[2] = (byte) pixel;

        //				pixel = green/255.0;
        //				pixel -= 0.5;
        //				pixel *= contrast;
        //				pixel += 0.5;
        //				pixel *= 255;
        //				if (pixel< 0) pixel = 0;
        //				if (pixel > 255) pixel = 255;
        //				p[1] = (byte) pixel;

        //				pixel = blue/255.0;
        //				pixel -= 0.5;
        //				pixel *= contrast;
        //				pixel += 0.5;
        //				pixel *= 255;
        //				if (pixel< 0) pixel = 0;
        //				if (pixel > 255) pixel = 255;
        //				p[0] = (byte) pixel;					

        //				p += 3;
        //			}
        //			p += nOffset;
        //		}
        //	}

        //	b.UnlockBits(bmData);

        //	return true;
        //}

        //    public Bitmap equalizeHist(Bitmap b) {
        //        BitmapData data = b.LockBits(new System.Drawing.Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format16bppGrayScale);
        //        unsafe
        //        {
        //            byte* ptr = (byte*)data.Scan0;

        //            int remain = data.Stride - data.Width * 3;

        //            int[] histogram = new int[256];
        //            for (int i = 0; i < histogram.Length; i++)
        //                histogram[i] = 0;

        //            for (int i = 0; i < data.Height; i++) {
        //                for (int j = 0; j < data.Width; j++) {
        //                    int mean = ptr[0] + ptr[1] + ptr[2];
        //                    mean /= 3;

        //                    histogram[mean]++;
        //                    ptr += 3;
        //                }

        //                ptr += remain;
        //            }

        //            float[] LUT = equalize(histogram, data.Width * data.Height);
        //            ptr = (byte*)data.Scan0;

        //            for (int i = 0; i < data.Height; i++) {
        //                for (int j = 0; j < data.Width; j++) {
        //                    int index = ptr[0];
        //                    byte nValue = (byte)LUT[index];
        //                    if (LUT[index] > 255)
        //                        nValue = 255;
        //                    ptr[0] = ptr[1] = ptr[2] = nValue;
        //                    ptr += 3;
        //                }

        //                ptr += remain;
        //            }

        //            ptr = (byte*)data.Scan0;

        //            histogram = new int[256];
        //            for (int i = 0; i < histogram.Length; i++)
        //                histogram[i] = 0;

        //            for (int i = 0; i < data.Height; i++) {
        //                for (int j = 0; j < data.Width; j++) {
        //                    int mean = ptr[0];

        //                    histogram[mean]++;
        //                    ptr += 3;
        //                }

        //                ptr += remain;
        //            }



        //        }

        //        b.UnlockBits(data);
        //        return b;
        //    }

        //    public float[] equalize(int[] histogram, long numPixel) {
        //        float[] hist = new float[256];

        //        hist[0] = histogram[0] * histogram.Length / numPixel;
        //        long prev = histogram[0];
        //        string str = "";
        //        str += (int)hist[0] + "\n";

        //        for (int i = 1; i < hist.Length; i++) {
        //            prev += histogram[i];
        //            hist[i] = prev * histogram.Length / numPixel;
        //            str += (int)hist[i] + "   _" + i + "\t";
        //        }

        //        return hist;

        //    }


        //// Perform gamma correction on the image.
        //private Bitmap adjustGamma(Image image, float gamma) {
        //    // Set the ImageAttributes object's gamma value.
        //    ImageAttributes attributes = new ImageAttributes();
        //    attributes.SetGamma(gamma);

        //    // Draw the image onto the new bitmap while applying the new gamma value.
        //    Point[] points =
        //    {
        //        new Point(0, 0),
        //        new Point(image.Width , 0),
        //        new Point(0, image.Height ),
        //        };
        //    Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
        //    // Make the result bitmap.
        //    Bitmap bm = new Bitmap(image.Width, image.Height);
        //    using (Graphics gr = Graphics.FromImage(bm)) {
        //        gr.DrawImage(image, points, rect, GraphicsUnit.Pixel, attributes);
        //    }
        //    // Return the result.
        //    return bm;
        //}

    }
}
