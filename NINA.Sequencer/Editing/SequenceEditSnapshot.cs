#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;

namespace NINA.Sequencer.Editing {
    internal sealed class SequenceEditSnapshot<T> : ISequenceEditSnapshot {
        private readonly Func<T> read;
        private readonly Action<T> write;
        private readonly T value;
        private readonly Func<T, T, bool> equal;

        public SequenceEditSnapshot(Func<T> read, Action<T> write, Func<T, T, bool> equal = null, Func<T, string> format = null)
            : this(read, write, read(), equal, format) { }

        public SequenceEditSnapshot(Func<T> read, Action<T> write, T value, Func<T, T, bool> equal = null, Func<T, string> format = null) {
            this.read = read;
            this.write = write;
            this.value = value;
            this.equal = equal ?? EqualityComparer<T>.Default.Equals;
            Description = format?.Invoke(value);
        }
        public string Description { get; }
        public bool IsCurrent => equal(read(), value);
        public void Restore() => write(value);
    }
}