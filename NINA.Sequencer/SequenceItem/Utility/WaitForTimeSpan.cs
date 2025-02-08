#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Math;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Sequencer.Generators;
using NINA.Sequencer.Logic;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.SequenceItem.Utility {

    [ExportMetadata("Name", "Lbl_SequenceItem_Utility_WaitForTimeSpan_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Utility_WaitForTimeSpan_Description")]
    [ExportMetadata("Icon", "HourglassSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Utility")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    [ExpressionObject]

    public partial class WaitForTimeSpan : SequenceItem, IValidatable {

        [ImportingConstructor]
        public WaitForTimeSpan() {
        }

        private WaitForTimeSpan(WaitForTimeSpan cloneMe) : base(cloneMe) {
        }

        [IsExpression (Default = 60, Range = [1, 0])]
        private double time;

        private IList<string> issues = new List<string>();


        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            TimeExpression.Evaluate();
            var a = TimeExpression.Value;
            return NINA.Core.Utility.CoreUtil.Wait(GetEstimatedDuration(), true, token, progress, "");            
        }

        public override TimeSpan GetEstimatedDuration() {
            return TimeSpan.FromSeconds(Time);
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(WaitForTimeSpan)}, Time: {Time}s";
        }

        public bool Validate() {
            Issues.Clear();
            Expression.ValidateExpressions(Issues, TimeExpression);
            RaisePropertyChanged("Issues");
            return Issues.Count == 0;
        }
    }
}