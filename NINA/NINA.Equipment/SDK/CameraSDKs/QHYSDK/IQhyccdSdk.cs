#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace QHYCCD {

    public interface IQhySdk {

        void InitSdk();

        void ReleaseSdk();

        void Open(StringBuilder id);

        void Close();

        void InitCamera();

        uint Scan();

        uint SetBinMode(uint binX, uint binY);

        uint SetBitsMode(uint bitDepth);

        uint SetStreamMode(byte mode);

        uint SetDebayerOnOff(bool onoff);

        uint GetReadMode(ref uint mode);

        uint SetReadMode(uint mode);

        uint GetNumberOfReadModes(ref uint numModes);

        uint GetReadModeName(uint mode, StringBuilder modeName);

        uint ControlTemp(double targetTemp);

        uint ControlShutter(byte shutterState);

        void GetId(uint index, StringBuilder id);

        void GetModel(StringBuilder id, StringBuilder model);

        uint GetParamMinMaxStep(QhySdk.CONTROL_ID controlId, ref double min, ref double max, ref double step);

        uint GetChipInfo(ref double chipW, ref double chipH, ref uint imageX, ref uint imageY, ref double pixelX, ref double pixelY, ref uint bpp);

        uint GetEffectiveArea(ref uint startX, ref uint startY, ref uint imageX, ref uint imageY);

        uint SetResolution(uint x, uint y, uint xSize, uint ySize);

        uint ExpSingleFrame();

        uint GetSingleFrame(ref uint sizeX, ref uint sizeY, ref uint bpp, ref uint channels, [Out] byte[] rawArray);

        uint GetSingleFrame(ref uint sizeX, ref uint sizeY, ref uint bpp, ref uint channels, [Out] ushort[] rawArray);

        uint CancelExposingAndReadout();

        uint GetExposureRemaining();

        uint GetPressure(ref double hpa);

        uint GetHumidity(ref double rh);

        bool IsCfwPlugged();

        uint GetCfwStatus([In, Out] byte[] status);

        uint SendOrderToCfw(string order, int length);

        bool IsControl(QhySdk.CONTROL_ID type);

        double GetControlValue(QhySdk.CONTROL_ID type);

        bool SetControlValue(QhySdk.CONTROL_ID type, double value);

        QhySdk.BAYER_ID GetBayerType();

        string GetSdkVersion();

        string GetFwVersion();

        string GetFpgaVersion();

        // Added for Live view/recording
        uint BeginQHYCCDLive();

        // These two methods are identical on the C side, they just have different pointer types (they're ABI compatible).
        uint GetQHYCCDLiveFrame(ref uint sizeX, ref uint sizeY, ref uint bpp, ref uint channels, [Out] byte[] rawArray);

        uint GetQHYCCDLiveFrame(ref uint sizeX, ref uint sizeY, ref uint bpp, ref uint channels, [Out] ushort[] rawArray);

        uint StopQHYCCDLive();

        uint GetQHYCCDMemLength();

        uint SetQHYCCDGPSVCOXFreq(ushort freq);

        uint SetQHYCCDGPSLedCalMode(byte mode);

        uint SetQHYCCDGPSMasterSlave(byte mode);

        void SetQHYCCDGPSPOSA(uint pos, byte width);

        void SetQHYCCDGPSPOSB(uint pos, byte width);

        uint GetQHYCCDPreciseExposureInfo(ref uint pixelPeriod, ref uint linePeriod, ref uint framePeriod, ref uint clocksPerLine, ref uint linesPerFrame, ref uint actualExposureTime, ref byte isLongExposureMode);
        
        uint GetQHYCCDRollingShutterEndOffset(uint row, ref double offset);
    }
}