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

namespace NINA.Sequencer.SequenceItem.Telescope {

    [ExportMetadata("Name", "Lbl_SequenceItem_Telescope_SlewScopeToRaDec_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Telescope_SlewScopeToRaDec_Description")]
    [ExportMetadata("Icon", "SlewToRaDecSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Telescope")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    [ExpressionObject]

    public partial class SlewScopeToRaDec : SequenceItem, IValidatable, IDisposable {

        [ImportingConstructor]
        public SlewScopeToRaDec(ITelescopeMediator telescopeMediator, IGuiderMediator guiderMediator) {
            this.telescopeMediator = telescopeMediator;
            this.guiderMediator = guiderMediator;
            Coordinates = new InputCoordinates();
        }

        private SlewScopeToRaDec(SlewScopeToRaDec cloneMe) : this(cloneMe.telescopeMediator, cloneMe.guiderMediator) {
            CopyMetaData(cloneMe);
        }

        partial void AfterClone(SlewScopeToRaDec clone, SlewScopeToRaDec cloned) {
            clone.Coordinates = cloned.Coordinates?.Clone();
        }

        private void Coordinates_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            // When coordinates change, we change the decimal value
            if (e.PropertyName == "Coordinates") {
                InputCoordinates ic = (InputCoordinates)sender;
                Coordinates c = ic.Coordinates;
                // I think 5 decimals is ok for this...
                RaExpression.Definition = Math.Round(c.RA, 5).ToString();
            }
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

        [IsExpression (Default = 0, Range = [0, 24], HasValidator = true)]
        private double ra = 0;

        partial void RaExpressionValidator(Expression expr) {
            // When the decimal value changes, we update the HMS values
            InputCoordinates ic = new InputCoordinates();
            ic.Coordinates.RA = RaExpression.Value;
            Coordinates.RAHours = ic.RAHours;
            Coordinates.RAMinutes = ic.RAMinutes;
            Coordinates.RASeconds = ic.RASeconds;
        }

        [JsonProperty]
        public InputCoordinates Coordinates { get; set; }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
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

        public override void AfterParentChanged() {
            var coordinates = ItemUtility.RetrieveContextCoordinates(this.Parent);
            if (coordinates != null) {
                Coordinates.Coordinates = coordinates.Coordinates;
                HasDsoParent = true;
            } else {
                HasDsoParent = false;
            }

            // Is this needed?
            Coordinates.PropertyChanged += Coordinates_PropertyChanged;
            Validate();
        }

        public bool Validate() {
            var i = new List<string>();
            var info = telescopeMediator.GetInfo();
            if (!info.Connected) {
                i.Add(Loc.Instance["LblTelescopeNotConnected"]);
            }
            Expression.ValidateExpressions(i, RaExpression);
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