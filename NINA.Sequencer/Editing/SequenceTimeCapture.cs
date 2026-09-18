#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Sequencer.Utility.DateTimeProvider;
using System;

namespace NINA.Sequencer.Editing {
    // Selecting an astronomical time provider overwrites the manual clock fields.
    // Preserve those fields only for manual time; calculated times stay live.
    internal static class SequenceTimeCapture {
        internal static ISequenceEditSnapshot Capture(Func<IDateTimeProvider> provider, Action<IDateTimeProvider> setProvider,
            Func<bool> calculated, Func<TimeSpan> time, Action<TimeSpan> setTime) =>
            new SequenceEditSnapshot<(IDateTimeProvider Provider, TimeSpan? ManualTime)>(
                () => (provider(), calculated() ? null : time()),
                value => {
                    setProvider(value.Provider);
                    if (value.ManualTime.HasValue) setTime(value.ManualTime.Value);
                }, format: value => value.ManualTime.HasValue ? $"{value.Provider?.Name} ({value.ManualTime.Value:c})" : value.Provider?.Name);
    }
}