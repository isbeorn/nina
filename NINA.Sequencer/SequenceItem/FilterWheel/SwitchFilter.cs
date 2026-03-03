#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Accord.Statistics.Models.Regression.Fitting;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Generators;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NINA.Sequencer.SequenceItem.FilterWheel {

    [ExportMetadata("Name", "Lbl_SequenceItem_FilterWheel_SwitchFilter_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_FilterWheel_SwitchFilter_Description")]
    [ExportMetadata("Icon", "FW_NoFill_SVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_FilterWheel")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions]

    public partial class SwitchFilter : SequenceItem, IValidatable {

        private IProfileService profileService;
        private IFilterWheelMediator filterWheelMediator;
        private static string NullFilterName => NullFilter.Instance.Name;

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            if (Filter != null && string.IsNullOrEmpty(ComboBoxText)) {
                // This is the upgrade from 3.2 case
                SetupFilter(Filter.Name);
            } else if (string.IsNullOrWhiteSpace(ComboBoxText)) {
                SetupFilter(NullFilterName);
            }
        }

        [OnSerializing]
        public void Serializing(StreamingContext context) {
            // We only save this in 3.2
            Filter = null;
        }


        [ImportingConstructor]
        public SwitchFilter(IProfileService profileservice, IFilterWheelMediator filterWheelMediator) {
            this.profileService = profileservice;
            this.filterWheelMediator = filterWheelMediator;

            WeakEventManager<IProfileService, EventArgs>.AddHandler(profileService, nameof(profileService.ProfileChanged), ProfileService_ProfileChanged);
        }

        private void SetupFilter(string filterString) {
            try {
                // Clone
                if (filterString == null) return;

                // Use current filter selection, if we're connected
                if (filterString == NullFilter.Instance.Name) {
                    FilterWheelInfo info = filterWheelMediator.GetInfo();
                    if (info.Connected) {
                        filter = info.SelectedFilter;
                    }
                    comboBoxText = filterString;
                    return;
                }

                // Setting the definition will lead to Evaluation
                XfilterExpression.Definition = filterString;
                // Simplest case is that the string is the name of a filter in the wheel
                filter = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters?.FirstOrDefault(x => x.Name == filterString);
                // If not, assume it's an Expression and find its value
                if (Filter == null) {
                    filter = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters?.FirstOrDefault(x => x.Position == (int)XfilterExpression.Value);
                }
                // Don't recurse; comboTextBox could be set from different places (including upgrade from 3.2)
                comboBoxText = filterString;
                RaisePropertyChanged(nameof(Filter));
                RaisePropertyChanged(nameof(ComboBoxText));
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            // We might have very different filter names
            FilterNames.Clear();
            // Force setup of current names
            Validate();
            // Find/setup the filter
            SetupFilter(ComboBoxText);
        }

        private SwitchFilter(SwitchFilter cloneMe) : this(cloneMe.profileService, cloneMe.filterWheelMediator) {
            CopyMetaData(cloneMe);
        }

        partial void AfterClone(SwitchFilter clone) {
            clone.comboBoxText = comboBoxText;
            SetupFilter(comboBoxText);
        }

        [IsExpression]
        public partial int Xfilter { get; set; }

        // Intentionally using this instead of field
        // Filter (capital F) is ONLY set during upgrade from 3.2
        private FilterInfo filter;

        [JsonProperty]
        public FilterInfo Filter {
            get => filter;
            set {
                filter = value;
                // ONLY upgrades from 3.2 come here...
                // This ensures that ComboBoxText is set properly
                SetupFilter(filter != null ? filter.Name : NullFilterName);
                RaisePropertyChanged();
            }
        }

        public int SelectedFilter {
            get => field;
            set {
                // This is the case in which user selected from the ComboBox
                field = value;
                SetupFilter(value == 0 ? NullFilterName : profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters[value - 1].Name);
            }
        }

        // Intentionally using this instead of field
        private string comboBoxText;

        [JsonProperty]
        public string ComboBoxText {
            get => comboBoxText;
            set {
                // We come here from a number of places: Expression entry from ExprComboControl, upgrades from 3.2, or XfilterExpression evaluation.
                // In the first two cases, we want to resolve the filter and set the expression; in the last case, we just want to update the ComboBoxText
                // to match the expression's value without re-resolving the filter (which would cause a loop)
                comboBoxText = value;
                SetupFilter(value);
            }
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (filterWheelMediator.GetInfo().Connected) {
                // ComboBoxText might have been set before a FW was connected, or a Symbol's value might have changed, so we need to re-resolve the filter here
                XfilterExpression.Evaluate(true);
                filter = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters?.FirstOrDefault(x => x.Position == (int)XfilterExpression.Value);
            } else {
                filter = null;
            }

            return Filter == null
                ? throw new SequenceItemSkippedException("Skipping SwitchFilter - No Filter was selected")
                : filterWheelMediator.ChangeFilter(Filter, token, progress);
        }


        private List<string> iFilterNames = new List<string>();
        public List<string> FilterNames {
            get => iFilterNames;
            set {
                iFilterNames = value;
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

        public bool Validate() {
            var i = new List<string>();

            if (filter != null && !filterWheelMediator.GetInfo().Connected) {
                i.Add(Loc.Instance["LblFilterWheelNotConnected"]);
            } else {
                if (FilterNames.Count == 0) {
                    // Lazy instantiation of FilterNames
                    var fwi = profileService.ActiveProfile?.FilterWheelSettings?.FilterWheelFilters;
                    if (fwi != null) {
                        foreach (var fw in fwi) {
                            FilterNames.Add(fw.Name);
                        }
                        RaisePropertyChanged("FilterNames");
                    }
                }
            }

            Logic.Expression.ValidateExpressions(i, XfilterExpression);

            Issues = i;
            RaisePropertyChanged("Issues");
            return Issues.Count == 0;
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(SwitchFilter)}, Filter: {Filter?.Name}";
        }

        // We don't want any of these serialized; only ComboBoxTest
        public bool ShouldSerializeXfilterExpression() {
            return false;
        }
        public bool ShouldSerializeXfilter() {
            return false;
        }
        public bool ShouldSerializeXfilterDefinition() {
            return false;
        }
    }
}