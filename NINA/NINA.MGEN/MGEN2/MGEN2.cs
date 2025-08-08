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
using NINA.MGEN;
using NINA.MGEN2.Commands;
using NINA.MGEN2.Commands.AppMode;
using NINA.MGEN2.Commands.CompatibilityMode;
using NINA.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.MGEN2 {

    public class MGEN : IMGEN {
        private DateTime lastCommandTime = DateTime.UtcNow;
        private readonly TimeSpan minimumCommandInterval = TimeSpan.FromMilliseconds(20);
        private IFTDI FTDI;
        private readonly SemaphoreSlim lockObj = new SemaphoreSlim(1, 1);

        private byte cbus_dir;
        private byte cbus_val;
        private string ftdiDllPath;
        private ILogger logger;

        public int SensorSizeX { get; } = 752;

        public int SensorSizeY { get; } = 582;

        public bool Connected => this.FTDI?.IsOpen ?? false;

        public double PixelSize => 4.85;

        /// <summary>
        ///
        /// </summary>
        /// <param name="ftdiDllPath">path to the ftd2xx dll</param>
        public MGEN(string ftdiDllPath, ILogger logger) {
            this.ftdiDllPath = ftdiDllPath;
            this.logger = logger;
        }

        public uint GetDriverVersion() {
            if (this.FTDI.GetDriverVersion(out var version) != FT_STATUS.FT_OK) {
                // throw exception
            }
            return version;
        }

        public async Task<ICollection<FT_DEVICE_INFO_NODE>> Scan(CancellationToken ct = default) {
            try {
                await lockObj.WaitAsync(ct);

                this.FTDI = new FTD2XX(this.ftdiDllPath, this.logger);
                var status = this.FTDI.GetDeviceList(out var ftdiDeviceList);
                if (status == FT_STATUS.FT_OK) {
                    return new List<FT_DEVICE_INFO_NODE>(ftdiDeviceList);
                } else {
                    // Device list exception
                    return null;
                }
            } finally {
                lockObj.Release();
            }
        }

        private async Task TurnOn(CancellationToken ct) {
            try {
                await lockObj.WaitAsync(ct);

                cbus_dir = (byte)(cbus_dir | (1 << 1));
                cbus_val = (byte)(cbus_val | (1 << 1));

                byte b = (byte)((cbus_dir << 4) + cbus_val);
                var status = this.FTDI.SetBitMode(b, 0x20);

                await Task.Delay(100, ct);

                cbus_val = (byte)(cbus_val & ~(1 << 1));
                b = (byte)((cbus_dir << 4) + cbus_val);
                status = this.FTDI.SetBitMode(b, 0x20);

                await Task.Delay(3000, ct);
            } finally {
                lockObj.Release();
            }
        }

        public Task DetectAndOpen(CancellationToken ct = default) {
            return Task.Run(() => DetectAndOpenInternal(ct));
        }

        private async Task DetectAndOpenInternal(CancellationToken ct) {
            var devices = await Scan(ct);
            if (!(devices?.Count > 0)) {
                throw new Exception("No MGEN device found");
            }

            var device = devices.FirstOrDefault(x => x?.Description?.ToLower()?.Contains("m-gen") ?? false);

            if (device != null) {
                await Open(device, ct);
                var query = await this.Send(new NoOpCommand(), ct, 1);
                var query2 = await this.Send(new NoOpCommand2(), ct, 1);
                if (query == null || (!query.Success && query2.Success)) {
                    await TurnOn(ct);
                }
            } else {
                throw new Exception("No MGEN device found");
            }

            await EnterNormalMode(ct);
            await Task.Delay(500);

            if (!await ValidateAppMode(ct)) {
                await Disconnect(ct);
                throw new NotImplementedException("MGEN is in Boot mode. This mode is not yet supported by this library");
            }
        }

        public async Task Open(FT_DEVICE_INFO_NODE device, CancellationToken ct = default) {
            try {
                await lockObj.WaitAsync(ct);
                var status = this.FTDI.Open(device);
                if (status == FT_STATUS.FT_OK) {
                    this.FTDI.SetTimeouts(1000, 1000);
                } else {
                    // Opening exception
                    return;
                }
            } finally {
                lockObj.Release();
            }
        }

        public async Task<bool> PressButton(MGENButton button, CancellationToken ct) {
            var command = new ButtonCommand(button);
            var response = await this.Send(command, ct);
            return response.Success;
        }

        public async Task<LEDState> ReadLEDState(CancellationToken ct = default) {
            var command = new GetLEDStatesCommand();
            var ledState = await this.Send(command, ct);

            var state = new LEDState() {
                TopLeft = ledState.LEDs.HasFlag(LEDS.BLUE) ? Color.Blue : Color.Transparent,
                TopRight = ledState.LEDs.HasFlag(LEDS.GREEN) ? Color.Green : Color.Transparent,
                Up = ledState.LEDs.HasFlag(LEDS.UP_RED) ? Color.Red : Color.Transparent,
                Down = ledState.LEDs.HasFlag(LEDS.DOWN_RED) ? Color.Red : Color.Transparent,
                Left = ledState.LEDs.HasFlag(LEDS.LEFT_RED) ? Color.Red : Color.Transparent,
                Right = ledState.LEDs.HasFlag(LEDS.RIGHT_RED) ? Color.Red : Color.Transparent
            };

            return state;
        }

        public async Task<Bitmap> ReadDisplay(Color primaryColor, Color backgroundColor, CancellationToken ct = default) {
            ReadDisplayCommand command;
            DisplayData chunk;
            byte[] rawDisplayData = new byte[1024];

            byte chunkSize = 128;
            var roundTrips = 1024 / chunkSize;
            for (var i = 0; i < roundTrips; i++) {
                command = new ReadDisplayCommand((ushort)(i * chunkSize), chunkSize);
                chunk = await this.Send(command, ct);
                Array.Copy(chunk.Data, 0, rawDisplayData, i * chunkSize, chunkSize);
            }

            BitArray displayBits = new BitArray(rawDisplayData);
            var width = 128; var height = 64;
            Bitmap display = new Bitmap(width, height);
            var index = 0;

            for (var startRow = 0; startRow < 64; startRow += 8) {
                for (var column = 0; column < 128; column++) {
                    for (var row = startRow; row < startRow + 8; row++) {
                        if (displayBits[index]) {
                            display.SetPixel(column, row, primaryColor);
                        } else {
                            display.SetPixel(column, row, backgroundColor);
                        }
                        index++;
                    }
                }
            }
            return display;
        }

        private async Task<bool> ValidateAppMode(CancellationToken ct) {
            var command = new NoOpCommand();
            var response = await this.Send(command, ct);
            return response.Success;
        }

        public async Task<bool> IsBootMode(CancellationToken ct = default) {
            var command = new QueryDeviceCommand();
            var response = await this.Send(command, ct);
            return response.IsBootMode;
        }

        public async Task<bool> EnterNormalMode(CancellationToken ct = default) {
            var command = new EnterNormalModeCommand();
            var result = await this.Send(command, ct);
            return result.Success;
        }

        public async Task<string> GetFirmwareVersion(CancellationToken ct = default) {
            var command = new FirmwareVersionCommand();
            var response = await this.Send(command, ct);
            return response.Version;
        }

        public async Task<ImagingParameter> GetImagingParameter(CancellationToken ct = default) {
            var command = new GetImagingParameterCommand();
            var parameter = await this.Send(command, ct);
            return new ImagingParameter(parameter.Gain, parameter.ExposureTime, parameter.Threshold);
        }

        public async Task<bool> SetImagingParameter(int gain, int exposureTime, int threshold, CancellationToken ct = default) {
            var command = new SetImagingParameterCommand((byte)gain, (ushort)exposureTime, (byte)threshold);
            var result = await this.Send(command, ct);
            return result.Success;
        }

        public async Task<bool> StartCamera(CancellationToken ct = default) {
            var command = new StartCameraCommand();
            await this.Send(command, ct);
            var result = await this.Send(command, ct);
            return result.Success;
        }

        public async Task<bool> StopCamera(CancellationToken ct = default) {
            var command = new StopCameraCommand();
            var result = await this.Send(command, ct);
            return result.Success;
        }

        public async Task<bool> StartCalibration(CancellationToken ct = default) {
            var command = new StartCalibrationCommand();
            var result = await this.Send(command, ct);
            return result.Success;
        }

        public async Task<bool> CancelCalibration(CancellationToken ct = default) {
            var command = new CancelCalibrationCommand();
            var result = await this.Send(command, ct);
            return result.Success;
        }

        public async Task<bool> IsGuidingActive(CancellationToken ct = default) {
            var command = new QueryCommand(QueryCommand.QueryCommandFlag.AutoguidingState);
            var result = await this.Send(command, ct);
            return result.AutoGuiderActive;
        }

        public async Task<bool> IsActivelyGuiding(CancellationToken ct = default) {
            var command = new QueryCommand(QueryCommand.QueryCommandFlag.All);
            var result = await this.Send(command, ct);
            return result.AutoGuiderActive && ((result.FrameInfo.FrameIndex & 0x40) != 0);
        }

        public async Task<bool> StartGuiding(CancellationToken ct = default) {
            var command = new StartGuidingCommand();
            var result = await this.Send(command, ct);
            return result.Success;
        }

        public async Task<bool> StopGuiding(CancellationToken ct = default) {
            var command = new StopGuidingCommand();
            var result = await this.Send(command, ct);
            return result.Success;
        }

        public async Task<GuideState> QueryGuideState(CancellationToken ct = default) {
            var command = new QueryCommand(QueryCommand.QueryCommandFlag.All);
            var result = await this.Send(command, ct);

            return new GuideState(result.AutoGuiderActive, result.FrameInfo);
        }

        public async Task<bool> Dither(CancellationToken ct = default) {
            var stateCommand = new GetDitherStateCommand();
            var state = await this.Send(stateCommand, ct);

            var command = new StartDitherCommand();
            _ = await this.Send(command, ct);

            stateCommand = new GetDitherStateCommand();
            state = await this.Send(stateCommand, ct);

            if (state.Dithering) {
                do {
                    await Task.Delay(TimeSpan.FromSeconds(1), ct);
                    stateCommand = new GetDitherStateCommand();
                    state = await this.Send(stateCommand, ct);
                } while (state.Dithering);
                //await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }
            return true;
        }

        public async Task<DitherAmplitude> GetDitherAmplitude(CancellationToken ct = default) {
            var ditherAmplitudeCommand = new GetDitherAmplitudeCommand();
            var ditherAmplitude = await this.Send(ditherAmplitudeCommand, ct);
            return new DitherAmplitude(ditherAmplitude.Amplitude);
        }

        public async Task<CalibrationStatusReport> QueryCalibration(CancellationToken ct = default) {
            var command = new QueryCalibrationCommand();
            var response = await this.Send(command, ct);
            return new CalibrationStatusReport(response.CalibrationStatus, response.Error);
        }

        public async Task Disconnect(CancellationToken ct = default) {
            try {
                await lockObj.WaitAsync(ct);

                if (Connected) {
                    var status = this.FTDI.Close();
                    this.FTDI = null;
                    if (status != FT_STATUS.FT_OK) {
                        // throw cannot disconnect exception
                        return;
                    }
                }
            } finally {
                lockObj.Release();
            }
        }

        public async Task<bool> SetNewGuidingPosition(StarData starDetail, CancellationToken ct = default) {
            var command = new SetNewGuidingPositionCommand(starDetail);
            var result = await this.Send(command, ct);
            return result.Success;
        }

        public async Task<StarData> GetStarData(byte starIndex, CancellationToken ct = default) {
            var command = new GetStarDataCommand(starIndex);
            var result = await this.Send(command, ct);
            return new StarData(result.PositionX, result.PositionY, result.Brightness, result.Pixels, result.Peak);
        }

        public async Task<int> StartStarSearch(int gain, int exposureTime, CancellationToken ct = default) {
            var command = new StartStarSearchCommand((byte)gain, (ushort)exposureTime);
            var result = await this.Send(command, ct);
            return result.NumberOfStars;
        }

        public Task<T> StartGenericCommand<T>(MGENCommand<T> command, CancellationToken ct = default) where T : IMGENResult {
            return this.Send(command, ct);
        }

        private async Task<TResult> Send<TResult>(IMGENCommand<TResult> command, CancellationToken ct, int retries = 3) where TResult : IMGENResult {
            try {
                await lockObj.WaitAsync(ct);

                if (Connected) {
                    var result = await Task.Run(() => Execute(command, retries));

                    lastCommandTime = DateTime.UtcNow;
                    return result;
                } else {
                    throw new Exception("MGEN is not connected!");
                }
            } finally {
                lockObj.Release();
            }
        }

        private TResult Execute<TResult>(IMGENCommand<TResult> command, int maxRetries) where TResult : IMGENResult {
            Exception exception = null;
            for (int i = 0; i < maxRetries; i++) {
                try {
                    var result = command.Execute(this.FTDI);
                    return result;
                } catch (UnexpectedReturnCodeException ex) {
                    // Check if the device fell back into compatibility mode
                    //var query = this.Execute(new QueryDeviceCommand(), 1);

                    exception = ex;
                    Thread.Sleep(50);
                } catch (Exception ex) {
                    exception = ex;
                    break;
                }
            }
            throw exception;
        }

        /// <summary>
        /// This method will return after a minimum of <see cref="minimumCommandInterval"/>} passed between the last command
        /// </summary>
        private async Task CommandCooldown(CancellationToken ct) {
            var lastCommandDeltaT = DateTime.UtcNow.Subtract(lastCommandTime);
            if (lastCommandDeltaT < minimumCommandInterval) {
                await Task.Delay(minimumCommandInterval - lastCommandDeltaT, ct);
            }
        }
    }
}