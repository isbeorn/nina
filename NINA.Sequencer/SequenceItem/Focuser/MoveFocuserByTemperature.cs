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
using NINA.Core.Utility;
using NINA.Sequencer.Generators;
using NINA.Sequencer.Logic;

namespace NINA.Sequencer.SequenceItem.Focuser {

    [ExportMetadata("Name", "Lbl_SequenceItem_Focuser_MoveFocuserByTemperature_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Focuser_MoveFocuserByTemperature_Description")]
    [ExportMetadata("Icon", "MoveFocuserByTemperatureSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Focuser")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions]

    public partial class MoveFocuserByTemperature : SequenceItem, IValidatable, IFocuserConsumer {

        [ImportingConstructor]
        public MoveFocuserByTemperature(IFocuserMediator focuserMediator) {
            this.focuserMediator = focuserMediator;
        }

        private MoveFocuserByTemperature(MoveFocuserByTemperature cloneMe) : this(cloneMe.focuserMediator) {
            CopyMetaData(cloneMe);
        }

        partial void AfterClone(MoveFocuserByTemperature clone) {
            Absolute = Absolute;
        }

        private IFocuserMediator focuserMediator;

        [IsExpression (Default = 1)]
        private double slope = 1;

        private static double lastTemperature = -1000;

        private static double lastRoundoff = 0;

        private bool absolute = true;

        [JsonProperty]
        public bool Absolute {
            get => absolute;
            set {
                absolute = value;
                RaisePropertyChanged();
            }
        }

        [IsExpression (Default = 0)]
        private double intercept = 0;

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var info = focuserMediator.GetInfo();
            Task<int> result;

            if (absolute) {
                double position = Slope * info.Temperature + Intercept;
                Logger.Info($"Moving Focuser By Temperature - Slope {Slope} * Temperature {info.Temperature} °C + Intercept {Intercept} = Position {position}");
                result = focuserMediator.MoveFocuser((int)position, token);
            } else {
                Logger.Info($"Moving Focuser By Temperature Relative - Slope {Slope}");
                result = focuserMediator.MoveFocuserByTemperatureRelative(info.Temperature, slope, token);
            }

            return result;
        }

        public bool Validate() {
            var i = new List<string>();
            var info = focuserMediator.GetInfo();
            if (!info.Connected) {
                i.Add(Loc.Instance["LblFocuserNotConnected"]);
            } else {
                if (double.IsNaN(info.Temperature)) {
                    i.Add(Loc.Instance["Lbl_SequenceItem_Focuser_MoveFocuserByTemperature_Validation_NoTemperature"]);
                }
            }
            Expression.ValidateExpressions(i, SlopeExpression, InterceptExpression);
            Issues = i;
            return i.Count == 0;
        }

        public string MiniDescription => $"{Slope} * " + (absolute ? $"T + {Intercept}" : "DeltaT");

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(MoveFocuserByTemperature)}, Slope: {Slope}" + (absolute ? $", Intercept {Intercept}" : " (Relative mode)");
        }

        public void UpdateDeviceInfo(Equipment.Equipment.MyFocuser.FocuserInfo deviceInfo) {
            ;
        }

        public void Dispose() {
        }

        public void UpdateEndAutoFocusRun(AutoFocusInfo info) {
            Logger.Info($"Autofocus notification received - Temperature {info.Temperature}");
            lastTemperature = info.Temperature;
            lastRoundoff = 0;
        }

        public void UpdateUserFocused(Equipment.Equipment.MyFocuser.FocuserInfo info) {
            Logger.Info($"User Focused notification received - Temperature {info.Temperature}");
            lastTemperature = info.Temperature;
            lastRoundoff = 0;
        }
    }
}