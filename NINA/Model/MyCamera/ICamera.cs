#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyCamera {

    internal interface ICamera : IDevice {
        bool HasShutter { get; }
        double Temperature { get; }
        double TemperatureSetPoint { get; set; }
        short BinX { get; set; }
        short BinY { get; set; }
        string SensorName { get; }
        SensorType SensorType { get; }
        short BayerOffsetX { get; }
        short BayerOffsetY { get; }
        int CameraXSize { get; }
        int CameraYSize { get; }
        double ExposureMin { get; }
        double ExposureMax { get; }
        short MaxBinX { get; }
        short MaxBinY { get; }
        double PixelSizeX { get; }
        double PixelSizeY { get; }
        bool CanSetTemperature { get; }
        bool CoolerOn { get; set; }
        double CoolerPower { get; }
        bool HasDewHeater { get; }
        bool DewHeaterOn { get; set; }
        string CameraState { get; }
        bool CanSubSample { get; }
        bool EnableSubSample { get; set; }
        int SubSampleX { get; set; }
        int SubSampleY { get; set; }
        int SubSampleWidth { get; set; }
        int SubSampleHeight { get; set; }
        bool CanShowLiveView { get; }
        bool LiveViewEnabled { get; }
        bool HasBattery { get; }
        int BatteryLevel { get; }
        int BitDepth { get; }

        bool CanSetOffset { get; }
        int Offset { get; set; }
        int OffsetMin { get; }
        int OffsetMax { get; }
        bool CanSetUSBLimit { get; }
        int USBLimit { get; set; }
        int USBLimitMin { get; }
        int USBLimitMax { get; }
        int USBLimitStep { get; }
        bool CanGetGain { get; }
        bool CanSetGain { get; }
        int GainMax { get; }
        int GainMin { get; }
        int Gain { get; set; }
        double ElectronsPerADU { get; }
        IList<string> ReadoutModes { get; }
        short ReadoutMode { get; set; }
        short ReadoutModeForSnapImages { get; set; }
        short ReadoutModeForNormalImages { get; set; }

        IList<int> Gains { get; }

        AsyncObservableCollection<BinningMode> BinningModes { get; }

        void SetBinning(short x, short y);

        void StartExposure(CaptureSequence sequence);

        Task WaitUntilExposureIsReady(CancellationToken token);

        void StopExposure();

        void StartLiveView();

        Task<IExposureData> DownloadLiveView(CancellationToken token);

        void StopLiveView();

        void AbortExposure();

        Task<IExposureData> DownloadExposure(CancellationToken token);
    }
}