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
using System.Collections.Generic;
using NINA.INDI.Interfaces;

namespace NINA.INDI.Model {
    /// <summary>
    /// Implementation of IAxisRates for INDI telescopes
    /// Provides rate range information from INDI TELESCOPE_MOTION_RATE property
    /// </summary>
    public class AxisRates : IAxisRates {
        private readonly List<IRate> _rates = new List<IRate>();
        private int _position = -1;

        public AxisRates(double min, double max) {
            // INDI typically provides a single continuous range
            _rates.Add(new Rate(min, max));
        }

        public int Count => _rates.Count;

        public IRate this[int index] => _rates[index];

        public IEnumerator GetEnumerator() {
            _position = -1;
            return this;
        }

        public bool MoveNext() {
            _position++;
            return _position < _rates.Count;
        }

        public void Reset() {
            _position = -1;
        }

        public object Current => _rates[_position];
    }

    /// <summary>
    /// Implementation of IRate for INDI telescopes
    /// Represents a rate range (min, max)
    /// </summary>
    internal class Rate : IRate {
        public Rate(double minimum, double maximum) {
            Minimum = minimum;
            Maximum = maximum;
        }

        public double Maximum { get; }
        public double Minimum { get; }
    }
}