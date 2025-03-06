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
using NINA.Profile.Interfaces;
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
using System.Windows;
using NINA.Sequencer.Generators;
using System.Runtime.Serialization;

namespace NINA.Sequencer.SequenceItem.Telescope {

    [ExportMetadata("Name", "Lbl_SequenceItem_Telescope_SlewScopeToAltAz_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Telescope_SlewScopeToAltAz_Description")]
    [ExportMetadata("Icon", "SlewToAltAzSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Telescope")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions]

    public partial class SlewScopeToAltAz : SequenceItem, IValidatable {

        [ImportingConstructor]
        public SlewScopeToAltAz(IProfileService profileService, ITelescopeMediator telescopeMediator, IGuiderMediator guiderMediator) {
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
            this.guiderMediator = guiderMediator;
            Coordinates = new InputTopocentricCoordinates(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude), profileService.ActiveProfile.AstrometrySettings.Elevation);
            WeakEventManager<IProfileService, EventArgs>.AddHandler(profileService, nameof(profileService.LocationChanged), ProfileService_LocationChanged);
        }

        private void ProfileService_LocationChanged(object sender, EventArgs e) {
            Coordinates?.SetPosition(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude), profileService.ActiveProfile.AstrometrySettings.Elevation);
        }

        private SlewScopeToAltAz(SlewScopeToAltAz cloneMe) : this(cloneMe.profileService, cloneMe.telescopeMediator, cloneMe.guiderMediator) {
            CopyMetaData(cloneMe);
        }

        
        partial void AfterClone(SlewScopeToAltAz clone) {
            clone.Coordinates = Coordinates?.Clone();
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            // Fix up Ra and Dec Expressions (auto-update to existing sequences)
            TopocentricCoordinates c = Coordinates.Coordinates;
            if (AltExpression.Definition.Length == 0 && c.Altitude.Degree != 0) {
                AltExpression.Definition = c.Altitude.Degree.ToString();
            }
            if (AzExpression.Definition.Length == 0 && c.Azimuth.Degree != 0) {
                AzExpression.Definition = c.Azimuth.Degree.ToString();
            }
        }

        private IProfileService profileService;
        private ITelescopeMediator telescopeMediator;
        private IGuiderMediator guiderMediator;

        [JsonProperty]
        public InputTopocentricCoordinates Coordinates { get; set; }

        private IList<string> issues = new List<string>();

        private bool Protect = false;

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        [IsExpression (Range = [-90, 90], HasValidator = true)]
        private double alt;

        partial void AltExpressionValidator(Logic.Expression expr) {
            // When the decimal value changes, we update the HMS values
            InputTopocentricCoordinates ic = new InputTopocentricCoordinates(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude), profileService.ActiveProfile.AstrometrySettings.Elevation);
            Protect = true;
            ic.Coordinates.Altitude = Angle.ByDegree(AltExpression.Value);
            Coordinates.AltDegrees = ic.AltDegrees;
            Coordinates.AltMinutes = ic.AltMinutes;
            Coordinates.AltSeconds = ic.AltSeconds;
            Protect = false;
        }

        [IsExpression (Range = [0, 360], HasValidator = true)]
        private double az;

        partial void AzExpressionValidator(Logic.Expression expr) {
            // When the decimal value changes, we update the HMS values
            InputTopocentricCoordinates ic = new InputTopocentricCoordinates(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude), profileService.ActiveProfile.AstrometrySettings.Elevation);
            Protect = true;
            ic.Coordinates.Azimuth = Angle.ByDegree(AzExpression.Value);
            Coordinates.AzDegrees = ic.AzDegrees;
            Coordinates.AzMinutes = ic.AzMinutes;
            Coordinates.AzSeconds = ic.AzSeconds;
            Protect = false;
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (telescopeMediator.GetInfo().AtPark) {
                Notification.ShowError(Loc.Instance["LblTelescopeParkedWarning"]);
                throw new SequenceEntityFailedException(Loc.Instance["LblTelescopeParkedWarning"]);
            }
            var stoppedGuiding = await guiderMediator.StopGuiding(token);
            await telescopeMediator.SlewToCoordinatesAsync(Coordinates.Coordinates, token);
            if (stoppedGuiding) {
                await guiderMediator.StartGuiding(false, progress, token);
            }
        }

        private Angle lastAlt;
        private Angle lastAz;

        protected void Coordinates_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            // When coordinates change, we change the decimal value
            InputTopocentricCoordinates ic = (InputTopocentricCoordinates)sender;
            TopocentricCoordinates c = ic.Coordinates;

            if (Protect) return;

            if (c.Altitude != lastAlt) {
                AltExpression.Definition = Math.Round(c.Altitude.Degree, 7).ToString();
            } else if (c.Azimuth != lastAz) {
                AzExpression.Definition = Math.Round(c.Azimuth.Degree, 7).ToString();
            }

            lastAlt = c.Altitude;
            lastAz = c.Azimuth;
        }

        public override void AfterParentChanged() {
            AltExpression.Context = this;
            AzExpression.Context = this;
            if (Coordinates != null) {
                Coordinates.PropertyChanged += Coordinates_PropertyChanged;
            }
            Validate();
        }

        public bool Validate() {
            var i = new List<string>();
            if (!telescopeMediator.GetInfo().Connected) {
                i.Add(Loc.Instance["LblTelescopeNotConnected"]);
            }
            Logic.Expression.ValidateExpressions(i, AltExpression, AzExpression);
            Issues = i;
            return i.Count == 0;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(SlewScopeToAltAz)}, Coordinates: {Coordinates}";
        }
    }
}