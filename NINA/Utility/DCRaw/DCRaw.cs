using NINA.Model.MyCamera;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Utility.DCRaw {

    class DCRaw {
        public DCRaw() {

        }

        static string DCRAWLOCATION = @"Utility\DCRaw\dcraw.exe";
        public static string TMPIMGFILEPATH = Environment.GetEnvironmentVariable("LocalAppData") + "\\NINA\\dcraw_tmp";

        public async Task<ImageArray> ConvertToImageArray(string fileextension, CancellationToken token) {
            ImageArray iarr = null;
            try {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                startInfo.FileName = DCRAWLOCATION;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                startInfo.Arguments = "-4 -d -T -t 0 " + TMPIMGFILEPATH + fileextension;
                process.StartInfo = startInfo;
                process.Start();

                while (!process.StandardOutput.EndOfStream) {
                    token.ThrowIfCancellationRequested();
                }

                var file = TMPIMGFILEPATH + ".tiff";

                if (File.Exists(file)) {
                    TiffBitmapDecoder TifDec = new TiffBitmapDecoder(new Uri(file), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    BitmapFrame bmp = TifDec.Frames[0];
                    ushort[] pixels = new ushort[bmp.PixelWidth * bmp.PixelHeight];
                    bmp.CopyPixels(pixels, 2 * bmp.PixelWidth, 0);
                    iarr = await ImageArray.CreateInstance(pixels, (int)bmp.PixelWidth, (int)bmp.PixelHeight, true);


                } else {
                    Notification.Notification.ShowError("Error occured during DCRaw conversion");
                }
            } catch (Exception ex) {
                Notification.Notification.ShowError(ex.Message);
                Logger.Error(ex.Message, ex.StackTrace);
            }
            return iarr;
        }
    }


}
