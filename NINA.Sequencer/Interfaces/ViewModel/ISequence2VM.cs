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
using NINA.Core.Utility;
using System.Collections.Generic;
using System.Windows.Input;
using System.Threading.Tasks;
using NINA.Equipment.Interfaces.Mediator;

namespace NINA.ViewModel.Sequencer {

    public interface ISequence2VM : ICameraConsumer {
        IAsyncCommand StartSequenceCommand { get; }
        ICommand CancelSequenceCommand { get; }
        NINA.Sequencer.ISequencer Sequencer { get; }
        NINA.Sequencer.ISequencerFactory SequencerFactory { get; }

        bool IsRunning { get; }

        Task Initialize();

        IList<IDeepSkyObjectContainer> GetDeepSkyObjectContainerTemplates();

        void AddTarget(IDeepSkyObjectContainer container);

        void AddTargetToTargetList(IDeepSkyObjectContainer container);
    }
}