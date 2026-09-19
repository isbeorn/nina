#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Collections.Generic;

namespace NINA.Sequencer.Editing {
    /// <summary>
    /// Optional ownership of editor children outside the standard Items, Conditions and Triggers.
    /// Return existing objects only, excluding generated execution contents. Standard collections
    /// are discovered automatically. Collection names must be unique and stable for this owner.
    /// </summary>
    public interface ISequenceEditorChildProvider {
        IEnumerable<ISequenceEditorChildList> GetEditorChildLists();
    }

    /// <summary>
    /// An editor-owned collection or single-child slot. Resolve the owner's current contents on
    /// each read. Mutations must use the same attach/detach hooks as normal editing. A read-only
    /// list describes ownership of fixed action containers; their own contents remain editable.
    /// </summary>
    public interface ISequenceEditorChildList {
        string Name { get; }
        bool IsReadOnly { get; }
        IReadOnlyList<ISequenceEntity> Read();
        void Insert(int index, ISequenceEntity entity);
        void Remove(ISequenceEntity entity);
        void Move(ISequenceEntity entity, int index);
    }
}