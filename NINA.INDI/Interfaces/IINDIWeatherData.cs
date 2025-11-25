#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Threading;
using System.Threading.Tasks;

namespace NINA.INDI.Interfaces {
    public interface IINDIWeatherData : IINDIDevice {
        double AveragePeriod { get; }
        double CloudCover { get; }
        double DewPoint { get; }
        double Humidity { get; }
        double Pressure { get; }
        double RainRate { get; }
        double SkyBrightness { get; }
        double SkyQuality { get; }
        double SkyTemperature { get; }
        double StarFWHM { get; }
        double Temperature { get; }
        double WindDirection { get; }
        double WindGust { get; }
        double WindSpeed { get; }
    }
}
