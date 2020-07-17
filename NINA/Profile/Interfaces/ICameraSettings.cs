#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyCamera;
using NINA.Utility.Enum;

namespace NINA.Profile {

    public interface ICameraSettings : ISettings {
        double BitDepth { get; set; }
        CameraBulbModeEnum BulbMode { get; set; }
        string Id { get; set; }
        double PixelSize { get; set; }
        RawConverterEnum RawConverter { get; set; }
        string SerialPort { get; set; }
        double MinFlatExposureTime { get; set; }
        double MaxFlatExposureTime { get; set; }
        string FileCameraFolder { get; set; }
        bool FileCameraUseBulbMode { get; set; }
        bool FileCameraIsBayered { get; set; }
        string FileCameraExtension { get; set; }
        BayerPatternEnum BayerPattern { get; set; }
        bool FLIEnableFloodFlush { get; set; }
        bool FLIEnableSnapshotFloodFlush { get; set; }
        double FLIFloodDuration { get; set; }
        uint FLIFlushCount { get; set; }
        BinningMode FLIFloodBin { get; set; }
        bool BitScaling { get; set; }
        double CoolingDuration { get; set; }
        double WarmingDuration { get; set; }
        double? Temperature { get; set; }
        short? BinningX { get; set; }
        short? BinningY { get; set; }
        int? Gain { get; set; }
        int? Offset { get; set; }
        int? USBLimit { get; set; }
        short? ReadoutMode { get; set; }
        short? ReadoutModeForSnapImages { get; set; }
        short? ReadoutModeForNormalImages { get; set; }
        bool QhyIncludeOverscan { get; set; }
        int Timeout { get; set; }
        bool? DewHeaterOn { get; set; }
    }
}