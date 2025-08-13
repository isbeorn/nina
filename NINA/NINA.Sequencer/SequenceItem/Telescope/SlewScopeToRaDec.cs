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
using NINA.Sequencer.Container;
using NINA.Sequencer.Validations;
using NINA.Astrometry;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.Core.Utility.Notification;
using NINA.Sequencer.Utility;

namespace NINA.Sequencer.SequenceItem.Telescope {

    [ExportMetadata("Name", "Lbl_SequenceItem_Telescope_SlewScopeToRaDec_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Telescope_SlewScopeToRaDec_Description")]
    [ExportMetadata("Icon", "SlewToRaDecSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Telescope")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SlewScopeToRaDec : SequenceItem, IValidatable {

        [ImportingConstructor]
        public SlewScopeToRaDec(ITelescopeMediator telescopeMediator, IGuiderMediator guiderMediator) {
            this.telescopeMediator = telescopeMediator;
            this.guiderMediator = guiderMediator;
            Coordinates = new InputCoordinates();
        }

        private SlewScopeToRaDec(SlewScopeToRaDec cloneMe) : this(cloneMe.telescopeMediator, cloneMe.guiderMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new SlewScopeToRaDec(this) {
                Coordinates = Coordinates?.Clone()
            };
        }

        private ITelescopeMediator telescopeMediator;
        private IGuiderMediator guiderMediator;

        private bool inherited;

        [JsonProperty]
        public bool Inherited {
            get => inherited;
            set {
                inherited = value;
                RaisePropertyChanged();
            }
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
                Inherited = true;
            } else {
                Inherited = false;
            }
            Validate();
        }

        public bool Validate() {
            var i = new List<string>();
            var info = telescopeMediator.GetInfo();
            if (!info.Connected) {
                i.Add(Loc.Instance["LblTelescopeNotConnected"]);
            } 
            Issues = i;
            return i.Count == 0;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(SlewScopeToRaDec)}, Coordinates: {Coordinates}";
        }
    }
}