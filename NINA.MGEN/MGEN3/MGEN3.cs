#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FTD2XX_NET;
using NINA.Exceptions;
using NINA.MGEN;
using NINA.MGEN2;
using NINA.MGEN2.Commands;
using NINA.MGEN2.Commands.AppMode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.MGEN3 {

    public class MGEN3 : IMGEN {
        private IMG3SDK sdk;
        private Bitmap display;
        private ushort[] displayBuffer;
        private byte[] ledBuffer;
        private LEDState lEDs = new LEDState();
        private const int displayWidth = 240;
        private const int displayHeight = 320;
        private bool displayInitialized = false;
        private ILogger logger;

        /// <summary>
        ///
        /// </summary>
        /// <param name="MGEN3LibPath">path to the MG3lib.dll</param>
        public MGEN3(string ftdiDllPath, string mg3DllPath, ILogger logger) {
            this.sdk = new LoggingMG3SDK(new MG3SDK(ftdiDllPath, mg3DllPath, logger), logger);
            this.logger = logger;
        }

        public double PixelSize => 3.75;

        public int SensorSizeX => 1280;

        public int SensorSizeY => 960;

        public Task Disconnect(CancellationToken ct = default) {
            return Task.Run(() => {
                sdk.Close();
            }, ct);
        }

        public Task DetectAndOpen(CancellationToken ct = default) {
            return Task.Run(async () => {
                var poll = sdk.PollDevice();
                var response = sdk.Open();
                logger.Trace($"MGEN3 Open returned with code {response}");

                if (response == MG3SDK.MG3_FAILED_TO_OPEN_DEVICE) {
                    throw new Exception("No MGEN device found");
                } else if (response == MG3SDK.MG3_CANT_READ_PROTOCOL) {
                    //DOES NOT WORK CURRENTLY in x64- PULSEESC does not return and goes into boot mode always!

                    //Device is turned off - turning it on
                    var pulse = sdk.PulseESC(200);

                    logger.Trace($"MGEN3 PulseESC returned with code {pulse}");

                    if (pulse != MG3SDK.MG3_OK) {
                        throw new Exception("Unable to turn on MGEN device");
                    }

                    await Task.Delay(5000, ct);
                    response = sdk.Open();

                    logger.Trace($"MGEN3 Open returned with code {response}");

                    if (response != MG3SDK.MG3_OK) {
                        throw new Exception($"Failed to connect to device due to Error {response}");
                    }
                } else if (response != MG3SDK.MG3_OK) {
                    throw new Exception($"Failed to connect to device due to Error {response}");
                }

                logger.Debug("MGEN3 connection opened successfully");

                displayBuffer = new ushort[displayWidth * displayHeight];
                ledBuffer = new byte[6 * 2];
                lEDs = new LEDState();
                display = new Bitmap(displayWidth, displayHeight, System.Drawing.Imaging.PixelFormat.Format16bppRgb565);
                displayInitialized = false;
            }, ct);
        }

        public Task<bool> Dither(CancellationToken ct = default) {
            return Task.Run(async () => {
                var ditherCode = this.sdk.Dither();

                logger.Trace($"Dither called with return code {ditherCode}");

                if (ditherCode == MG3SDK.MG3_OK) {
                    bool dithering = true;
                    do {
                        var err = this.sdk.GetDitherState(out var state);

                        logger.Trace($"GetDitherState called with return code {err} and state {state}");

                        if (err == MG3SDK.MG3_OK) {
                            // bits[0..3] -> state => 0 = inactive; 1 or 2 = active
                            // bits[6] -> dithering enabled
                            // bits[7] -> AG enabled
                            if ((state & 1) == 0) {
                                logger.Trace($"Dither is inactive");
                                //Dithering inactive
                                break;
                            }
                        }
                        await Task.Delay(1000, ct);
                    } while (dithering);

                    return true;
                } else {
                    logger.Error($"MGEN3 Dither failed with code {ditherCode}");
                    return false;
                }
            });
        }

        public Task<ImagingParameter> GetImagingParameter(CancellationToken ct = default) {
            return Task.Run(() => {
                ValidateReturnCode(this.sdk.ReadImagingParameters(out var gain, out var exposureTime));

                var parameter = new ImagingParameter(gain, exposureTime, 0);
                return parameter;
            });
        }

        public Task<bool> IsGuidingActive(CancellationToken ct = default) {
            return Task.Run(() => {
                MG3SDK.MGEN3_FrameData data = new MG3SDK.MGEN3_FrameData();
                data.query = 0b_0001;
                var err = this.sdk.ReadLastFrameData(ref data);

                this.logger.Trace($"ReadLastFrameData returned with code {err} - Guiding enabled: {data.ag_enabled}");

                if (err == MG3SDK.MG3_OK) {
                    return data.ag_enabled > 0;
                }
                // todo error
                return false;
            });
        }

        public Task<bool> PressButton(MGENButton button, CancellationToken ct) {
            return Task.Run(async () => {
                int buttonCode = (int)button;
                ValidateReturnCode(sdk.PushButton(buttonCode, false));
                await Task.Delay(50, ct);
                var btn2 = sdk.PushButton(buttonCode, true);
                ValidateReturnCode(btn2);
                return btn2 == MG3SDK.MG3_OK;
            });
        }

        private object lockobj = new object();

        public Task<Bitmap> ReadDisplay(Color primaryColor, Color backgroundColor, CancellationToken ct = default) {
            return Task.Run(() => {
                /* Read display and led state */
                ValidateReturnCode(sdk.ReadDisplay(displayBuffer, ledBuffer, !displayInitialized));

                /* Generate iamge based on displayBuffer array */
                var rectangle = new System.Drawing.Rectangle(0, 0, displayWidth, displayHeight);

                lock (lockobj) {
                    BitmapData bitmapData = display.LockBits(rectangle, ImageLockMode.ReadWrite, display.PixelFormat);
                    var scan0 = bitmapData.Scan0;

                    unsafe {
                        byte* destination = (byte*)scan0;

                        var idxDest = 0;
                        for (var i = 0; i < displayBuffer.Length; i++) {
                            destination[idxDest++] = (byte)(displayBuffer[i] & byte.MaxValue);
                            destination[idxDest++] = (byte)((displayBuffer[i] & 65280) >> 8);
                        }
                    }
                    display.UnlockBits(bitmapData);
                }

                /* Set led state based on led buffer */
                lEDs = new LEDState();
                // ESC
                lEDs.TopLeft = GetLEDColor(ledBuffer[0], ledBuffer[1]);

                // SET
                lEDs.TopRight = GetLEDColor(ledBuffer[2], ledBuffer[3]);

                // LEFT
                lEDs.Left = GetLEDColor(ledBuffer[4], 0);

                // RIGHT
                lEDs.Right = GetLEDColor(ledBuffer[6], 0);

                // UP
                lEDs.Up = GetLEDColor(ledBuffer[8], 0);

                // DOWN
                lEDs.Down = GetLEDColor(ledBuffer[10], 0);

                displayInitialized = true;

                return display;
            });
        }

        private Color GetLEDColor(byte brightness, byte color) {
            var baseColor = Color.Transparent;
            if (brightness > 0) {
                switch (color) {
                    case 0:
                        baseColor = Color.Red;
                        break;

                    case 1:
                        baseColor = Color.Purple;
                        break;

                    case 2:
                        baseColor = Color.Blue;
                        break;

                    default:
                        baseColor = Color.Red;
                        break;
                }

                var alpha = (int)((brightness / 9d) * 255);

                return Color.FromArgb(alpha, baseColor);
            }

            return baseColor;
        }

        public Task<LEDState> ReadLEDState(CancellationToken ct = default) {
            return Task.FromResult(lEDs);
        }

        public Task<bool> SetImagingParameter(int gain, int exposureTime, int threshold, CancellationToken ct = default) {
            return Task.Run(() => {
                ValidateReturnCode(this.sdk.SetImagingParameters(gain, exposureTime));
                return true;
            });
        }

        public Task<bool> CancelCalibration(CancellationToken ct = default) {
            return Task.Run(() => {
                int res;
                do {
                    res = this.sdk.CancelFunction();
                } while (res == MG3SDK.MG3_FUNC_BUSY);
                return true;
            });
        }

        public Task<StarData> GetStarData(byte starIndex, CancellationToken ct = default) {
            // Doesn't look like this is available. Start Starsearch already sets star so GetStarData + SetNewGuidingPosition seem to be not required
            // Maybe remove it from the interface and put the required logic for mgen2 into StartStarSearch
            return Task.FromResult(new StarData(0, 0, 0, 0, 0));
        }

        public Task<CalibrationStatusReport> QueryCalibration(CancellationToken ct = default) {
            return Task.Run(() => {
                ValidateReturnCode(this.sdk.ReadCalibration(out var calibration));

                logger.Debug($"Calibration is raX {calibration.rax}, raY {calibration.ray}, decX {calibration.decx}, decY {calibration.decy}");

                var status = CalibrationStatus.NotStarted;
                if (calibration.rax > 0.0000001F || calibration.ray > 0.0000001F || calibration.rax < -0.0000001F || calibration.ray < -0.0000001F) {
                    status = CalibrationStatus.Done;
                }

                return new CalibrationStatusReport(status);
            });
        }

        public Task<GuideState> QueryGuideState(CancellationToken ct = default) {
            return Task<GuideState>.Run(() => {
                MG3SDK.MGEN3_FrameData data = new MG3SDK.MGEN3_FrameData();
                data.query = 0b_1111;

                var err = this.sdk.ReadLastFrameData(ref data);

                this.logger.Trace($"ReadLastFrameData returned with code {err} - Guiding enabled: {data.ag_enabled}");

                if (err == MG3SDK.MG3_OK) {
                    var dx = data.pos_x - data.ag_center_x;
                    var dy = data.pos_y - data.ag_center_y;
                    var driftRA = dx * data.cal_ra_x + dy * data.cal_ra_y;
                    var driftDEC = dx * data.cal_dec_x + dy * data.cal_dec_y;

                    var frameInfo = new FrameInfo(
                        data.frame_idx,
                        data.pos_x,
                        data.pos_y,
                        driftRA * 256,
                        driftDEC * 256
                    );

                    var state = new GuideState(data.ag_enabled > 0, frameInfo);
                    return state;
                }
                return new GuideState(false, new FrameInfo(0, 0, 0, 0, 0));
            });
        }

        public Task<bool> SetNewGuidingPosition(StarData starDetail, CancellationToken ct = default) {
            // Doesn't look like this is available
            return Task.FromResult(true);
        }

        public Task<bool> StartCamera(CancellationToken ct = default) {
            // Doesn't look like this is available
            return Task.FromResult(true);
        }

        public Task<int> StartStarSearch(int gain, int exposureTime, CancellationToken ct = default) {
            return Task.Run(async () => {
                ValidateReturnCode(this.sdk.StarSearch());

                await WaitForFunction(ct);

                MG3SDK.MGEN3_FrameData data = new MG3SDK.MGEN3_FrameData();
                data.query = 0b_0001;
                var err = this.sdk.ReadLastFrameData(ref data);

                return (int)data.star_present;
            });
        }

        public Task<bool> ClearCalibration(CancellationToken ct = default) {
            return Task.Run(async () => {
                var err = this.sdk.ClearCalibration();
                if (err != MG3SDK.MG3_OK) {
                    logger.Error($"Clearing calibration failed with return code {err}");
                    return false;
                }
                return true;
            });
        }

        public Task<bool> StartCalibration(CancellationToken ct = default) {
            return Task.Run(async () => {
                ValidateReturnCode(this.sdk.StartCalibration());
                await Task.Delay(2000, ct);

                await WaitForFunction(ct);

                return true;
            });
        }

        private void ValidateReturnCode(int returnCode, [CallerMemberName] string caller = "") {
            logger.Trace($"{caller} returned {returnCode}");
            switch (returnCode) {
                case MG3SDK.MG3_INVALID_HANDLE:
                    throw new Exception("Connection handle is invalid");
                case MG3SDK.MG3_CMD_EXEC_ERROR:
                    throw new Exception("Command execution failed");
                case MG3SDK.MG3_FUNC_BUSY:
                    throw new AnotherCommandInProgressException();
                case MG3SDK.MG3_INVALID_ANSWER:
                case MG3SDK.MG3_ANSWER_TOO_SHORT:
                    throw new UnexpectedReturnCodeException();
                case MG3SDK.MG3_OK:
                    return;
            }
        }

        private void ValidateFunctionReturnCode(uint fnReturnCode, [CallerMemberName] string caller = "") {
            logger.Trace($"{caller} function is origin. GetFunctionState returned function code {fnReturnCode}");
            switch (fnReturnCode) {
                case MG3SDK.MG3_FUNC_BUSY:
                    throw new AnotherCommandInProgressException();

                case MG3SDK.ERR_CAMERA_NOT_ACTIVE:
                    throw new CameraIsOffException();

                case MG3SDK.ERR_CAMERA_BUSY:
                    throw new UILockedException();

                case MG3SDK.ERR_FUNCTION_IS_DISABLED:
                    throw new AutoGuidingActiveException();

                case MG3SDK.ERR_GUIDESTAR_NOT_AVAILABLE:
                    throw new NoStarSeenException();

                case MG3SDK.ERR_CALIBRATION_MISSING:
                    throw new Exception("Calibration is missing to start autoguiding");

                case MG3SDK.ERR_AG_ACTIVE:
                    throw new AutoGuidingActiveException();

                case MG3SDK.ERR_FUNCTION_TIMED_OUT:
                    throw new Exception("Function timed out");
                case MG3SDK.ERR_CANCELED:
                    logger.Info("Function was cancelled");
                    return;

                case MG3SDK.MG3_OK:
                    return;
            }
        }

        private async Task WaitForFunction(CancellationToken ct, [CallerMemberName] string caller = "") {
            int result;
            while ((result = this.sdk.GetFunctionState(out var fnResult)) == MG3SDK.MG3_FUNC_BUSY) {
                await Task.Delay(500, ct);
                logger.Trace($"{caller} function is origin. GetFunctionState returned {result}");
                ValidateFunctionReturnCode((uint)fnResult, caller);
            };
            logger.Trace($"{caller} function is origin. GetFunctionState returned {result}");
            await Task.Delay(500, ct);
        }

        public Task<bool> StartGuiding(CancellationToken ct = default) {
            return Task.Run(async () => {
                var p = await this.GetImagingParameter(ct);
                ValidateReturnCode(await StartStarSearch(p.Gain, p.ExposureTime, ct));

                await Task.Delay(5000, ct);

                ValidateReturnCode(this.sdk.StartAutoGuiding(0));
                try {
                    await WaitForFunction(ct);
                } catch (MGEN.Exceptions.CalibrationIsMissingException) {
                    logger.Info("Calibration is missing to start guiding. Starting calibration");
                    await StartCalibration(ct);
                    return await StartGuiding(ct);
                }

                return true;
            });
        }

        public Task<bool> StopGuiding(CancellationToken ct = default) {
            return Task.Run(async () => {
                ValidateReturnCode(this.sdk.StopAutoGuiding());

                await WaitForFunction(ct);

                return true;
            });
        }

        public Task<DitherAmplitude> GetDitherAmplitude(CancellationToken ct = default) {
            return Task.Run(() => {
                ValidateReturnCode(this.sdk.ReadDitheringParameters(out var dp));

                var amp = new DitherAmplitude(dp.diameter);
                return amp;
            });
        }

        public Task<bool> IsActivelyGuiding(CancellationToken ct) {
            return IsGuidingActive(ct);
        }
    }
}