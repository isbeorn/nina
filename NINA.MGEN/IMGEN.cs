#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.MGEN {

    public interface IMGEN {
        double PixelSize { get; }
        int SensorSizeX { get; }
        int SensorSizeY { get; }

        Task<bool> CancelCalibration(CancellationToken ct = default);

        Task DetectAndOpen(CancellationToken ct = default);

        Task Disconnect(CancellationToken ct = default);

        Task<bool> Dither(CancellationToken ct = default);

        Task<ImagingParameter> GetImagingParameter(CancellationToken ct = default);

        Task<StarData> GetStarData(byte starIndex, CancellationToken ct = default);

        Task<bool> IsGuidingActive(CancellationToken ct = default);

        Task<bool> PressButton(MGENButton button, CancellationToken ct);

        Task<CalibrationStatusReport> QueryCalibration(CancellationToken ct = default);

        Task<GuideState> QueryGuideState(CancellationToken ct = default);

        Task<Bitmap> ReadDisplay(Color primaryColor, Color backgroundColor, CancellationToken ct = default);

        Task<LEDState> ReadLEDState(CancellationToken ct = default);

        Task<bool> SetImagingParameter(int gain, int exposureTime, int threshold, CancellationToken ct = default);

        Task<bool> SetNewGuidingPosition(StarData starDetail, CancellationToken ct = default);

        Task<bool> StartCalibration(CancellationToken ct = default);

        Task<bool> StartCamera(CancellationToken ct = default);

        Task<bool> StartGuiding(CancellationToken ct = default);

        Task<int> StartStarSearch(int gain, int exposureTime, CancellationToken ct = default);

        Task<bool> StopGuiding(CancellationToken ct = default);

        Task<DitherAmplitude> GetDitherAmplitude(CancellationToken ct = default);

        Task<bool> IsActivelyGuiding(CancellationToken ct);
    }
}