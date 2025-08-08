#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using System;

namespace NINA.Equipment.Equipment.MyGuider.PHD2 {

    public abstract class Phd2Method {

        [JsonProperty(PropertyName = "id")]
        public string Id { get; } = Guid.NewGuid().ToString();

        [JsonProperty(PropertyName = "method")]
        public abstract string Method { get; }
    }

    public abstract class Phd2Method<T> : Phd2Method {

        [JsonProperty(PropertyName = "params")]
        public T Parameters { get; set; }
    }

    public class Phd2Guide : Phd2Method<Phd2GuideParameter> {
        public override string Method => "guide";
    }

    public class Phd2GuideParameter {

        [JsonProperty(PropertyName = "settle")]
        public Phd2Settle Settle { get; set; }

        [JsonProperty(PropertyName = "recalibrate")]
        public bool Recalibrate { get; set; }

        [JsonProperty(PropertyName = "roi")]
        public int[] Roi { get; set; }
    }

    public class Phd2Dither : Phd2Method<Phd2DitherParameter> {
        public override string Method => "dither";
    }

    public class Phd2DitherParameter {

        [JsonProperty(PropertyName = "amount")]
        public double Amount { get; set; }

        [JsonProperty(PropertyName = "raOnly")]
        public bool RaOnly { get; set; }

        [JsonProperty(PropertyName = "settle")]
        public Phd2Settle Settle { get; set; }
    }

    public class Phd2Settle {

        [JsonProperty(PropertyName = "pixels")]
        public double Pixels { get; set; }

        [JsonProperty(PropertyName = "time")]
        public int Time { get; set; }

        [JsonProperty(PropertyName = "timeout")]
        public int Timeout { get; set; }
    }

    public class Phd2GetCameraFrameSize : Phd2Method {
        public override string Method => "get_camera_frame_size";
    }

    public class Phd2FindStar : Phd2Method<Phd2FindStarParameter> {
        public override string Method => "find_star";
    }

    public class Phd2FindStarParameter {

        [JsonProperty(PropertyName = "roi")]
        public int[] Roi { get; set; }
    }

    public class Phd2Loop : Phd2Method {
        public override string Method => "loop";
    }

    public class Phd2StopCapture : Phd2Method {
        public override string Method => "stop_capture";
    }

    public class Phd2GetStarImage : Phd2Method {
        public override string Method => "get_star_image";
    }

    public class Phd2GetPixelScale : Phd2Method {
        public override string Method => "get_pixel_scale";
    }

    public class Phd2GetExposure : Phd2Method {
        public override string Method => "get_exposure";
    }

    public class Phd2GetAppState : Phd2Method {
        public override string Method => "get_app_state";
    }

    public class Phd2Pause : Phd2Method<Array> {
        public override string Method => "set_paused";
    }

    public class Phd2GetConnected : Phd2Method {
        public override string Method => "get_connected";
    }

    public class Phd2SetConnected : Phd2Method<Array> {
        public override string Method => "set_connected";
    }

    public class Phd2ClearCalibration : Phd2Method<Array> {
        public override string Method => "clear_calibration";
    }

    public class Phd2GetProfile : Phd2Method {
        public override string Method => "get_profile";
    }

    public class Phd2GetProfiles : Phd2Method<Array> {
        public override string Method => "get_profiles";
    }

    public class Phd2GetLockPosition : Phd2Method {
        public override string Method => "get_lock_position";
    }

    public class Phd2SetProfile : Phd2Method<Array> {
        public override string Method => "set_profile";
    }

    public class Phd2GetAlgoParamNames : Phd2Method<Array> {
        public override string Method => "get_algo_param_names";
    }

    public class Phd2GetAlgoParam : Phd2Method {
        public override string Method => "get_algo_param";
    }

    public class Phd2GetCalibrated : Phd2Method {
        public override string Method => "get_calibrated";
    }

    public class Phd2GetCalibrationData : Phd2Method<Array> {
        public override string Method => "get_calibration_data";
    }

    public class Phd2GetCoolerStatus : Phd2Method {
        public override string Method => "get_cooler_status";
    }

    public class Phd2GetCurrentEquipment : Phd2Method {
        public override string Method => "get_current_equipment";
    }

    public class Phd2GetDecGuideMode : Phd2Method {
        public override string Method => "get_dec_guide_mode";
    }

    public class Phd2GetExposureDurations : Phd2Method {
        public override string Method => "get_exposure_durations";
    }

    public class Phd2GetGuideOutputEnabled : Phd2Method {
        public override string Method => "get_guide_output_enabled";
    }

    public class Phd2GetLockShiftEnabled : Phd2Method {
        public override string Method => "get_lock_shift_enabled";
    }

    public class Phd2GetLockShiftParams : Phd2Method {
        public override string Method => "get_lock_shift_params";
    }

    public class Phd2GetPaused : Phd2Method {
        public override string Method => "get_paused";
    }

    public class Phd2GetSearchRegion : Phd2Method {
        public override string Method => "get_search_region";
    }

    public class Phd2GetCCDTemperature : Phd2Method {
        public override string Method => "get_ccd_temperature";
    }

    public class Phd2GetUseSubFrames : Phd2Method {
        public override string Method => "get_use_subframes";
    }

    public class Phd2SetAlgoParam : Phd2Method<Array> {
        public override string Method => "set_algo_param";
    }

    public class Phd2SetDecGuideMode : Phd2Method<Array> {
        public override string Method => "set_dec_guide_mode";
    }

    public class Phd2SetExposure : Phd2Method<Array> {
        public override string Method => "set_exposure";
    }

    public class Phd2SetGuideOutputEnabled : Phd2Method<Array> {
        public override string Method => "set_guide_output_enabled";
    }

    public class Phd2SetLockPosition : Phd2Method<Array> {
        public override string Method => "set_lock_position";
    }

    public class Phd2SetLockShiftEnabled : Phd2Method<Array> {
        public override string Method => "set_lock_shift_enabled";
    }

    public class Phd2SetLockShiftParams : Phd2Method<Phd2SetLockShiftParamsParameter> {
        public override string Method => "set_lock_shift_params";
    }

    public class Phd2SetLockShiftParamsParameter {

        [JsonProperty(PropertyName = "rate")]
        public double[] Rate { get; set; }

        [JsonProperty(PropertyName = "units")]
        public string Units { get; set; }

        [JsonProperty(PropertyName = "axes")]
        public string Axes { get; set; }
    }

    public class Phd2CaptureSingleFrame : Phd2Method<Phd2CaptureSingleFrameParameter> {
        public override string Method => "capture_single_frame";
    }

    public class Phd2CaptureSingleFrameParameter {

        [JsonProperty(PropertyName = "exposure")]
        public int Exposure { get; set; }

        [JsonProperty(PropertyName = "subframe")]
        public int[] Subframe { get; set; }
    }

    public class Phd2FlipCalibration : Phd2Method {
        public override string Method => "flip_calibration";
    }

    public class Phd2GuidePulse : Phd2Method<Array> {
        public override string Method => "guide_pulse";
    }

    public class Phd2SaveImage : Phd2Method {
        public override string Method => "save_image";
    }

    public class Phd2Shutdown : Phd2Method {
        public override string Method => "shutdown";
    }

    public class PhdMethodResponse {
        public string jsonrpc;
        public PhdError error;
        public string id;
    }

    public class GenericPhdMethodResponse : PhdMethodResponse {
        public object result;
    }

    public class BooleanPhdMethodResponse : PhdMethodResponse {
        public bool result;
    }

    public class GetCameraFrameSizeResponse : PhdMethodResponse {
        public int[] result;
    }

    public class PhdImageResult {
        public int frame;
        public int width;
        public int height;
        public double[] star_pos;
        public string pixels;
    }

    public class Phd2ProfileResponse {
        public int id;
        public string name { get; set; }
    }

    public class GetProfileResponse : PhdMethodResponse {
        public Phd2ProfileResponse result;
    }

    public class GetProfilesResponse : PhdMethodResponse {
        public Phd2ProfileResponse[] result;
    }

    public class GetLockPositionResponse : PhdMethodResponse {
        public float[] result;
    }

    public class GetLockShiftParamsResponse : PhdMethodResponse {
        public LockShiftParams result;
    }

    public class GetExposureResponse : PhdMethodResponse {
        public int result;
    }

    public class LockShiftParams {

        [JsonProperty(PropertyName = "enabled")]
        public bool Enabled { get; set; }

        [JsonProperty(PropertyName = "rate")]
        public float[] Rate { get; set; }

        [JsonProperty(PropertyName = "units")]
        public string Units { get; set; }

        [JsonProperty(PropertyName = "axes")]
        public string Axes { get; set; }
    }

    public class PhdError {
        public int code;
        public string message;
    }
}