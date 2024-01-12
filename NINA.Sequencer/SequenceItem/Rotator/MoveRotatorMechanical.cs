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
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;

namespace NINA.Sequencer.SequenceItem.Rotator {

    [ExportMetadata("Name", "Lbl_SequenceItem_Rotator_MoveRotatorMechanical_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Rotator_MoveRotatorMechanical_Description")]
    [ExportMetadata("Icon", "RotatorSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Rotator")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class MoveRotatorMechanical : SequenceItem, IValidatable {

        [ImportingConstructor]
        public MoveRotatorMechanical(IRotatorMediator RotatorMediator) {
            this.rotatorMediator = RotatorMediator;
        }

        private MoveRotatorMechanical(MoveRotatorMechanical cloneMe) : this(cloneMe.rotatorMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new MoveRotatorMechanical(this) {
                MechanicalPosition = MechanicalPosition
            };
        }

        private IRotatorMediator rotatorMediator;

        private float mechanicalPosition = 0;

        [JsonProperty]
        public float MechanicalPosition {
            get => mechanicalPosition;
            set {
                mechanicalPosition = value;
                RaisePropertyChanged();
            }
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            return rotatorMediator.MoveMechanical(MechanicalPosition, token);
        }

        public bool Validate() {
            var i = new List<string>();
            if (!rotatorMediator.GetInfo().Connected) {
                i.Add(Loc.Instance["LblRotatorNotConnected"]);
            }
            Issues = i;
            return i.Count == 0;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(MoveRotatorMechanical)}, Mechanical Position: {MechanicalPosition}";
        }
    }
}