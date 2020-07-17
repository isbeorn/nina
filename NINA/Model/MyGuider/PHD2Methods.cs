#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;

namespace NINA.Model.MyGuider {

    internal class PHD2EventId {
        public const string LOOP = "1";
        public const string AUTO_SELECT_STAR = "2";
        public const string GUIDE = "3";
        public const string CLEAR_CALIBRATION = "4";
        public const string DITHER = "5";
        public const string STOP_CAPTURE = "6";
        public const string PAUSE = "10";
        public const string SET_CONNECTED = "20";

        public const string GET_PIXEL_SCALE = "96";
        public const string GET_STAR_IMAGE = "97";
        public const string GET_EXPOSURE = "98";
        public const string GET_APP_STATE = "99";
    }

    internal class PHD2Methods {

        /// <summary>
        /// 0: PIXELS TO MOVE: int
        /// 1: RA_ONLY: true/false Not Variable:
        /// 2: SETTLE PIXELS: int - maximum guide distance for guiding to be considered stable or "in-range"
        /// 3: SETTLE TIME: int - minimum time to be in-range before considering guiding to be stable
        /// 4: SETTLE TIMEOUT: int - time limit before settling is considered to have failed
        /// </summary>
        public static string DITHER = "{{\"method\": \"dither\", \"params\": [{0}, {1}, {{\"pixels\": {2}, \"time\": {3}, \"timeout\": {4}}}], \"id\": " + PHD2EventId.DITHER + "}}" + Environment.NewLine;

        public static string LOOP = "{\"method\": \"loop\", \"id\": " + PHD2EventId.LOOP + "}" + Environment.NewLine;

        public static string STOP_CAPTURE = "{\"method\": \"stop_capture\", \"id\": " + PHD2EventId.STOP_CAPTURE + "}" + Environment.NewLine;

        public static string AUTO_SELECT_STAR = "{\"method\": \"find_star\", \"id\": " + PHD2EventId.AUTO_SELECT_STAR + "}" + Environment.NewLine;

        public static string SET_CONNECTED = "{{\"method\": \"set_connected\", \"params\": [{0}], \"id\": " + PHD2EventId.SET_CONNECTED + "}}" + Environment.NewLine;

        /// <summary>
        /// 0: RECALIBRATE: true/false Not Variable:
        /// 1: SETTLE PIXELS: int - maximum guide distance for guiding to be considered stable or "in-range"
        /// 2: SETTLE TIME: int - minimum time to be in-range before considering guiding to be stable
        /// 3: SETTLE TIMEOUT: int - time limit before settling is considered to have failed
        /// </summary>
        public static string GUIDE = "{{\"method\": \"guide\", \"params\": [{{\"pixels\": 1.5, \"time\": 8, \"timeout\": 60}}, {0}], \"id\": " + PHD2EventId.GUIDE + "}}" + Environment.NewLine;

        public static string CLEAR_CALIBRATION = "{\"method\": \"clear_calibration\", \"params\": [\"both\"], \"id\": " + PHD2EventId.CLEAR_CALIBRATION + "}" + Environment.NewLine;

        public static string PAUSE = "{{\"method\": \"set_paused\", \"params\": [{0}], \"id\": " + PHD2EventId.PAUSE + "}}" + Environment.NewLine;

        public static string GET_STAR_IMAGE = "{\"method\": \"get_star_image\",\"id\": " + PHD2EventId.GET_STAR_IMAGE + "}" + Environment.NewLine;

        public static string GET_PIXEL_SCALE = "{\"method\": \"get_pixel_scale\",\"id\": " + PHD2EventId.GET_PIXEL_SCALE + "}" + Environment.NewLine;

        public static string GET_EXPOSURE = "{\"method\": \"get_exposure\",\"id\": " + PHD2EventId.GET_EXPOSURE + "}" + Environment.NewLine;
        public static string GET_APP_STATE = "{\"method\": \"get_app_state\",\"id\": " + PHD2EventId.GET_APP_STATE + "}" + Environment.NewLine;

        //public static string AUTO_SELECT_STAR = "{\"method\": \"\", \"params\": [\"\"], \"id\": }" + Environment.NewLine
    }
}