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
using NINA.Astrometry;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.Core.Utility.Notification;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Generators;
using NINA.Sequencer.Logic;
using CommunityToolkit.Mvvm.ComponentModel;
using NINA.Core.Utility;
using Parlot.Fluent;
using System.Runtime.InteropServices;

namespace NINA.Sequencer.SequenceItem.Telescope {

    [ExportMetadata("Name", "Lbl_SequenceItem_Telescope_SlewScopeToRaDec_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Telescope_SlewScopeToRaDec_Description")]
    [ExportMetadata("Icon", "SlewToRaDecSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Telescope")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]

    public class SlewScopeToRaDec : CoordinatesInstruction, IValidatable, IDisposable {

        [ImportingConstructor]
        public SlewScopeToRaDec(ITelescopeMediator telescopeMediator, IGuiderMediator guiderMediator) :base(null) {
            this.telescopeMediator = telescopeMediator;
            this.guiderMediator = guiderMediator;
        }

        private SlewScopeToRaDec(SlewScopeToRaDec cloneMe) : this(cloneMe.telescopeMediator, cloneMe.guiderMediator) {
            CopyMetaData(cloneMe);
        }

        public override SlewScopeToRaDec Clone() {
            SlewScopeToRaDec clone = new SlewScopeToRaDec(this);
            clone.RaExpression = new Expression(RaExpression);
            clone.DecExpression = new Expression(DecExpression);
            clone.Coordinates = Coordinates?.Clone();
            return clone;
        }

        private ITelescopeMediator telescopeMediator;
        private IGuiderMediator guiderMediator;

        private bool hasDsoParent;

        [JsonProperty]
        public bool HasDsoParent {
            get => hasDsoParent;
            set {
                hasDsoParent = value;
                RaisePropertyChanged();
            }
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if(telescopeMediator.GetInfo().AtPark) {
                Notification.ShowError(Loc.Instance["LblTelescopeParkedWarning"]);
                throw new SequenceEntityFailedException(Loc.Instance["LblTelescopeParkedWarning"]);
            }
            
            var stoppedGuiding = await guiderMediator.StopGuiding(token);
            await telescopeMediator.SlewToCoordinatesAsync(Coordinates.Coordinates, token);
            if (stoppedGuiding) {
                await guiderMediator.StartGuiding(false, progress, token);
            }
        }

        public IList<string> Issues = new List<string>();

        public override void AfterParentChanged() {
            var coordinates = ItemUtility.RetrieveContextCoordinates(this.Parent);
            if (coordinates != null) {
                Coordinates.Coordinates = coordinates.Coordinates;
                HasDsoParent = true;
            } else {
                HasDsoParent = false;
            }

            // Is this needed?
            if (Coordinates != null) {
                Coordinates.PropertyChanged += Coordinates_PropertyChanged;
            }
            RaExpression.Context = Parent;
            DecExpression.Context = Parent;
            Validate();
        }

        public bool Validate() {
            var i = new List<string>();
            var info = telescopeMediator.GetInfo();
            if (!info.Connected) {
                i.Add(Loc.Instance["LblTelescopeNotConnected"]);
            }
            Expression.ValidateExpressions(i, RaExpression, DecExpression);
            Issues = i;
            return i.Count == 0;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(SlewScopeToRaDec)}, Coordinates: {Coordinates}";
        }

        public void Dispose() {
            Coordinates.PropertyChanged -= Coordinates_PropertyChanged;
        }
    }
}