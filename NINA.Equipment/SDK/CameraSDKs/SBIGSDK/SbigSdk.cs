#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Equipment.SDK.CameraSDKs.SBIGSDK.SbigSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Threading;
using NINA.Core.Interfaces.Utility;

namespace NINA.Equipment.SDK.CameraSDKs.SBIGSDK {

    /// <summary>
    /// Encapsulates SBIG driver access primitives
    /// </summary>
    public class SbigSdk : ISbigSdk {
        private object driverLock = new object();
        private object readoutLock = new object();
        private IMicroCache<SBIG.QueryTemperatureStatusResults2> queryTemperatureStatusCache;

        private class ConnectedDevice {
            public DeviceInfo deviceInfo;
            public SBIG.StartExposureParams2 latestStartExposureParams;
            public SBIG.StartExposureParams2 latestStartTrackingExposureParams;
            public byte refCount = 0;
            public short handle;
        }

        private Dictionary<SBIG.DeviceType, ConnectedDevice> connectedDevices = new Dictionary<SBIG.DeviceType, ConnectedDevice>();

        /// <summary>
        /// An RAII contain for open driver handles that restores the previous driver instance on release, and additionally closes the driver instance if it owns (created) it
        /// </summary>
        private class DriverInstance : IDisposable {
            private readonly ISbigSdk sbigSdk;

            private readonly short previousHandle;

            public short Handle { get; private set; }

            private bool ownsHandle;

            private DriverInstance(ISbigSdk sbigSdk, short handle, bool ownsHandle, short previousHandle) {
                this.sbigSdk = sbigSdk;
                Handle = handle;
                this.ownsHandle = ownsHandle;
                this.previousHandle = previousHandle;
            }

            public static DriverInstance Create(ISbigSdk sbigSdk) {
                var driverHandleResult = sbigSdk.UnivDrvCommand<object, SBIG.GetDriverHandleResults>(SBIG.Cmd.CC_GET_DRIVER_HANDLE, null);
                var previousHandle = driverHandleResult.handle;
                sbigSdk.UnivDrvCommand<short>(SBIG.Cmd.CC_SET_DRIVER_HANDLE, -1);
                sbigSdk.UnivDrvCommand<object>(SBIG.Cmd.CC_OPEN_DRIVER, null);
                driverHandleResult = sbigSdk.UnivDrvCommand<object, SBIG.GetDriverHandleResults>(SBIG.Cmd.CC_GET_DRIVER_HANDLE, null);
                var handle = driverHandleResult.handle;
                bool ownsHandle = true;

                // The SBIG SDK seems to "optimize" by not actually creating a new instance if an already open instance doesn't have an open device. In this case, we can't close
                // on release
                if (handle == previousHandle) {
                    Logger.Trace($"SBIGSDK: New driver instance {handle} is the same as the already active one. Relinquishing ownership");
                    ownsHandle = false;
                } else {
                    Logger.Trace($"SBIGSDK: Created driver instance {handle}");
                }
                return new DriverInstance(sbigSdk, handle: handle, ownsHandle: ownsHandle, previousHandle: previousHandle);
            }

            public static DriverInstance GetExisting(ISbigSdk sbigSdk, short handle) {
                var driverHandleResult = sbigSdk.UnivDrvCommand<object, SBIG.GetDriverHandleResults>(SBIG.Cmd.CC_GET_DRIVER_HANDLE, null);
                var previousHandle = driverHandleResult.handle;
                Logger.Trace($"SBIGSDK: Setting existing driver instance {handle}");
                sbigSdk.UnivDrvCommand<short>(SBIG.Cmd.CC_SET_DRIVER_HANDLE, handle);
                return new DriverInstance(sbigSdk, handle: handle, ownsHandle: false, previousHandle: previousHandle);
            }

            public void ReleaseOwnership() {
                // This method is useful if you create a driver instance that successfully later opens a device, so we avoid closing on release
                this.ownsHandle = false;
            }

            public void Dispose() {
                if (ownsHandle) {
                    Logger.Trace($"SBIGSDK: Closing driver instance {this.Handle}");
                    SBIG.UnivDrvCommand(SBIG.Cmd.CC_CLOSE_DRIVER, null);
                    ownsHandle = false;
                }

                if (this.previousHandle != this.Handle) {
                    Logger.Trace($"SBIGSDK: Restoring previous handle {this.previousHandle}");
                    sbigSdk.UnivDrvCommand<short>(SBIG.Cmd.CC_SET_DRIVER_HANDLE, this.previousHandle);
                }
            }
        }

        public SbigSdk(IMicroCacheFactory microCacheFactory) {
            this.queryTemperatureStatusCache = microCacheFactory.Create<SBIG.QueryTemperatureStatusResults2>();
        }

        /// <summary>
        /// Gets a string representing the sdk file version
        /// </summary>
        /// <returns></returns>
        public string GetSdkVersion() {
            return SBIG.GetVersion().FileVersion;
        }

        /// <summary>
        /// Opens the device connected on the interface defined by deviceId
        /// </summary>
        /// <param name="deviceId">An enumeration representing the port and connection type for the device (ie, USB1, USB2, etc). The deviceId from QueryUsbDevices should be provided</param>
        /// <returns>Information about the opened device. Raises an exception if the device couldn't be opened</returns>
        public DeviceInfo OpenDevice(SBIG.DeviceType deviceId) {
            lock (driverLock) {
                Logger.Trace($"SBIGSDK: Opening device {deviceId}");

                ConnectedDevice connectedDevice;
                if (connectedDevices.TryGetValue(deviceId, out connectedDevice)) {
                    Logger.Trace($"SBIGSDK: Device {deviceId} already open, deviceRefCount={connectedDevice.refCount}");
                    ++connectedDevice.refCount;
                } else {
                    using (var driver = DriverInstance.Create(this)) {
                        UnivDrvCommand(SBIG.Cmd.CC_OPEN_DEVICE, new SBIG.OpenDeviceParams { deviceType = deviceId });
                        var linkResult = UnivDrvCommand<SBIG.EstablishLinkParams, SBIG.EstablishLinkResults>(SBIG.Cmd.CC_ESTABLISH_LINK, new SBIG.EstablishLinkParams());
                        CcdCameraInfo? cameraInfo = null;
                        CcdCameraInfo? trackingCameraInfo = null;
                        CcdCameraInfo? externalTrackingCameraInfo = null;
                        if (linkResult.cameraType != SBIG.CameraType.NoCamera) {
                            cameraInfo = GetCameraInfoAlreadyActive(SBIG.CCD.Imaging);
                            try {
                                trackingCameraInfo = GetCameraInfoAlreadyActive(SBIG.CCD.Tracking);
                            } catch (Exception) { }
                            if (trackingCameraInfo?.SupportsExternalTracker == true) {
                                try {
                                    externalTrackingCameraInfo = GetCameraInfoAlreadyActive(SBIG.CCD.ExternalTrackingInStxOrStl);
                                } catch (Exception) { }
                            }
                        }

                        var filterWheelInfo = GetFilterWheelInfoAlreadyActive();
                        connectedDevice = new ConnectedDevice() {
                            deviceInfo = new DeviceInfo() {
                                DeviceId = deviceId,
                                CameraType = linkResult.cameraType,
                                FilterWheelInfo = filterWheelInfo,
                                CameraInfo = cameraInfo,
                                TrackingCameraInfo = trackingCameraInfo,
                                ExternalTrackingCameraInfo = externalTrackingCameraInfo
                            },
                            handle = driver.Handle,
                            refCount = 1
                        };
                        connectedDevices.Add(deviceId, connectedDevice);
                        driver.ReleaseOwnership();
                    }
                }
                return connectedDevice.deviceInfo;
            }
        }

        private ConnectedDevice GetConnectedDevice(SBIG.DeviceType deviceId) {
            lock (driverLock) {
                ConnectedDevice result;
                if (connectedDevices.TryGetValue(deviceId, out result)) {
                    return result;
                } else {
                    return null;
                }
            }
        }

        private DriverInstance SetActiveDriver(SBIG.DeviceType deviceId) {
            lock (driverLock) {
                var connectedDevice = GetConnectedDevice(deviceId);
                if (connectedDevice != null) {
                    return DriverInstance.GetExisting(this, connectedDevice.handle);
                }
                return null;
            }
        }

        private DriverInstance EnsureActiveDriver(SBIG.DeviceType deviceId) {
            var activeDevice = SetActiveDriver(deviceId);
            if (activeDevice == null) {
                throw new Exception($"SBIG device {deviceId} is not connected");
            }
            return activeDevice;
        }

        /// <summary>
        /// Closes the connected device
        /// </summary>
        /// <param name="deviceId">The connected device to operate on</param>
        public void CloseDevice(SBIG.DeviceType deviceId) {
            lock (driverLock) {
                Logger.Trace($"SBIGSDK: Closing device {deviceId}");
                var connectedDevice = GetConnectedDevice(deviceId);
                if (connectedDevice == null) {
                    // No connected device, move on
                    Logger.Warning($"SBIGSDK: Can't close device {deviceId}, since it doesn't seem to be open");
                } else if (connectedDevice.refCount > 1) {
                    Logger.Trace($"SBIGSDK: Released device {deviceId}, deviceRefCount={connectedDevice.refCount}");
                    --connectedDevice.refCount;
                } else {
                    using (var driver = SetActiveDriver(deviceId)) {
                        SBIG.UnivDrvCommand(SBIG.Cmd.CC_CLOSE_DEVICE, null);
                        connectedDevices.Remove(deviceId);
                    }

                    Logger.Trace($"SBIGSDK: Closed device {deviceId}");
                }
            }
        }

        /// <summary>
        /// Converts a binary coded decimal to a native format. Every base-10 digit is encoded in 4 bits
        /// </summary>
        /// <param name="bcd">The binary coded decimal</param>
        /// <returns>The native uint converted</returns>
        public static uint BCDToUInt(uint bcd) {
            uint result = 0;
            uint multiplier = 1;
            for (int i = 0; i < 8; ++i) {
                var nextDigit = (bcd >> (4 * i)) & 0xF;
                result += nextDigit * multiplier;
                multiplier *= 10;
            }
            return result;
        }

        /// <summary>
        /// Gets a unified set of camera settings and information for the connected imaging device
        /// </summary>
        /// <param name="deviceId">The connected device to operate on</param>
        /// <param name="ccd">Type of CCD to connect to</param>
        /// <returns>A struct containing a union of all relevant camera information</returns>
        public CcdCameraInfo GetCameraInfo(SBIG.DeviceType deviceId, SBIG.CCD ccd) {
            lock (driverLock) {
                using (var driver = EnsureActiveDriver(deviceId)) {
                    return GetCameraInfoAlreadyActive(ccd);
                }
            }
        }

        /// <summary>
        /// Gets information about the connected filter wheel. If the device doesn't have a filter wheel, then this returns null
        /// </summary>
        /// <param name="deviceId">The connected device to operate on</param>
        /// <returns>A struct containing a union of all relevant filter wheel information, or null if there's no filter wheel on the requested device</returns>
        public FilterWheelInfo? GetFilterWheelInfo(SBIG.DeviceType deviceId) {
            lock (driverLock) {
                using (var driver = EnsureActiveDriver(deviceId)) {
                    return GetFilterWheelInfoAlreadyActive();
                }
            }
        }

        private FilterWheelInfo? GetFilterWheelInfoAlreadyActive() {
            try {
                var cfwParams = new SBIG.CfwParams() {
                    cfwModel = SBIG.CfwModelSelect.CFWSEL_AUTO,
                    cfwCommand = SBIG.CfwCommand.CFWC_GET_INFO,
                    cfwParam1 = (ushort)SBIG.CfwGetInfoSelect.CFWG_FIRMWARE_VERSION
                };
                var cfwResult = UnivDrvCommand<SBIG.CfwParams, SBIG.CfwResult>(SBIG.Cmd.CC_CFW, cfwParams);
                return new FilterWheelInfo() {
                    Model = cfwResult.cfwModel,
                    Position = cfwResult.cfwPosition,
                    Status = cfwResult.cfwStatus,
                    FirmwareVersion = cfwResult.cfwResult1,
                    FilterCount = cfwResult.cfwResult2
                };
            } catch (Exception) {
                // UnivDrvCommand throws a CE_BAD_PARAMETER error if there's no filter wheel on the device. This is unfortunately the only way to tell if there's a FW
                return null;
            }
        }

        /// <summary>
        /// Gets camera info for a device that's already active. This is useful during initial device connection
        /// </summary>
        /// <param name="ccd">Type of CCD to connect to</param>
        /// <returns>A struct containing a union of all relevant camera information</returns>
        private CcdCameraInfo GetCameraInfoAlreadyActive(SBIG.CCD ccd) {
            var cameraInfo = new CcdCameraInfo();
            var ccdInfo01Type = ccd == SBIG.CCD.Imaging ? SBIG.CcdInfoRequest.ImagingCcdStandard : SBIG.CcdInfoRequest.TrackingCcdStandard;
            var ccdInfo01 = UnivDrvCommand<SBIG.GetCcdInfoParams, SBIG.GetCcdInfoResults01>(
                SBIG.Cmd.CC_GET_CCD_INFO, new SBIG.GetCcdInfoParams(ccdInfo01Type));
            var ccdInfo2 = UnivDrvCommand<SBIG.GetCcdInfoParams, SBIG.GetCcdInfoResults2>(
                SBIG.Cmd.CC_GET_CCD_INFO, new SBIG.GetCcdInfoParams(SBIG.CcdInfoRequest.CameraInfoExtended));
            var ccdInfo45Type = ccd == SBIG.CCD.Imaging ? SBIG.CcdInfoRequest.ImagingCcdSecondaryExtended : SBIG.CcdInfoRequest.TrackingCcdSecondaryExtended;
            var ccdInfo45 = UnivDrvCommand<SBIG.GetCcdInfoParams, SBIG.GetCcdInfoResults45>(
                SBIG.Cmd.CC_GET_CCD_INFO, new SBIG.GetCcdInfoParams(ccdInfo45Type));
            var ccdInfo6 = UnivDrvCommand<SBIG.GetCcdInfoParams, SBIG.GetCcdInfoResults6>(
                SBIG.Cmd.CC_GET_CCD_INFO, new SBIG.GetCcdInfoParams(SBIG.CcdInfoRequest.CcdCameraExtended));

            // Info from GetCcdInfoResults01
            cameraInfo.FirmwareVersion = $"{ccdInfo01.firmwareVersion >> 8}.{ccdInfo01.firmwareVersion & 0xFF}";
            cameraInfo.CameraType = ccdInfo01.cameraType;
            cameraInfo.Name = ccdInfo01.name;
            cameraInfo.ReadoutModeConfigs = new ReadoutModeConfig[ccdInfo01.readoutModeCount];
            for (int i = 0; i < ccdInfo01.readoutModeCount; ++i) {
                var rawMode = ccdInfo01.readoutInfo[i].mode;
                var readoutMode = new ReadoutMode() {
                    RawMode = rawMode,
                    Mode = (SBIG.ReadoutMode)(rawMode & 0xFF)
                };

                int binX = 0, binY = 0;
                bool binOffChip = false;
                // The raw binning mode is encoded in 2-bytes. The least significant corresponds to the SBIG.ReadoutMode enum, and the most significant is an optional
                // one containing a parameter, if relevant. For example "BinNx1" requires the higher byte set for N, as does "BinNxN"
                switch (readoutMode.Mode) {
                    case SBIG.ReadoutMode.NoBinning:
                        binX = binY = 1;
                        break;

                    case SBIG.ReadoutMode.Bin2x2:
                        binX = binY = 2;
                        break;

                    case SBIG.ReadoutMode.Bin3x3:
                        binX = binY = 3;
                        break;

                    case SBIG.ReadoutMode.BinNx1:
                        binX = 1;
                        break;

                    case SBIG.ReadoutMode.BinNx2:
                        binX = 2;
                        break;

                    case SBIG.ReadoutMode.BinNx3:
                        binX = 3;
                        break;

                    case SBIG.ReadoutMode.NoBinning2:
                        binX = binY = 1;
                        binOffChip = true;
                        break;

                    case SBIG.ReadoutMode.Bin2x2VertOffChip:
                        binX = binY = 2;
                        binOffChip = true;
                        break;

                    case SBIG.ReadoutMode.Bin3x3VertOffChip:
                        binX = binY = 3;
                        binOffChip = true;
                        break;

                    case SBIG.ReadoutMode.Bin9x9:
                        binX = binY = 9;
                        break;

                    case SBIG.ReadoutMode.BinNxN:
                        break;
                }
                cameraInfo.ReadoutModeConfigs[i].ReadoutMode = readoutMode;
                cameraInfo.ReadoutModeConfigs[i].BinX = binX;
                cameraInfo.ReadoutModeConfigs[i].BinY = binY;
                cameraInfo.ReadoutModeConfigs[i].BinningOffChip = binOffChip;
                cameraInfo.ReadoutModeConfigs[i].ElectronsPerAdu = BCDToUInt(ccdInfo01.readoutInfo[i].gain) / 100.0d;
                cameraInfo.ReadoutModeConfigs[i].Width = ccdInfo01.readoutInfo[i].width;
                cameraInfo.ReadoutModeConfigs[i].Height = ccdInfo01.readoutInfo[i].height;
                cameraInfo.ReadoutModeConfigs[i].PixelWidthMicrons = BCDToUInt(ccdInfo01.readoutInfo[i].pixelWidth) / 100.0d;
                cameraInfo.ReadoutModeConfigs[i].PixelHeightMicrons = BCDToUInt(ccdInfo01.readoutInfo[i].pixelHeight) / 100.0d;
            }

            // Info from GetCcdInfoResults2
            var numBadColumns = Math.Min((ushort)4, ccdInfo2.badColumns);
            cameraInfo.BadColumns = new ushort[numBadColumns];
            for (int i = 0; i < numBadColumns; ++i) {
                cameraInfo.BadColumns[i] = ccdInfo2.columns[i];
            }
            cameraInfo.HasAntiBloomingGateProtection = ccdInfo2.imagingABG == SBIG.AntiBloomingGatePresence.HasABG;
            cameraInfo.SerialNumber = ccdInfo2.serialNumber;

            // Info from GetCcdInfoResults45
            cameraInfo.CcdFrameType = (ccdInfo45.capabilitiesBits & 0x1) == 0x1 ? CcdFrameType.FRAME_TRANSFER_CCD : CcdFrameType.FULL_FRAME_CCD;
            cameraInfo.HasElectronicShutter = (ccdInfo45.capabilitiesBits & 0x2) == 0x2;
            cameraInfo.SupportsExternalTracker = (ccdInfo45.capabilitiesBits & 0x4) == 0x4;
            cameraInfo.SupportsBTDI = (ccdInfo45.capabilitiesBits & 0x8) == 0x8;
            cameraInfo.HasAO8 = (ccdInfo45.capabilitiesBits & 0x10) == 0x10;
            cameraInfo.HasFrameBuffer = (ccdInfo45.capabilitiesBits & 0x20) == 0x20;
            cameraInfo.RequiresStartExposure2 = (ccdInfo45.capabilitiesBits & 0x40) == 0x40;
            cameraInfo.NumberExtraUnbinnedRows = ccdInfo45.dumpExtra;

            // Info from GetCcdInfoResults6
            cameraInfo.IsSTXL = (ccdInfo6.cameraBits & 0x1) == 0x1;
            cameraInfo.HasMechanicalShutter = (ccdInfo6.cameraBits & 0x2) == 0x2;
            if ((ccdInfo6.ccdBits & 0x1) == 0) {
                cameraInfo.CcdType = CcdType.MONO;
            } else if ((ccdInfo6.ccdBits & 0x2) == 0) {
                cameraInfo.CcdType = CcdType.BAYER_MATRIX;
            } else {
                cameraInfo.CcdType = CcdType.TRUSENSE_COLOR_MATRIX;
            }

            try {
                // This will only work for PixCel255/237 devices. Otherwise it is always 16-bit ADC
                var ccdInfo3 = UnivDrvCommand<SBIG.GetCcdInfoParams, SBIG.GetCcdInfoResults3>(
                    SBIG.Cmd.CC_GET_CCD_INFO, new SBIG.GetCcdInfoParams(SBIG.CcdInfoRequest.PixCel255_237Extended));
                if (ccdInfo3.a2dSize == SBIG.A2dSize.SixteenBits) {
                    cameraInfo.AdcBits = 16;
                } else if (ccdInfo3.a2dSize == SBIG.A2dSize.TwelveBits) {
                    cameraInfo.AdcBits = 12;
                } else {
                    Logger.Warning($"SBIGSDK: Unknown a2dSize reported by GetCcdInfoResults3. Assuming 16 bit ADC");
                    cameraInfo.AdcBits = 16;
                }
                cameraInfo.FilterType = ccdInfo3.filterType;
            } catch (SBIG.FailedOperation) {
                Logger.Info($"SBIGSDK: Failed to GetCcdInfoResults3. Device likely isn't Pixcel255/237. Assuming 16-bit ADC");
                // All other cameras are 16 bits
                cameraInfo.AdcBits = 16;
            }
            return cameraInfo;
        }

        /// <summary>
        /// Convenience method to extract a given ReadoutModeConfig from an array given the desired SBIG.ReadoutMode
        /// </summary>
        /// <param name="modeConfigs">The readout mode configurations to search</param>
        /// <param name="readoutMode">The SBIG readout mode to match</param>
        /// <returns>Null if the config isn't found, the first match otherwise</returns>
        public static ReadoutModeConfig? GetReadoutModeConfig(ReadoutModeConfig[] modeConfigs, SBIG.ReadoutMode readoutMode) {
            for (int i = 0; i < modeConfigs.Length; ++i) {
                if (modeConfigs[i].ReadoutMode.Mode == readoutMode) {
                    return modeConfigs[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Queries temperature status from the connected CCD camera. This uses a thread-safe cache with a 1-second TTL to simplify accessing the temperature information
        /// without overwhelming the device with one request per property per device poll
        /// </summary>
        /// <param name="deviceId">The connected device to operate on</param>
        /// <returns>The queried temperature status</returns>
        public SBIG.QueryTemperatureStatusResults2 QueryTemperatureStatus(SBIG.DeviceType deviceId) {
            return this.queryTemperatureStatusCache.GetOrAdd(
                deviceId.ToString(),
                () => {
                    using (var driver = EnsureActiveDriver(deviceId)) {
                        return UnivDrvCommand<SBIG.QueryTemperatureStatusParams, SBIG.QueryTemperatureStatusResults2>(
                            SBIG.Cmd.CC_QUERY_TEMPERATURE_STATUS,
                            new SBIG.QueryTemperatureStatusParams(SBIG.TempStatusRequest.TEMP_STATUS_ADVANCED2));
                    }
                },
                TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// Wraps the uber-method that issues a command to the universal SBIG driver. It uses a lock to ensure only one call is made at a time, which is required per the SDK documentation
        /// </summary>
        /// <param name="command">The command</param>
        /// <param name="Params">The input parameters. This should be IntPtr.Zero if the command doesn't take input, or a pinned GC handle to a memory location otherwise</param>
        /// <param name="results">Where to directly write the command output. This should be IntPtr.Zero if the command doesn't write output, or a pinned GC handle to a memory location otherwise</param>
        public void UnivDrvCommandDirect(SBIG.Cmd command, IntPtr Params, IntPtr results) {
            lock (driverLock) {
                SBIG.UnivDrvCommandDirect(command, Params, results);
            }
        }

        /// <summary>
        /// A convenience wrapper for UnivDrvCommand that passes input parameters directly if they are blittable (or an array of blittables), or marshals to a pinned GC handle otherwise.
        /// No return value is provided, so use this only for commands that don't return a result structure
        /// </summary>
        /// <typeparam name="P">The input parameter type</typeparam>
        /// <param name="command">The command</param>
        /// <param name="Params">The input parameters as a native CLR type</param>
        public void UnivDrvCommand<P>(SBIG.Cmd command, P Params) {
            lock (driverLock) {
                var paramsBlittable = (Params == null) || Blittable<P>.IsBlittable;
                if (paramsBlittable) {
                    SBIG.UnivDrvCommand(command, Params);
                } else {
                    SBIG.UnivDrvCommandMarshal(command, Params);
                }
            }
        }

        /// <summary>
        /// Another convenience wrapper for UnivDryCommand that returns data. If both input and output are blittable, then a more efficient version of UnivDrvCommand is used which doesn't
        /// marshal the data
        /// </summary>
        /// <typeparam name="P">The input parameter type</typeparam>
        /// <typeparam name="R">The output parameter type</typeparam>
        /// <param name="command">The command</param>
        /// <param name="Params">The input parameter as a native CLR type</param>
        /// <param name="pResults">The output parameter will be written directly to the given instance</param>
        public void UnivDrvCommand<P, R>(SBIG.Cmd command, P Params, R pResults) where R : new() {
            lock (driverLock) {
                if (Blittable<P>.IsBlittable && Blittable<R>.IsBlittable) {
                    SBIG.UnivDrvCommand(command, Params, pResults);
                } else if (Blittable<P>.IsBlittable) {
                    SBIG.UnivDrvCommand_OutComplex(command, Params, pResults);
                } else {
                    throw new NotSupportedException("Cannot use this version of UnivDrvCommand when both Params nor pResults aren't blittable. Use the other signature");
                }
            }
        }

        /// <summary>
        /// Yet another convenience wrapper similar, except a new instance of the return data are provided instead of writing inline to an existing one
        /// </summary>
        /// <typeparam name="P">The input parameter type</typeparam>
        /// <typeparam name="R">The output parameter type</typeparam>
        /// <param name="command">The command</param>
        /// <param name="Params">The input parameter as a native CLR type</param>
        /// <returns>The command output</returns>
        public R UnivDrvCommand<P, R>(SBIG.Cmd command, P Params) where R : new() {
            lock (driverLock) {
                var paramsBlittable = (Params == null) || Blittable<P>.IsBlittable;
                if (paramsBlittable && Blittable<R>.IsBlittable) {
                    return SBIG.UnivDrvCommand<R>(command, Params);
                } else if (paramsBlittable) {
                    var result = new R();
                    SBIG.UnivDrvCommand_OutComplex(command, Params, result);
                    return result;
                } else {
                    return SBIG.UnivDrvCommandMarshal<R>(command, Params);
                }
            }
        }

        /// <summary>
        /// Queries all cameras connected on the USB bus
        /// </summary>
        /// <returns>An array containing all connected cameras. This will be empty if there are no cameras.</returns>
        public DeviceQueryInfo[] QueryUsbDevices() {
            lock (driverLock) {
                using (var driver = DriverInstance.Create(this)) {
                    var queryResults = UnivDrvCommand<object, SBIG.QueryUsbResults>(SBIG.Cmd.CC_QUERY_USB, (object)null);

                    // Queried USB devices excludes already connected devices, so we have to manually append them to the list so devices that comprise multiple equipment can
                    // be independently connected
                    var queryInfos = new DeviceQueryInfo[queryResults.camerasFound + connectedDevices.Count];
                    for (int i = 0; i < queryResults.camerasFound; i++) {
                        queryInfos[i].CameraType = queryResults.dev[i].cameraType;
                        queryInfos[i].Name = queryResults.dev[i].name;
                        queryInfos[i].SerialNumber = queryResults.dev[i].serialNumber;
                        queryInfos[i].DeviceId = (SBIG.DeviceType)((ushort)SBIG.DeviceType.USB1 + i);
                        DeviceInfo? connectedDevice;
                        try {
                            connectedDevice = OpenDevice(queryInfos[i].DeviceId);
                            queryInfos[i].FilterWheelInfo = connectedDevice?.FilterWheelInfo;
                        } finally {
                            CloseDevice(queryInfos[i].DeviceId);
                        }
                    }
                    int j = queryResults.camerasFound;
                    foreach (var connectedDevice in connectedDevices.Values) {
                        queryInfos[j].CameraType = connectedDevice.deviceInfo.CameraType;
                        queryInfos[j].Name = connectedDevice.deviceInfo.CameraInfo.Value.Name;
                        queryInfos[j].SerialNumber = connectedDevice.deviceInfo.CameraInfo.Value.SerialNumber;
                        queryInfos[j].DeviceId = connectedDevice.deviceInfo.DeviceId;
                        queryInfos[j].FilterWheelInfo = connectedDevice.deviceInfo.FilterWheelInfo;
                        ++j;
                    }

                    foreach (var queryInfo in queryInfos) {
                        Logger.Trace($"SBIGSDK: Queried device - {queryInfo}");
                    }

                    return queryInfos;
                }
            }
        }

        /// <summary>
        /// Enables temperature regulation at the given set point. If temperature regulation is already enabled, it updates the set point
        /// </summary>
        /// <param name="deviceId">The connected device to operate on</param>
        /// <param name="ccd">The CCD to use for the exposure</param>
        /// <param name="celcius">The set point</param>
        public void RegulateTemperature(SBIG.DeviceType deviceId, SBIG.CCD ccd, double celcius) {
            lock (driverLock) {
                using (var driver = EnsureActiveDriver(deviceId)) {
                    var connectedDevice = GetConnectedDevice(deviceId);
                    var serialNumber = connectedDevice.deviceInfo.CameraInfo.Value.SerialNumber;

                    // CONSIDER: AutoFreeze is an option for guiding cameras to reduce readout noise. It would be easy to support as an equipment option, but probably
                    // won't be needed for regular imaging
                    // NOTE: There doesn't seem to be a way in the SDK to differentiate between tracking and CCD regulation
                    var tempRegulationParams = new SBIG.SetTemperatureRegulationParams2 {
                        state = SBIG.TemperatureRegulation.On,
                        ccdSetpointCelcius = celcius,
                    };
                    UnivDrvCommand(SBIG.Cmd.CC_SET_TEMPERATURE_REGULATION2, tempRegulationParams);
                    this.queryTemperatureStatusCache.Remove(serialNumber);
                }
            }
        }

        /// <summary>
        /// Disables temperature regulation if it is enabled, or does nothing otherwise
        /// </summary>
        /// <param name="deviceId">The connected device to operate on</param>
        /// <param name="ccd">The CCD to use for the exposure</param>
        public void DisableTemperatureRegulation(SBIG.DeviceType deviceId, SBIG.CCD ccd) {
            SetTemperatureRegulationState(deviceId, ccd, SBIG.TemperatureRegulation.Off);
        }

        /// <summary>
        /// Gets the exposure state (Idle/In Progress/Complete), which is useful for polling to determine when to download the exposure
        /// </summary>
        /// <param name="deviceId">The connected device to operate on</param>
        /// <param name="ccd">The CCD to use for the exposure</param>
        /// <returns>A CommandState representing the exposure state</returns>
        public CommandState GetExposureState(SBIG.DeviceType deviceId, SBIG.CCD ccd) {
            lock (driverLock) {
                using (var driver = EnsureActiveDriver(deviceId)) {
                    var status = UnivDrvCommand<SBIG.QueryCommandStatusParams, SBIG.QueryCommandStatusResults>(
                        SBIG.Cmd.CC_QUERY_COMMAND_STATUS,
                        new SBIG.QueryCommandStatusParams(SBIG.Cmd.CC_START_EXPOSURE));
                    int rawState;
                    if (ccd == SBIG.CCD.Imaging) {
                        rawState = ((ushort)status.status & 0x3);
                    } else {
                        rawState = ((ushort)status.status & 0xF) >> 2;
                    }

                    // The 2 least significant bits represent Ccd camera status. Bit 0 is "Complete" if set, and "In Progress" otherwise. Bit 1 is "Command Active" if set, and "Idle" otherwise.
                    if ((rawState & 0x2) == 0) {
                        return CommandState.IDLE;
                    } else if ((rawState & 0x1) == 0) {
                        return CommandState.IN_PROGRESS;
                    } else {
                        return CommandState.COMPLETE;
                    }
                }
            }
        }

        /// <summary>
        /// Starts an exposure. EndExposure must be called to either abort, or after the exposure is complete (use GetExposureState)
        /// </summary>
        /// <param name="deviceId">The connected device to operate on</param>
        /// <param name="ccd">The CCD to use for the exposure</param>
        /// <param name="readoutMode">The readout mode</param>
        /// <param name="darkFrame">Whether this is a dark frame. This will be used to close/open the internal shutter if one is available</param>
        /// <param name="exposureTimeSecs">Exposure time in seconds. The SDK supports hundredths of a second, so any additional precision is ignored</param>
        /// <param name="exposureStart">The X,Y coordinates representing a bounding box for the exposure. Unless ROI is used, this should be 0, 0</param>
        /// <param name="exposureSize">The width, height of a bounding box for the exposure. This should account for the amount of binning represented in the readout mode</param>
        public void StartExposure(
            SBIG.DeviceType deviceId,
            SBIG.CCD ccd,
            ReadoutMode readoutMode,
            bool darkFrame,
            double exposureTimeSecs,
            Point exposureStart,
            Size exposureSize) {
            Logger.Trace($"SBIGSDK: StartExposure DeviceId={deviceId}, Ccd={ccd}");
            lock (driverLock) {
                var connectedDevice = GetConnectedDevice(deviceId);
                using (var driver = EnsureActiveDriver(deviceId)) {
                    var targetShutterState = darkFrame ? SBIG.ShutterState.Close : SBIG.ShutterState.Open;
                    var exposureTimeHundredths = (uint)Math.Round(exposureTimeSecs * 100.0f);
                    var exposureParams = new SBIG.StartExposureParams2() {
                        ccd = ccd,
                        readoutMode = readoutMode.RawMode,
                        abgState = SBIG.AbgState.Off,
                        openShutter = targetShutterState,
                        exposureTime = exposureTimeHundredths,
                        top = (ushort)exposureStart.Y,
                        left = (ushort)exposureStart.X,
                        height = (ushort)exposureSize.Height,
                        width = (ushort)exposureSize.Width
                    };

                    UnivDrvCommand(SBIG.Cmd.CC_START_EXPOSURE2, exposureParams);
                    if (ccd == SBIG.CCD.Imaging) {
                        connectedDevice.latestStartExposureParams = exposureParams;
                    } else {
                        connectedDevice.latestStartTrackingExposureParams = exposureParams;
                    }
                }
            }
        }

        /// <summary>
        /// Ends an exposure. This must be called before downloading an exposure (readout). This method is also used for aborting an exposure
        /// </summary>
        /// <param name="deviceId">The connected device to operate on</param>
        /// <param name="ccd">The CCD to use for the exposure</param>
        public void EndExposure(SBIG.DeviceType deviceId, SBIG.CCD ccd) {
            Logger.Trace($"SBIGSDK: EndExposure DeviceId={deviceId}, Ccd={ccd}");
            lock (driverLock) {
                using (var driver = EnsureActiveDriver(deviceId)) {
                    UnivDrvCommand(SBIG.Cmd.CC_END_EXPOSURE, new SBIG.EndExposureParams(ccd));
                }
            }
        }

        /// <summary>
        /// Downloads an exposure. EndExposure must have been called already
        /// </summary>
        /// <param name="deviceId">The connected device to operate on</param>
        /// <param name="ccd">The CCD to use for the exposure</param>
        /// <param name="ct">The cancellation token</param>
        /// <returns>Object containing flat array of 16-bit pixel data and other metadata</returns>
        public SBIGExposureData DownloadExposure(SBIG.DeviceType deviceId, SBIG.CCD ccd, CancellationToken ct) {
            Logger.Trace($"SBIGSDK: Start DownloadExposure DeviceId={deviceId}, Ccd={ccd}");
            lock (readoutLock) {
                var connectedDevice = GetConnectedDevice(deviceId);
                var exposureParams = ccd == SBIG.CCD.Imaging ? connectedDevice.latestStartExposureParams : connectedDevice.latestStartTrackingExposureParams;
                using (var driver = EnsureActiveDriver(deviceId)) {
                    var pinnedParams = default(GCHandle);
                    var dataGcHandle = default(GCHandle);
                    var tempStatus = QueryTemperatureStatus(deviceId);
                    bool coolingEnabled = tempStatus.coolingEnabled > 0;
                    if (coolingEnabled) {
                        SetTemperatureRegulationState(deviceId, ccd, SBIG.TemperatureRegulation.Freeze);
                    }

                    var readoutParams = new SBIG.StartReadoutParams() {
                        ccd = ccd,
                        readoutMode = exposureParams.readoutMode,
                        left = exposureParams.left,
                        top = exposureParams.top,
                        width = exposureParams.width,
                        height = exposureParams.height,
                    };

                    ushort[] data = null;
                    try { 
                        UnivDrvCommand(SBIG.Cmd.CC_START_READOUT, readoutParams);

                        data = new ushort[exposureParams.width * exposureParams.height];
                        dataGcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                        var pinnedPtr = dataGcHandle.AddrOfPinnedObject();
                        var readoutLineParams = new SBIG.ReadoutLineParams() {
                            ccd = ccd,
                            pixelStart = exposureParams.left,
                            pixelLength = exposureParams.width,
                            readoutMode = exposureParams.readoutMode
                        };

                        pinnedParams = GCHandle.Alloc(readoutLineParams, GCHandleType.Pinned);
                        for (int i = 0; i < exposureParams.height; i++) {
                            if (ct.IsCancellationRequested) {
                                throw new OperationCanceledException();
                            }
                            UnivDrvCommandDirect(SBIG.Cmd.CC_READOUT_LINE, pinnedParams.AddrOfPinnedObject(), pinnedPtr + (i * exposureParams.width * sizeof(ushort)));
                        }
                    } catch (Exception e) {
                        Logger.Error("SBIGSDK: DownloadExposure failed", e);
                        throw;
                    } finally {
                        Logger.Trace($"SBIGSDK: End DownloadExposure DeviceId={deviceId}, Ccd={ccd}");
                        if (pinnedParams != default(GCHandle)) {
                            pinnedParams.Free();
                        }
                        if (dataGcHandle != default(GCHandle)) {
                            dataGcHandle.Free();
                        }

                        try {
                            UnivDrvCommand(SBIG.Cmd.CC_END_READOUT, readoutParams);
                        } catch (Exception e) {
                            Logger.Error($"SBIGSDK: Failed to end readout for {deviceId} {ccd}", e);
                        }

                        try {
                            if (coolingEnabled) {
                                SetTemperatureRegulationState(deviceId, ccd, SBIG.TemperatureRegulation.Unfreeze);
                            }
                        } catch (Exception e) {
                            Logger.Error($"SBIGSDK: Failed to unfreeze TEC {deviceId} {ccd}", e);
                        }
                    }

                    return new SBIGExposureData() {
                        Data = data,
                        Width = exposureParams.width,
                        Height = exposureParams.height
                    };
                }
            }
        }

        public void SetFilterWheelPosition(SBIG.DeviceType deviceId, ushort position) {
            lock (driverLock) {
                using (var device = EnsureActiveDriver(deviceId)) {
                    var cfwParams = new SBIG.CfwParams() {
                        cfwModel = SBIG.CfwModelSelect.CFWSEL_AUTO,
                        cfwCommand = SBIG.CfwCommand.CFWC_GOTO,
                        cfwParam1 = (uint)position
                    };
                    UnivDrvCommand<SBIG.CfwParams, SBIG.CfwResult>(SBIG.Cmd.CC_CFW, cfwParams);
                }
            }
        }

        public FilterWheelStatus GetFilterWheelStatus(SBIG.DeviceType deviceId) {
            lock (driverLock) {
                using (var device = EnsureActiveDriver(deviceId)) {
                    var queryStatusParams = new SBIG.CfwParams() {
                        cfwModel = SBIG.CfwModelSelect.CFWSEL_AUTO,
                        cfwCommand = SBIG.CfwCommand.CFWC_QUERY
                    };
                    var queryStatusResult = UnivDrvCommand<SBIG.CfwParams, SBIG.CfwResult>(SBIG.Cmd.CC_CFW, queryStatusParams);

                    Logger.Trace($"SBIGSDK: FilterWheel status. Position={queryStatusResult.cfwPosition}, Status={queryStatusResult.cfwStatus}");
                    return new FilterWheelStatus() {
                        Position = queryStatusResult.cfwPosition,
                        Status = queryStatusResult.cfwStatus
                    };
                }
            }
        }

        private void SetTemperatureRegulationState(SBIG.DeviceType deviceId, SBIG.CCD ccd, SBIG.TemperatureRegulation state) {
            lock (driverLock) {
                using (var driver = EnsureActiveDriver(deviceId)) {
                    var connectedDevice = GetConnectedDevice(deviceId);
                    var serialNumber = connectedDevice.deviceInfo.CameraInfo.Value.SerialNumber;
                    var tempRegulationParams = new SBIG.SetTemperatureRegulationParams2 {
                        state = state
                    };
                    UnivDrvCommand(SBIG.Cmd.CC_SET_TEMPERATURE_REGULATION2, tempRegulationParams);
                    this.queryTemperatureStatusCache.Remove(serialNumber);
                }
            }
        }
    }
}