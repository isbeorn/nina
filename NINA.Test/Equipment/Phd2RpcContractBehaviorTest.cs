#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NINA.Equipment.Equipment.MyGuider.PHD2;

namespace NINA.Test.Equipment {

    [TestFixture]
    public class Phd2RpcContractBehaviorTest {

        /// <summary>
        /// Verifies representative PHD2 guide requests serialize the JSON-RPC method and nested settle/ROI parameters expected by PHD2.
        /// </summary>
        [Test]
        public void Phd2Guide_SerializesMethodIdAndNestedParameters() {
            var request = new Phd2Guide {
                Parameters = new Phd2GuideParameter {
                    Recalibrate = true,
                    Roi = new[] { 10, 20, 300, 400 },
                    Settle = new Phd2Settle {
                        Pixels = 1.25,
                        Time = 8,
                        Timeout = 45
                    }
                }
            };

            JObject json = JObject.Parse(JsonConvert.SerializeObject(request));

            json["method"].Value<string>().Should().Be("guide");
            Guid.TryParse(json["id"].Value<string>(), out _).Should().BeTrue();
            json["params"]["recalibrate"].Value<bool>().Should().BeTrue();
            json["params"]["roi"].Values<int>().Should().Equal(10, 20, 300, 400);
            json["params"]["settle"]["pixels"].Value<double>().Should().Be(1.25);
            json["params"]["settle"]["time"].Value<int>().Should().Be(8);
            json["params"]["settle"]["timeout"].Value<int>().Should().Be(45);
        }

        /// <summary>
        /// Verifies PHD2 dither and lock-shift requests preserve the exact wire names and numeric values used by the PHD2 JSON-RPC API.
        /// </summary>
        [Test]
        public void Phd2ParameterizedRequests_SerializeDocumentedWireNames() {
            var dither = new Phd2Dither {
                Parameters = new Phd2DitherParameter {
                    Amount = 3.5,
                    RaOnly = true,
                    Settle = new Phd2Settle {
                        Pixels = 0.7,
                        Time = 5,
                        Timeout = 30
                    }
                }
            };
            var lockShift = new Phd2SetLockShiftParams {
                Parameters = new Phd2SetLockShiftParamsParameter {
                    Rate = new[] { 0.2, -0.1 },
                    Units = "arcsec/hr",
                    Axes = "RA/Dec"
                }
            };

            JObject ditherJson = JObject.Parse(JsonConvert.SerializeObject(dither));
            JObject lockShiftJson = JObject.Parse(JsonConvert.SerializeObject(lockShift));

            ditherJson["method"].Value<string>().Should().Be("dither");
            ditherJson["params"]["amount"].Value<double>().Should().Be(3.5);
            ditherJson["params"]["raOnly"].Value<bool>().Should().BeTrue();
            ditherJson["params"]["settle"]["pixels"].Value<double>().Should().Be(0.7);
            lockShiftJson["method"].Value<string>().Should().Be("set_lock_shift_params");
            lockShiftJson["params"]["rate"].Values<double>().Should().Equal(0.2, -0.1);
            lockShiftJson["params"]["units"].Value<string>().Should().Be("arcsec/hr");
            lockShiftJson["params"]["axes"].Value<string>().Should().Be("RA/Dec");
        }

        /// <summary>
        /// Verifies zero-argument PHD2 command DTOs expose stable JSON-RPC method names and unique request IDs.
        /// </summary>
        [TestCase(typeof(Phd2GetCameraFrameSize), "get_camera_frame_size")]
        [TestCase(typeof(Phd2Loop), "loop")]
        [TestCase(typeof(Phd2StopCapture), "stop_capture")]
        [TestCase(typeof(Phd2GetStarImage), "get_star_image")]
        [TestCase(typeof(Phd2GetPixelScale), "get_pixel_scale")]
        [TestCase(typeof(Phd2GetExposure), "get_exposure")]
        [TestCase(typeof(Phd2GetAppState), "get_app_state")]
        [TestCase(typeof(Phd2GetConnected), "get_connected")]
        [TestCase(typeof(Phd2GetProfile), "get_profile")]
        [TestCase(typeof(Phd2GetLockPosition), "get_lock_position")]
        [TestCase(typeof(Phd2GetCalibrated), "get_calibrated")]
        [TestCase(typeof(Phd2GetCoolerStatus), "get_cooler_status")]
        [TestCase(typeof(Phd2GetCurrentEquipment), "get_current_equipment")]
        [TestCase(typeof(Phd2GetDecGuideMode), "get_dec_guide_mode")]
        [TestCase(typeof(Phd2GetExposureDurations), "get_exposure_durations")]
        [TestCase(typeof(Phd2GetGuideOutputEnabled), "get_guide_output_enabled")]
        [TestCase(typeof(Phd2GetLockShiftEnabled), "get_lock_shift_enabled")]
        [TestCase(typeof(Phd2GetLockShiftParams), "get_lock_shift_params")]
        [TestCase(typeof(Phd2GetPaused), "get_paused")]
        [TestCase(typeof(Phd2GetSearchRegion), "get_search_region")]
        [TestCase(typeof(Phd2GetCCDTemperature), "get_ccd_temperature")]
        [TestCase(typeof(Phd2GetUseSubFrames), "get_use_subframes")]
        [TestCase(typeof(Phd2FlipCalibration), "flip_calibration")]
        [TestCase(typeof(Phd2SaveImage), "save_image")]
        [TestCase(typeof(Phd2Shutdown), "shutdown")]
        public void Phd2ZeroArgumentMethods_SerializeStableMethodNames(Type methodType, string expectedMethod) {
            var first = (Phd2Method)Activator.CreateInstance(methodType);
            var second = (Phd2Method)Activator.CreateInstance(methodType);

            JObject json = JObject.Parse(JsonConvert.SerializeObject(first));

            first.Method.Should().Be(expectedMethod);
            json["method"].Value<string>().Should().Be(expectedMethod);
            Guid.TryParse(json["id"].Value<string>(), out _).Should().BeTrue();
            first.Id.Should().NotBe(second.Id);
        }

        /// <summary>
        /// Verifies PHD2 response DTOs deserialize success, profile, lock-shift, and error payloads without losing fields used by the guider.
        /// </summary>
        [Test]
        public void Phd2Responses_DeserializeResultAndErrorPayloads() {
            const string profileJson = """{"jsonrpc":"2.0","id":"1","result":{"id":7,"name":"OAG profile"}}""";
            const string profilesJson = """{"jsonrpc":"2.0","id":"2","result":[{"id":1,"name":"Wide"},{"id":2,"name":"Narrow"}]}""";
            const string lockShiftJson = """{"jsonrpc":"2.0","id":"3","result":{"enabled":true,"rate":[0.1,-0.2],"units":"arcsec/hr","axes":"RA/Dec"}}""";
            const string errorJson = """{"jsonrpc":"2.0","id":"4","error":{"code":-32602,"message":"Invalid params"}}""";

            GetProfileResponse profile = JsonConvert.DeserializeObject<GetProfileResponse>(profileJson);
            GetProfilesResponse profiles = JsonConvert.DeserializeObject<GetProfilesResponse>(profilesJson);
            GetLockShiftParamsResponse lockShift = JsonConvert.DeserializeObject<GetLockShiftParamsResponse>(lockShiftJson);
            GenericPhdMethodResponse error = JsonConvert.DeserializeObject<GenericPhdMethodResponse>(errorJson);

            profile.result.id.Should().Be(7);
            profile.result.name.Should().Be("OAG profile");
            profiles.result.Select(x => x.name).Should().Equal("Wide", "Narrow");
            lockShift.result.Enabled.Should().BeTrue();
            lockShift.result.Rate.Should().Equal(0.1f, -0.2f);
            lockShift.result.Units.Should().Be("arcsec/hr");
            lockShift.result.Axes.Should().Be("RA/Dec");
            error.error.code.Should().Be(-32602);
            error.error.message.Should().Be("Invalid params");
        }
    }
}
