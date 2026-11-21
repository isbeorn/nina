#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Locale;
using NINA.Core.Utility;
using static NINA.Sequencer.Utility.ItemUtility;
using NINA.Sequencer.SequenceItem.Telescope;
using NINA.Sequencer.Generators;
using NINA.Sequencer.Logic;
using System.Runtime.Serialization;

namespace NINA.Sequencer.SequenceItem.Utility {

    [ExportMetadata("Name", "Lbl_SequenceItem_Utility_WaitUntilAboveHorizon_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Utility_WaitUntilAboveHorizon_Description")]
    [ExportMetadata("Icon", "WaitForAltitudeSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Utility")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions]

    public partial class WaitUntilAboveHorizon : CoordinatesInstruction, IValidatable {

        private IProfileService ProfileService;

        [ImportingConstructor]
        public WaitUntilAboveHorizon(IProfileService profileService) : base() {
            Data = new WaitLoopData(profileService, true, GetType().Name);
            Data.Offset = 30;
            // An explicit definition instead of the grey default hint
            UpperOffsetExpression.Definition = "0";
            PropertyChanged += RebindReplacedUpperOffsetExpression;
            BindEffectiveCoordinates();
            ProfileService = profileService;
        }

        private WaitUntilAboveHorizon(WaitUntilAboveHorizon cloneMe) : this(cloneMe.ProfileService) {
            CopyMetaData(cloneMe);
        }

        partial void AfterClone(WaitUntilAboveHorizon clone) {
            UpdateExpressions(clone, this);
            clone.Data = Data.Clone();
            clone.Coordinates = clone.Data.Coordinates;
        }

        [OnDeserialized]
        public new void OnDeserialized(StreamingContext context) {
            if (OffsetExpression.Definition.Length == 0) {
                OffsetExpression.Definition = Data.Offset.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Signed safety margin below the upper horizon: the target has to stay at or below [upper horizon] - UpperOffset,
        /// so a positive value lowers the limit and a negative value raises it.
        /// Independent of <see cref="CoordinatesInstruction.Offset"/>, which only applies to the lower horizon.
        /// </summary>
        [IsExpression(Default = 0, Range = [-90, 90], HasValidator = true)]
        public partial double UpperOffset { get; set; }

        partial void UpperOffsetExpressionValidator(Expression expr) {
            if (expr.Error == null && Data != null) {
                Data.UpperOffset = expr.Value;
            }
        }

        /// <summary>
        /// Binds a replaced <see cref="UpperOffsetExpression"/> to this item as its context and validator. Plain
        /// <c>[UsesExpressions]</c> does not do that (generated validation does), and without them the offset would neither
        /// resolve variables nor follow later edits.
        /// </summary>
        private void RebindReplacedUpperOffsetExpression(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName != nameof(UpperOffsetExpression)) {
                return;
            }

            UpperOffsetExpression.Context = this;
            UpperOffsetExpression.Validator = UpperOffsetExpressionValidator;
        }

        /// <summary>
        /// Evaluates the offset expressions and gives the shared altitude calculator the evaluated values. The upper offset
        /// is handed over explicitly, because its validator only runs when the value changes and the data can be replaced
        /// after the expression (e.g. while cloning).
        /// </summary>
        private void RefreshOffsets() {
            if (OffsetExpression.Definition.Length > 0) {
                _ = Offset;
            }

            if (UpperOffsetExpression.Definition.Length > 0) {
                _ = UpperOffset;
            }

            // The validator only runs when the value changes, and the data can have been replaced since (e.g. by cloning),
            // so the calculator is given the evaluated value explicitly.
            UpperOffsetExpressionValidator(UpperOffsetExpression);
        }

        protected override bool UsesDataCoordinates => true;

        public int UpdateInterval { get; set; } = 1;

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            do {
                RefreshOffsets();

                var valid = Data.UpdateCurrentTargetPosition(Data.Coordinates.Coordinates.DateTime.Now);

                var aboveUpperHorizon = valid && Data.HasUpperHorizon && Data.CurrentAltitude > Data.UpperTargetAltitude;
                progress?.Report(new ApplicationStatus() {
                    Status = aboveUpperHorizon
                        ? string.Format(Loc.Instance["Lbl_SequenceItem_Utility_WaitUntilAboveHorizon_ProgressUpper"], Math.Round(Data.CurrentAltitude, 2), Math.Round(Data.UpperTargetAltitude, 2))
                        : string.Format(Loc.Instance["Lbl_SequenceItem_Utility_WaitUntilAboveHorizon_Progress"], Math.Round(Data.CurrentAltitude, 2), Math.Round(Data.TargetAltitude, 2))
                });

                if (valid && Data.CurrentAltitude > Data.TargetAltitude && !aboveUpperHorizon) {
                    Logger.Info("WaitUntilAboveHorizon finished: " + Data.CurrentAltitude + " > " + Data.TargetAltitude + " Offset = " + Data.Offset);
                    break;
                } else {
                    _ = await CoreUtil.Delay(TimeSpan.FromSeconds(UpdateInterval), token);
                }
            } while (true);
        }

        public double GetCurrentAltitude(DateTime time, ObserverInfo observer) {
            var altaz = Data.Coordinates.Coordinates.Transform(Angle.ByDegree(observer.Latitude), Angle.ByDegree(observer.Longitude), observer.Elevation, time);
            return altaz.Altitude.Degree;
        }

        public void CalculateExpectedTime() {
            CalculateTargetCrossing();
        }

        private TargetCrossingResult CalculateTargetCrossing() {
            RefreshOffsets();

            return Data.CalculateTargetExpectedTime(DateTime.Now, TargetCrossingComparison.AboveStrict);
        }


        public override void AfterParentChanged() {
            base.AfterParentChanged();
            Validate();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(WaitUntilAboveHorizon)}";
        }

        public bool Validate() {
            ResolveEffectiveCoordinates();
            RefreshOffsets();
            Issues.Clear();

            var maxAlt = AstroUtil.GetAltitude(0, Data.Latitude, Data.Coordinates.Coordinates.Dec);
            var minHorizonAlt = (Data.Horizon?.GetMinAltitude() ?? 0) + Data.Offset;

            if (maxAlt < minHorizonAlt) {
                Issues.Add(Loc.Instance["LblUnreachableAltitudeForHorizon"]);
                Data.ExpectedTime = "--";
            } else {
                var crossing = CalculateTargetCrossing();
                if (Data.HasUpperHorizon && crossing.Status == TargetCrossingStatus.NoEvent) {
                    // Within the next 24 hours the target is never between the lower and the upper horizon
                    Issues.Add(Loc.Instance["LblUnreachableTargetForSkyWindow"]);
                    Data.ExpectedTime = "--";
                }
            }

            Expression.ValidateExpressions(Issues, OffsetExpression, PositionAngleExpression);
            ValidateOwnExpressions(Issues);
            if (!Inherited) {
                Expression.ValidateExpressions(Issues, RaExpression, DecExpression);
            }

            RaisePropertyChanged("Issues");
            return Issues.Count == 0;
        }
    }
}
