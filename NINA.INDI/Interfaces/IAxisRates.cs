#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections;

namespace NINA.INDI.Interfaces {
    /// <summary>
    /// Interface for axis rate information
    /// Compatible with ASCOM's IAxisRates interface for NINA integration
    /// </summary>
    public interface IAxisRates : IEnumerable, IEnumerator {
        int Count { get; }
        IRate this[int index] { get; }
    }

    /// <summary>
    /// Interface for rate range information
    /// Compatible with ASCOM's IRate interface for NINA integration
    /// </summary>
    public interface IRate {
        double Maximum { get; }
        double Minimum { get; }
    }
}
