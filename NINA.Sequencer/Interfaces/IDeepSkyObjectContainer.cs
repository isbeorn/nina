#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using NINA.Core.Model.Equipment;
using NINA.Sequencer.Interfaces;

namespace NINA.Sequencer.Container {

    public interface IDeepSkyObjectContainer : ISequenceContainer {
        InputTarget Target { get; set; }
        NighttimeData NighttimeData { get; }

        // This should be part of the interface, however for plugin compatibiltiy this is only part of DSO Container Implementation for now
        // int GetOrCreateExposureCountForItemAndCurrentFilter(IExposureItem exposureItem, double roi);
        // void IncrementExposureCountForItemAndCurrentFilter(IExposureItem exposureItem, int roi)
    }
}