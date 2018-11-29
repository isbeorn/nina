using NINA.Model.MyCamera;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NINA.Utility.RawConverter {

    internal class DCRaw : IRawConverter {

        public DCRaw() {
        }

        private static string DCRAWLOCATION = @"Utility\DCRaw\dcraw.exe";
        public static string FILEPREFIX = "dcraw_tmp";

        public async Task<ImageArray> ConvertToImageArray(MemoryStream s, int bitDepth, int histogramResolution, CancellationToken token) {
            return await Task.Run(async () => {
                using (MyStopWatch.Measure()) {
                    var fileextension = ".raw";
                    var filename = Path.Combine(Utility.APPLICATIONTEMPPATH, DCRaw.FILEPREFIX + fileextension);

                    System.IO.FileStream filestream = new System.IO.FileStream(filename, System.IO.FileMode.Create);
                    s.WriteTo(filestream);

                    ImageArray iarr = null;
                    var rawfile = Path.Combine(Utility.APPLICATIONTEMPPATH, FILEPREFIX + fileextension);
                    var file = Path.Combine(Utility.APPLICATIONTEMPPATH, FILEPREFIX + ".tiff");
                    try {
                        System.Diagnostics.Process process;
                        System.Diagnostics.ProcessStartInfo startInfo;
                        using (MyStopWatch.Measure("DCRawStart")) {
                            process = new System.Diagnostics.Process();
                            startInfo = new System.Diagnostics.ProcessStartInfo();
                            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                            startInfo.FileName = DCRAWLOCATION;
                            startInfo.UseShellExecute = false;
                            startInfo.RedirectStandardOutput = true;
                            startInfo.CreateNoWindow = true;
                            startInfo.Arguments = "-4 -d -T -t 0 " + rawfile;
                            process.StartInfo = startInfo;
                            process.Start();
                        }

                        var sb = new StringBuilder();
                        using (MyStopWatch.Measure("DCRawWrite")) {
                            while (!process.StandardOutput.EndOfStream) {
                                sb.AppendLine(process.StandardOutput.ReadLine());
                                token.ThrowIfCancellationRequested();
                            }
                        }

                        using (MyStopWatch.Measure("DCRawReadIntoImageArray")) {
                            if (File.Exists(file)) {
                                TiffBitmapDecoder TifDec = new TiffBitmapDecoder(new Uri(file), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                                BitmapFrame bmp = TifDec.Frames[0];
                                ushort[] pixels = new ushort[bmp.PixelWidth * bmp.PixelHeight];
                                bmp.CopyPixels(pixels, 2 * bmp.PixelWidth, 0);

                                //Due to the settings of dcraw decoding the values will be stretched to 16 bits
                                bitDepth = 16;

                                iarr = await ImageArray.CreateInstance(pixels, (int)bmp.PixelWidth, (int)bmp.PixelHeight, bitDepth, true, true, histogramResolution);
                                iarr.RAWData = s.ToArray();
                            } else {
                                Notification.Notification.ShowError("Error occured during DCRaw conversion." + Environment.NewLine + sb.ToString());
                                Logger.Error(sb.ToString(), null);
                                Logger.Error("File not found: " + file, null);
                            }
                        }
                    } catch (Exception ex) {
                        Notification.Notification.ShowError(ex.Message);
                        Logger.Error(ex);
                    } finally {
                        filestream.Dispose();
                        if (File.Exists(rawfile)) {
                            File.Delete(rawfile);
                        }
                        if (File.Exists(file)) {
                            File.Delete(file);
                        }
                    }
                    return iarr;
                }
            });
        }
    }
}