#region "copyright"

/*
    Copyright © 2016 - 2023 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using System;

namespace NINA.Core.API.SGP {

    public enum ImageType {
        Light = 0,
        Dark = 1,
        Bias = 2,
        Flat = 3
    }

    public enum CameraSpeed {
        Normal = 0,
        HiSpeed = 1
    }

    public enum StateType {
        IDLE,
        CAPTURING,
        SOLVING,
        BUSY,
        MOVING,
        DISCONNECTED,
        PARKED
    }

    public enum DeviceType {
        Camera,
        FilterWheel,
        Focuser,
        Telescope,
        PlateSolver
    }

    public enum DirectionType {
        North,
        South,
        East,
        West
    }

    public abstract class SgBaseReceiptRequest {

        [JsonProperty(Required = Required.Always)]
        public Guid Receipt { get; set; }
    }

    public class SgCaptureImage {

        [JsonProperty(Required = Required.Always)]
        public int BinningMode { get; set; }

        [JsonProperty(Required = Required.Always)]
        public double ExposureLength { get; set; }

        [JsonProperty(Required = Required.Default)]
        public string Gain { get; set; }

        [JsonProperty(Required = Required.Default)]
        public string Iso { get; set; }

        [JsonProperty(Required = Required.Default)]
        public string Speed { get; set; }

        [JsonProperty(Required = Required.Default)]
        public string FrameType { get; set; }

        [JsonProperty(Required = Required.Default)]
        public string Path { get; set; }

        [JsonProperty(Required = Required.Default)]
        public bool? UseSubframe { get; set; }

        [JsonProperty(Required = Required.Default)]
        public int? X { get; set; }

        [JsonProperty(Required = Required.Default)]
        public int? Y { get; set; }

        [JsonProperty(Required = Required.Default)]
        public int? Width { get; set; }

        [JsonProperty(Required = Required.Default)]
        public int? Height { get; set; }
    }

    public class SgGetDeviceStatus {

        [JsonProperty(Required = Required.Always)]
        public DeviceType Device { get; set; }
    }

    public class SgGetImagePath : SgBaseReceiptRequest { }

    public class SgEnumerateDevices {

        [JsonProperty(Required = Required.Always)]
        public DeviceType Device { get; set; }
    }

    public class SgConnectDevice {

        [JsonProperty(Required = Required.Always)]
        public DeviceType Device { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string DeviceName { get; set; }
    }

    public class SgDisconnectDevice {

        [JsonProperty(Required = Required.Always)]
        public DeviceType Device { get; set; }
    }

    public class SgSetCameraTemp {

        [JsonProperty(Required = Required.Always)]
        public double Temperature { get; set; }
    }

    public class SgGetCameraTemp { }

    public class SgAbortImage { }

    public class SgSetCameraCoolerEnabled {

        [JsonProperty(Required = Required.Always)]
        public bool Enabled { get; set; }
    }

    public abstract class SgBaseResponse {

        [JsonProperty(Required = Required.AllowNull)]
        public string Message { get; set; }

        [JsonProperty(Required = Required.Always)]
        public bool Success { get; set; }
    }

    public class SgCaptureImageResponse : SgBaseResponse {

        [JsonProperty(Required = Required.AllowNull)]
        public Guid? Receipt { get; set; }
    }

    public class SgGetDeviceStatusResponse : SgBaseResponse {

        [JsonProperty(Required = Required.AllowNull)]
        public StateType State { get; set; }
    }

    public class SgEnumerateDevicesResponse : SgBaseResponse {

        [JsonProperty(Required = Required.AllowNull)]
        public string[] Devices { get; set; }
    }

    public class SgConnectDeviceResponse : SgBaseResponse { }

    public class SgDisconnectDeviceResponse : SgBaseResponse { }

    public class SgSetCameraTempResponse : SgBaseResponse { }

    public class SgGetCameraTempResponse : SgBaseResponse {

        [JsonProperty(Required = Required.AllowNull)]
        public double? Temperature { get; set; }
    }

    public class SgAbortImageResponse : SgBaseResponse { }

    public class SgGetCameraPropsResponse : SgBaseResponse {

        [JsonProperty(Required = Required.AllowNull)]
        public string[] GainValues { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public string[] IsoValues { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public int? NumPixelsX { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public int? NumPixelsY { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public bool? SupportsSubframe { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public bool? CanSetTemperature { get; set; }
    }

    public class SgGetImagePathResponse : SgBaseResponse { }

    public class SgSetCameraCoolerEnabledResponse : SgBaseResponse { }

    public class SgCameraCoolerResponse : SgBaseResponse {

        [JsonProperty(Required = Required.AllowNull)]
        public bool? Enabled { get; set; }
    }
}