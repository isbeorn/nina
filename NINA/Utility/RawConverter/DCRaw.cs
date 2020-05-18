#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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

        public async Task<IImageData> Convert(
            MemoryStream s,
            int bitDepth,
            string rawType,
            ImageMetaData metaData,
            CancellationToken token = default) {
            return await Task.Run(async () => {
                using (MyStopWatch.Measure()) {
                    var fileextension = ".raw";
                    var filename = Path.GetRandomFileName();
                    var rawfile = Path.Combine(Utility.APPLICATIONTEMPPATH, filename + fileextension);

                    using (var filestream = new System.IO.FileStream(rawfile, System.IO.FileMode.Create)) {
                        s.WriteTo(filestream);
                    }

                    ImageData data = null;
                    var outputFile = Path.Combine(Utility.APPLICATIONTEMPPATH, filename + ".tiff");
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

                                var imageArray = new ImageArray(flatArray: pixels, rawData: s.ToArray(), rawType: rawType);
                                data = new ImageData(
                                    imageArray: imageArray,
                                    width: (int)bmp.PixelWidth,
                                    height: (int)bmp.PixelHeight,
                                    bitDepth: bitDepth,
                                    isBayered: true,
                                    metaData: metaData);
                            } else {
                                Logger.Error("File not found: " + outputFile);
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
