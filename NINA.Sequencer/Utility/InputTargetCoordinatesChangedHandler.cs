#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using System;

namespace NINA.Sequencer.Utility {
    // Sequence graphs can be constructed, cloned and replaced on different threads.
    // Keep their target subscriptions independent of WPF's thread-local weak event table.
    internal sealed class InputTargetCoordinatesChangedHandler<T> where T : class {
        private readonly WeakReference<T> listener;
        private readonly Action<T, object, EventArgs> callback;

        internal InputTargetCoordinatesChangedHandler(T listener, Action<T, object, EventArgs> callback) {
            this.listener = new WeakReference<T>(listener);
            this.callback = callback;
        }

        internal void Handle(object sender, EventArgs args) {
            if (listener.TryGetTarget(out T owner)) {
                callback(owner, sender, args);
            } else if (sender is InputTarget source) {
                // A quiet source may retain this small handler, but never its collected listener.
                source.CoordinatesChanged -= Handle;
            }
        }
    }
}
