#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;
using NINA.Sequencer.Container;
using NINA.Equipment.Interfaces.Mediator;
using NINA.ViewModel.Sequencer;
using System;
using System.Collections.Generic;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using System.Threading.Tasks;
using System.Threading;

namespace NINA.Sequencer.Mediator {

    // This mediator can only be used after it is initialized to prevent cyclic event issues
    public class SequenceMediator : ISequenceMediator {
        private ISequenceNavigationVM sequenceNavigation;
        private readonly Exception SequenceMediatorException = new Exception("SequencerNavigation not initialized yet!");

        public void RegisterSequenceNavigation(ISequenceNavigationVM sequenceNavigation) {
            if (this.sequenceNavigation != null) {
                throw new Exception("Sequencer already registered!");
            }
            this.sequenceNavigation = sequenceNavigation;
        }

        public bool Initialized => sequenceNavigation.Initialized;

        public void AddSimpleTarget(IDeepSkyObject deepSkyObject) {
            if (!Initialized) {
                throw SequenceMediatorException;
            }
            sequenceNavigation.AddSimpleTarget(deepSkyObject);
        }

        public void AddAdvancedTarget(IDeepSkyObjectContainer container) {
            if (!Initialized) {
                throw SequenceMediatorException;
            }
            sequenceNavigation.AddAdvancedTarget(container);
        }

        public void SetAdvancedSequence(ISequenceRootContainer container) {
            if (!Initialized) {
                throw SequenceMediatorException;
            }
            sequenceNavigation.SetAdvancedSequence(container);
        }

        public IList<IDeepSkyObjectContainer> GetDeepSkyObjectContainerTemplates() {
            if (!Initialized) {
                throw SequenceMediatorException;
            }
            return sequenceNavigation.GetDeepSkyObjectContainerTemplates();
        }

        public void SwitchToAdvancedView() {
            if (!Initialized) {
                throw SequenceMediatorException;
            }
            sequenceNavigation.SwitchToAdvancedView();
        }

        public void SwitchToOverview() {
            if (!Initialized) {
                throw SequenceMediatorException;
            }
            sequenceNavigation.SwitchToOverview();
        }

        public void AddTargetToTargetList(IDeepSkyObjectContainer container) {
            if (!Initialized) {
                throw SequenceMediatorException;
            }
            sequenceNavigation.AddTargetToTargetList(container);
        }

        public IList<IDeepSkyObjectContainer> GetAllTargetsInAdvancedSequence() {
            if (!Initialized) {
                throw SequenceMediatorException;
            }
            return sequenceNavigation.GetAllTargetsInAdvancedSequence();
        }

        public IList<IDeepSkyObjectContainer> GetAllTargetsInSimpleSequence() {
            if (!Initialized) {
                throw SequenceMediatorException;
            }
            return sequenceNavigation.GetAllTargetsInSimpleSequence();
        }

        public Task StartAdvancedSequence(bool skipValidation) {
            if (IsAdvancedSequenceRunning()) {
                throw new Exception("Advanced sequence is still running!");
            }
            return sequenceNavigation.Sequence2VM.StartSequenceCommand.ExecuteAsync(skipValidation);
        }

        public void CancelAdvancedSequence() {
            if (!Initialized) {
                throw SequenceMediatorException;
            }
            sequenceNavigation.Sequence2VM.CancelSequenceCommand.Execute(null);
        }

        public bool IsAdvancedSequenceRunning() {
            if (!Initialized) {
                throw SequenceMediatorException;
            }
            return sequenceNavigation.Sequence2VM.IsRunning;
        }
        public Task SaveContainer(ISequenceContainer content, string filePath, CancellationToken token) {
            if (!Initialized) {
                throw SequenceMediatorException;
            }
            return sequenceNavigation.Sequence2VM.SaveContainer(content, filePath, token);
        }

        public string GetAdvancedSequencerSavePath() {
            if (!Initialized) {
                throw SequenceMediatorException;
            }
            return sequenceNavigation.Sequence2VM.GetAdvancedSequencerSavePath();
        }

        public event Func<object, EventArgs, Task> SequenceStarting {
            add { 
                this.sequenceNavigation.Sequence2VM.SequenceStarting += value;
                this.sequenceNavigation.SimpleSequenceVM.SequenceStarting += value;
            }
            remove { 
                this.sequenceNavigation.Sequence2VM.SequenceStarting -= value;
                this.sequenceNavigation.SimpleSequenceVM.SequenceStarting -= value;
            }
        }

        public event Func<object, EventArgs, Task> SequenceFinished {
            add {
                this.sequenceNavigation.Sequence2VM.SequenceFinished += value;
                this.sequenceNavigation.SimpleSequenceVM.SequenceFinished += value;
            }
            remove {
                this.sequenceNavigation.Sequence2VM.SequenceFinished -= value;
                this.sequenceNavigation.SimpleSequenceVM.SequenceFinished -= value;
            }
        }
    }
}