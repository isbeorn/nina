#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Validations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NINA.Sequencer.Container {

    public interface ISequenceContainer : ISequenceItem, IValidatable {
        IList<ISequenceItem> Items { get; }
        bool IsExpanded { get; set; }

        int Iterations { get; set; }
        IExecutionStrategy Strategy { get; }

        void Add(ISequenceItem item);

        void MoveUp(ISequenceItem item);

        void MoveDown(ISequenceItem item);

        bool Remove(ISequenceItem item);

        bool Remove(ISequenceCondition item);

        bool Remove(ISequenceTrigger item);

        void ResetAll();
       
        Task Interrupt();

        ICollection<ISequenceItem> GetItemsSnapshot();
    }
}