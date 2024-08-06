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
using NINA.Astrometry.Interfaces;
using NINA.Sequencer.Container;
using NINA.ViewModel.Sequencer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.Interfaces.Mediator {

    public interface ISequenceMediator {

        IList<IDeepSkyObjectContainer> GetDeepSkyObjectContainerTemplates();

        void SetAdvancedSequence(ISequenceRootContainer container);

        void AddAdvancedTarget(IDeepSkyObjectContainer container);

        void AddSimpleTarget(IDeepSkyObject deepSkyObject);

        void RegisterSequenceNavigation(ISequenceNavigationVM sequenceNavigation);

        void SwitchToAdvancedView();

        void SwitchToOverview();

        void AddTargetToTargetList(IDeepSkyObjectContainer container);

        bool Initialized { get; }

        IList<IDeepSkyObjectContainer> GetAllTargetsInAdvancedSequence();
        IList<IDeepSkyObjectContainer> GetAllTargetsInSimpleSequence();

        Task StartAdvancedSequence(bool skipValidation);
        void CancelAdvancedSequence();
        bool IsAdvancedSequenceRunning();
        Task SaveContainer(ISequenceContainer content, string filePath, CancellationToken token);
        string GetAdvancedSequencerSavePath();

        event Func<object, EventArgs, Task> SequenceStarting;
        event Func<object, EventArgs, Task> SequenceFinished;
    }
}