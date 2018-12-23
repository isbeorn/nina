#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

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

using NINA.Utility;
using System.Collections;
using System.IO;
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
        bool LiveViewEnabled { get; set; }
        bool HasBattery { get; }
        int BatteryLevel { get; }
        int BitDepth { get; }

        int Offset { get; set; }
        int USBLimit { get; set; }
        bool CanSetOffset { get; }
        bool CanSetUSBLimit { get; }
        bool CanGetGain { get; }
        bool CanSetGain { get; }
        short GainMax { get; }
        short GainMin { get; }
        short Gain { get; set; }

        ArrayList Gains { get; }

        AsyncObservableCollection<BinningMode> BinningModes { get; }

        void SetBinning(short x, short y);

        void StartExposure(double exposureTime, bool isLightFrame);

        void StopExposure();

        void StartLiveView();

        Task<ImageArray> DownloadLiveView(CancellationToken token);

        void StopLiveView();

        void AbortExposure();

        Task<ImageArray> DownloadExposure(CancellationToken token, bool calculateStatistics);
    }
}