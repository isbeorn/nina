#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Sequencer.Validations;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.Equipment.Interfaces;

namespace NINA.Sequencer.SequenceItem.Dome {

    [ExportMetadata("Name", "Lbl_SequenceItem_Dome_SynchronizeDome_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Dome_SynchronizeDome_Description")]
    [ExportMetadata("Icon", "LoopSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Dome")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SynchronizeDome : SequenceItem, IValidatable {

        [ImportingConstructor]
        public SynchronizeDome(IDomeMediator domeMediator, IDomeFollower domeFollower, ITelescopeMediator telescopeMediator) {
            this.domeMediator = domeMediator;
            this.domeFollower = domeFollower;
            this.telescopeMediator = telescopeMediator;
        }

        private SynchronizeDome(SynchronizeDome cloneMe) : this(cloneMe.domeMediator, cloneMe.domeFollower, cloneMe.telescopeMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new SynchronizeDome(this);
        }

        private readonly IDomeMediator domeMediator;
        private readonly IDomeFollower domeFollower;
        private readonly ITelescopeMediator telescopeMediator;
        private IList<string> issues = ImmutableList<string>.Empty;

        public IList<string> Issues {
            get => issues;
            set {
                issues = ImmutableList.CreateRange(value);
                RaisePropertyChanged();
            }
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (Validate()) {
                if (!await domeFollower.TriggerTelescopeSync()) {
                    throw new Exception("Synchronize dome operation didn't complete successfully");
                }
            } else {
                throw new SequenceItemSkippedException(string.Join(",", Issues));
            }
        }

        public bool Validate() {
            var i = new List<string>();
            if (!domeMediator.GetInfo().Connected) {
                i.Add(Loc.Instance["LblDomeNotConnected"]);
            }
            if (!telescopeMediator.GetInfo().Connected) {
                i.Add(Loc.Instance["LblTelescopeNotConnected"]);
            }
            Issues = i;
            return i.Count == 0;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(SynchronizeDome)}";
        }
    }
}