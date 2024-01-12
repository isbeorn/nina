#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

namespace NINA.MGEN3 {

    internal interface IMG3SDK {

        int CancelFunction();

        void Close();

        int Dither();

        int GetDitherState(out int state);

        int GetFunctionState(out int pires);

        int PollDevice();

        int Open();

        int PulseESC(int duration);

        int PushButton(int bcode, bool keyup);

        int ReadCalibration(out MG3SDK.MGEN3_Calibration calibration);

        int ClearCalibration();

        int ReadDisplay(ushort[] buffer, byte[] leds, bool fullRead);

        int ReadImagingParameters(out int pgain, out int pexpo_ms);

        int ReadLastFrameData(ref MG3SDK.MGEN3_FrameData data);

        int SetImagingParameters(int pgain, int pexpo_ms);

        int StarSearch(int ming = -1, int maxg = -1, int minexpo = -1, int maxexpo = -1);

        int StartAutoGuiding(int newrefpt);

        int StartCalibration();

        int StopAutoGuiding();

        int ReadDitheringParameters(out MG3SDK.MGEN3_DitherParameters ditherParameters);
    }
}