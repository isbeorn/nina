#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.SDK.CameraSDKs.SBIGSDK.SbigSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Threading;

namespace NINA.Equipment.SDK.CameraSDKs.SBIGSDK {

    public struct DeviceQueryInfo {
        public SBIG.CameraType CameraType;
        public string Name;
        public string SerialNumber;
        public SBIG.DeviceType DeviceId;
        public FilterWheelInfo? FilterWheelInfo;

        public override string ToString() {
            return $"{{{nameof(CameraType)}={CameraType}, {nameof(Name)}={Name}, {nameof(SerialNumber)}={SerialNumber}, {nameof(DeviceId)}={DeviceId}, {nameof(FilterWheelInfo)}={FilterWheelInfo}}}";
        }
    }

    public struct DeviceInfo {
        public SBIG.DeviceType DeviceId;
        public SBIG.CameraType CameraType;
        public CcdCameraInfo? CameraInfo;
        public CcdCameraInfo? TrackingCameraInfo;
        public CcdCameraInfo? ExternalTrackingCameraInfo;
        public FilterWheelInfo? FilterWheelInfo;

        public override string ToString() {
            return $"{{{nameof(DeviceId)}={DeviceId}, {nameof(CameraType)}={CameraType}, {nameof(CameraInfo)}={CameraInfo}, {nameof(TrackingCameraInfo)}={TrackingCameraInfo}, {nameof(ExternalTrackingCameraInfo)}={ExternalTrackingCameraInfo}, {nameof(FilterWheelInfo)}={FilterWheelInfo}}}";
        }
    }

    public struct ReadoutMode {
        public ushort RawMode;
        public SBIG.ReadoutMode Mode;

        public static ReadoutMode Create(int binning) {
            if (binning < 1 || binning > 3) {
                throw new NotSupportedException($"Only binning modes 1-3 are supported");
            }

            SBIG.ReadoutMode readoutMode;
            if (binning == 1) {
                readoutMode = SBIG.ReadoutMode.NoBinning;
            } else if (binning == 2) {
                readoutMode = SBIG.ReadoutMode.Bin2x2;
            } else {
                readoutMode = SBIG.ReadoutMode.Bin3x3;
            }

            // If other binning modes are supported, then the higher bits of RawMode will be set as well
            return new ReadoutMode() {
                RawMode = (ushort)readoutMode,
                Mode = readoutMode
            };
        }
    }

    public struct ReadoutModeConfig {
        public ReadoutMode ReadoutMode;
        public int BinX;
        public int BinY;
        public bool BinningOffChip;
        public ushort Width;
        public ushort Height;
        public double ElectronsPerAdu;
        public double PixelWidthMicrons;
        public double PixelHeightMicrons;

        public override string ToString() {
            return $"{{{nameof(ReadoutMode)}={ReadoutMode}, {nameof(BinX)}={BinX}, {nameof(BinY)}={BinY}, {nameof(BinningOffChip)}={BinningOffChip}, {nameof(Width)}={Width}, {nameof(Height)}={Height}, {nameof(ElectronsPerAdu)}={ElectronsPerAdu}, {nameof(PixelWidthMicrons)}={PixelWidthMicrons}, {nameof(PixelHeightMicrons)}={PixelHeightMicrons}}}";
        }
    }

    public enum SBIG_CAMERA_STATE {
        IDLE = 0,
        EXPOSING,
        DOWNLOADING,
        ERROR
    };

    public enum CcdFrameType {
        FULL_FRAME_CCD,
        FRAME_TRANSFER_CCD
    }

    public enum CcdType {
        MONO,
        BAYER_MATRIX,
        TRUSENSE_COLOR_MATRIX
    }

    public enum CommandState {
        IDLE,
        IN_PROGRESS,
        COMPLETE
    }

    public struct CcdCameraInfo {
        public string FirmwareVersion;
        public SBIG.CameraType CameraType;
        public string Name;
        public ReadoutModeConfig[] ReadoutModeConfigs;
        public ushort[] BadColumns;
        public bool HasAntiBloomingGateProtection;
        public string SerialNumber;
        public ushort AdcBits;
        public SBIG.FilterType FilterType;
        public CcdFrameType CcdFrameType;
        public bool HasElectronicShutter;
        public bool SupportsExternalTracker;
        public bool SupportsBTDI;
        public bool HasAO8;
        public bool HasFrameBuffer;
        public bool RequiresStartExposure2;
        public ushort NumberExtraUnbinnedRows;
        public bool IsSTXL;
        public bool HasMechanicalShutter;
        public CcdType CcdType;

        public override string ToString() {
            return $"{nameof(FirmwareVersion)}={FirmwareVersion}, {nameof(CameraType)}={CameraType}, {nameof(Name)}={Name}, {nameof(ReadoutModeConfigs)}={ReadoutModeConfigs}, {nameof(BadColumns)}={BadColumns}, {nameof(HasAntiBloomingGateProtection)}={HasAntiBloomingGateProtection}, {nameof(SerialNumber)}={SerialNumber}, {nameof(AdcBits)}={AdcBits}, {nameof(FilterType)}={FilterType}, {nameof(CcdFrameType)}={CcdFrameType}, {nameof(HasElectronicShutter)}={HasElectronicShutter}, {nameof(SupportsExternalTracker)}={SupportsExternalTracker}, {nameof(SupportsBTDI)}={SupportsBTDI}, {nameof(HasAO8)}={HasAO8}, {nameof(HasFrameBuffer)}={HasFrameBuffer}, {nameof(RequiresStartExposure2)}={RequiresStartExposure2}, {nameof(NumberExtraUnbinnedRows)}={NumberExtraUnbinnedRows}, {nameof(IsSTXL)}={IsSTXL}, {nameof(HasMechanicalShutter)}={HasMechanicalShutter}, {nameof(CcdType)}={CcdType}";
        }
    }

    public struct FilterWheelInfo {
        public SBIG.CfwModelSelect Model;
        public SBIG.CfwPosition Position;
        public SBIG.CfwStatus Status;
        public uint FirmwareVersion;
        public uint FilterCount;

        public override string ToString() {
            return $"{{{nameof(Model)}={Model}, {nameof(Position)}={Position}, {nameof(Status)}={Status}, {nameof(FirmwareVersion)}={FirmwareVersion}, {nameof(FilterCount)}={FilterCount}}}";
        }
    }

    public struct FilterWheelStatus {
        public SBIG.CfwPosition Position;
        public SBIG.CfwStatus Status;

        public override string ToString() {
            return $"{nameof(Position)}={Position}, {nameof(Status)}={Status}";
        }
    }

    public class SBIGExposureData {
        public ushort[] Data;
        public ushort Width;
        public ushort Height;

        public override string ToString() {
            return $"{{{nameof(Data)}={Data.Length} bytes, {nameof(Width)}={Width}, {nameof(Height)}={Height}}}";
        }
    }

    public interface ISbigSdk {

        DeviceInfo OpenDevice(SBIG.DeviceType deviceId);

        void CloseDevice(SBIG.DeviceType deviceId);

        string GetSdkVersion();

        void UnivDrvCommand<P>(SBIG.Cmd command, P Params);

        void UnivDrvCommand<P, R>(SBIG.Cmd command, P Params, R pResults) where R : new();

        R UnivDrvCommand<P, R>(SBIG.Cmd command, P Params) where R : new();

        DeviceQueryInfo[] QueryUsbDevices();

        CcdCameraInfo GetCameraInfo(SBIG.DeviceType deviceId, SBIG.CCD ccd);

        FilterWheelInfo? GetFilterWheelInfo(SBIG.DeviceType deviceId);

        SBIG.QueryTemperatureStatusResults2 QueryTemperatureStatus(SBIG.DeviceType deviceId);

        void RegulateTemperature(SBIG.DeviceType deviceId, SBIG.CCD ccd, double celcius);

        void DisableTemperatureRegulation(SBIG.DeviceType deviceId, SBIG.CCD ccd);

        CommandState GetExposureState(SBIG.DeviceType deviceId, SBIG.CCD ccd);

        void StartExposure(SBIG.DeviceType deviceId, SBIG.CCD ccd, ReadoutMode readoutMode, bool darkFrame, double exposureTimeSecs, Point exposureStart, Size exposureSize);

        void EndExposure(SBIG.DeviceType deviceId, SBIG.CCD ccd);

        SBIGExposureData DownloadExposure(SBIG.DeviceType deviceId, SBIG.CCD ccd, CancellationToken ct);

        void SetFilterWheelPosition(SBIG.DeviceType deviceId, ushort position);

        FilterWheelStatus GetFilterWheelStatus(SBIG.DeviceType deviceId);
    }
}