#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Model.ImageData;
using NINA.Model.MyCamera;
using NINA.Utility.Extensions;
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

        public async Task<IImageData> Convert(MemoryStream s, int bitDepth, CancellationToken token) {
            return await Task.Run(async () => {
                using (MyStopWatch.Measure()) {
                    var fileextension = ".raw";
                    var rawfile = Path.Combine(Utility.APPLICATIONTEMPPATH, FILEPREFIX + fileextension);

                    using (var filestream = new System.IO.FileStream(rawfile, System.IO.FileMode.Create)) {
                        s.WriteTo(filestream);
                    }

                    ImageData data = null;
                    var outputFile = Path.Combine(Utility.APPLICATIONTEMPPATH, FILEPREFIX + ".tiff");
                    try {
                        System.Diagnostics.Process process;
                        System.Diagnostics.ProcessStartInfo startInfo;
                        var sb = new StringBuilder();
                        using (MyStopWatch.Measure("DCRawStart")) {
                            process = new System.Diagnostics.Process();
                            startInfo = new System.Diagnostics.ProcessStartInfo();
                            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                            startInfo.FileName = DCRAWLOCATION;
                            startInfo.UseShellExecute = false;
                            startInfo.RedirectStandardOutput = true;
                            startInfo.RedirectStandardError = true;
                            startInfo.RedirectStandardInput = true;
                            startInfo.CreateNoWindow = true;
                            startInfo.Arguments = "-4 -d -T -t 0 -v \"" + rawfile + "\"";
                            process.StartInfo = startInfo;
                            process.EnableRaisingEvents = true;

                            process.OutputDataReceived += (object sender, System.Diagnostics.DataReceivedEventArgs e) => {
                                sb.AppendLine(e.Data);
                            };

                            process.ErrorDataReceived += (object sender, System.Diagnostics.DataReceivedEventArgs e) => {
                                sb.AppendLine(e.Data);
                            };

                            process.Start();
                            process.BeginOutputReadLine();
                            process.BeginErrorReadLine();

                            await process.WaitForExitAsync(token);

                            Logger.Trace(sb.ToString());
                        }

                        using (MyStopWatch.Measure("DCRawReadIntoImageArray")) {
                            if (File.Exists(outputFile)) {
                                TiffBitmapDecoder TifDec = new TiffBitmapDecoder(new Uri(outputFile), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                                BitmapFrame bmp = TifDec.Frames[0];
                                ushort[] pixels = new ushort[bmp.PixelWidth * bmp.PixelHeight];
                                bmp.CopyPixels(pixels, 2 * bmp.PixelWidth, 0);

                                //Due to the settings of dcraw decoding the values will be stretched to 16 bits
                                bitDepth = 16;

                                data = new ImageData(pixels, (int)bmp.PixelWidth, (int)bmp.PixelHeight, bitDepth, true);
                                data.Data.RAWData = s.ToArray();
                            } else {
                                Logger.Error("File not found: " + outputFile, null);
                                throw new Exception("Error occured during DCRaw conversion." + Environment.NewLine + sb.ToString());
                            }
                        }
                    } catch (Exception ex) {
                        Notification.Notification.ShowError(ex.Message);
                        Logger.Error(ex);
                    } finally {
                        if (File.Exists(rawfile)) {
                            File.Delete(rawfile);
                        }
                        if (File.Exists(outputFile)) {
                            File.Delete(outputFile);
                        }
                    }
                    return data;
                }
            });
        }
    }
}