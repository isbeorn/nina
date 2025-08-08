#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Sequencer.Container;
using System.Windows.Input;

namespace NINA.Sequencer.DragDrop {

    public interface IDroppable {
        ISequenceContainer Parent { get; }

        void AttachNewParent(ISequenceContainer newParent);

        void AfterParentChanged();

        /// <summary>
        /// Command to detach the item from the UI
        /// </summary>
        ICommand DetachCommand { get; }

        /// <summary>
        /// Removes this item from the parent
        /// </summary>
        void Detach();

        ICommand MoveUpCommand { get; }

        void MoveUp();

        ICommand MoveDownCommand { get; }

        void MoveDown();
    }
}