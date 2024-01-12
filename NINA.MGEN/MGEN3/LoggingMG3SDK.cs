#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.MGEN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.MGEN3 {

    internal class LoggingMG3SDK : IMG3SDK {
        private IMG3SDK mg3sdk;
        private ILogger logger;

        public LoggingMG3SDK(IMG3SDK mG3SDK, ILogger logger) {
            this.mg3sdk = mG3SDK;
            this.logger = logger;
        }

        public int PollDevice() {
            logger.Trace($"{nameof(IMG3SDK)} - Calling PollDevice");
            var result = this.mg3sdk.PollDevice();
            logger.Trace($"{nameof(IMG3SDK)} - PollDevice returned {result}");
            return result;
        }

        public int CancelFunction() {
            logger.Trace($"{nameof(IMG3SDK)} - Calling CancelFunction");
            var result = this.mg3sdk.CancelFunction();
            logger.Trace($"{nameof(IMG3SDK)} - CancelFunction returned {result}");
            return result;
        }

        public void Close() {
            logger.Trace($"{nameof(IMG3SDK)} - Calling Close");
            this.mg3sdk.Close();
        }

        public int Dither() {
            logger.Trace($"{nameof(IMG3SDK)} - Calling Dither");
            var result = this.mg3sdk.Dither();
            logger.Trace($"{nameof(IMG3SDK)} - Dither returned {result}");
            return result;
        }

        public int GetDitherState(out int state) {
            logger.Trace($"{nameof(IMG3SDK)} - Calling GetDitherState");
            var result = this.mg3sdk.GetDitherState(out state);
            logger.Trace($"{nameof(IMG3SDK)} - GetDitherState returnd {result} and state {state}");
            return result;
        }

        public int GetFunctionState(out int pires) {
            logger.Trace($"{nameof(IMG3SDK)} - Calling GetFunctionState");
            var result = this.mg3sdk.GetFunctionState(out pires);
            logger.Trace($"{nameof(IMG3SDK)} - GetFunctionState returnd {result} and state {pires}");
            return result;
        }

        public int Open() {
            logger.Trace($"{nameof(IMG3SDK)} - Calling Open");
            var result = this.mg3sdk.Open();
            logger.Trace($"{nameof(IMG3SDK)} - Open returned {result}");
            return result;
        }

        public int PulseESC(int duration) {
            logger.Trace($"{nameof(IMG3SDK)} - Calling PulseESC with duration {duration}");
            var result = this.mg3sdk.PulseESC(duration);
            logger.Trace($"{nameof(IMG3SDK)} - PulseESC returned {result}");
            return result;
        }

        public int PushButton(int bcode, bool keyup) {
            logger.Trace($"{nameof(IMG3SDK)} - Calling PushButton with button code {bcode} and keyup {keyup}");
            var result = this.mg3sdk.PushButton(bcode, keyup);
            logger.Trace($"{nameof(IMG3SDK)} - PushButton returned {result}");
            return result;
        }

        public int ReadCalibration(out MG3SDK.MGEN3_Calibration calibration) {
            logger.Trace($"{nameof(IMG3SDK)} - Calling ReadCalibration");
            var result = this.mg3sdk.ReadCalibration(out calibration);
            logger.Trace($"{nameof(IMG3SDK)} - ReadCalibration returned {result} and calibration data {calibration.rax} {calibration.ray} {calibration.decx} {calibration.decy}");
            return result;
        }

        public int ReadDisplay(ushort[] buffer, byte[] leds, bool fullRead) {
            logger.Trace($"{nameof(IMG3SDK)} - Calling ReadDisplay with fullread {fullRead}");
            var result = this.mg3sdk.ReadDisplay(buffer, leds, fullRead);
            logger.Trace($"{nameof(IMG3SDK)} - ReadDisplay returned {result}");
            return result;
        }

        public int ReadImagingParameters(out int pgain, out int pexpo_ms) {
            logger.Trace($"{nameof(IMG3SDK)} - Calling ReadImagingParameters");
            var result = this.mg3sdk.ReadImagingParameters(out pgain, out pexpo_ms);
            logger.Trace($"{nameof(IMG3SDK)} - ReadImagingParameters returned {result} and gain {pgain}, expo_ms {pexpo_ms}");
            return result;
        }

        public int ReadLastFrameData(ref MG3SDK.MGEN3_FrameData data) {
            logger.Trace($"{nameof(IMG3SDK)} - Calling ReadLastFrameData with query {data.query}");
            var result = this.mg3sdk.ReadLastFrameData(ref data);
            logger.Trace($"{nameof(IMG3SDK)} - ReadLastFrameData returned {result}");
            return result;
        }

        public int SetImagingParameters(int pgain, int pexpo_ms) {
            logger.Trace($"{nameof(IMG3SDK)} - Calling SetImagingParameters with pgain {pgain}, pexpo_ms {pexpo_ms}");
            var result = this.mg3sdk.SetImagingParameters(pgain, pexpo_ms);
            logger.Trace($"{nameof(IMG3SDK)} - SetImagingParameters returned {result}");
            return result;
        }

        public int StarSearch(int ming = -1, int maxg = -1, int minexpo = -1, int maxexpo = -1) {
            logger.Trace($"{nameof(IMG3SDK)} - Calling StarSearch with ming {ming}, maxg {maxg}, minexpo {minexpo} ming {maxexpo}");
            var result = this.mg3sdk.StarSearch(ming, maxg, minexpo, maxexpo);
            logger.Trace($"{nameof(IMG3SDK)} - StarSearch returned {result}");
            return result;
        }

        public int StartAutoGuiding(int newrefpt) {
            logger.Trace($"{nameof(IMG3SDK)} - Calling StartAutoGuiding with newrefpt {newrefpt}");
            var result = this.mg3sdk.StartAutoGuiding(newrefpt);
            logger.Trace($"{nameof(IMG3SDK)} - StartAutoGuiding returned {result}");
            return result;
        }

        public int StartCalibration() {
            logger.Trace($"{nameof(IMG3SDK)} - Calling StartCalibration");
            var result = this.mg3sdk.StartCalibration();
            logger.Trace($"{nameof(IMG3SDK)} - StartCalibration returned {result}");
            return result;
        }

        public int ClearCalibration() {
            logger.Trace($"{nameof(IMG3SDK)} - Calling ClearCalibration");
            var result = this.mg3sdk.ClearCalibration();
            logger.Trace($"{nameof(IMG3SDK)} - ClearCalibration returned {result}");
            return result;
        }

        public int StopAutoGuiding() {
            logger.Trace($"{nameof(IMG3SDK)} - Calling StopAutoGuiding");
            var result = this.mg3sdk.StopAutoGuiding();
            logger.Trace($"{nameof(IMG3SDK)} - StopAutoGuiding returned {result}");
            return result;
        }

        public int ReadDitheringParameters(out MG3SDK.MGEN3_DitherParameters ditherParameters) {
            logger.Trace($"{nameof(IMG3SDK)} - Calling ReadDitheringParameters");
            var result = this.mg3sdk.ReadDitheringParameters(out ditherParameters);
            logger.Trace($"{nameof(IMG3SDK)} - ReadDitheringParameters returned {result}");
            return result;
        }
    }
}