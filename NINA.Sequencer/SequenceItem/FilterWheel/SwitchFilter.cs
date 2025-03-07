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
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Model.Equipment;
using NINA.Core.Locale;
using System.Windows;
using NINA.Core.Utility;
using NINA.Sequencer.Generators;
using NINA.Profile;
using NINA.Equipment.Equipment.MyFilterWheel;
using Accord.Statistics.Models.Regression.Fitting;

namespace NINA.Sequencer.SequenceItem.FilterWheel {

    [ExportMetadata("Name", "Lbl_SequenceItem_FilterWheel_SwitchFilter_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_FilterWheel_SwitchFilter_Description")]
    [ExportMetadata("Icon", "FW_NoFill_SVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_FilterWheel")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions]

    public partial class SwitchFilter : SequenceItem, IValidatable {

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            MatchFilter();
            if (Filter != null) {
                ComboBoxText = Filter.Name;
            } else {
                ComboBoxText = "{Current}";
            }
        }

        [OnSerialized]
        public void Serialized(StreamingContext context) {
        }

        [OnSerializing]
        public void Serializing(StreamingContext context) {
            Filter = null;
        }


        [ImportingConstructor]
        public SwitchFilter(IProfileService profileservice, IFilterWheelMediator filterWheelMediator) {
            this.profileService = profileservice;
            this.filterWheelMediator = filterWheelMediator;

            WeakEventManager<IProfileService, EventArgs>.AddHandler(profileService, nameof(profileService.ProfileChanged), ProfileService_ProfileChanged);
        }

        private void MatchFilter() {
            try {
                var idx = this.Filter?.Position ?? -1;
                this.filter = this.profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters?.FirstOrDefault(x => x.Name == this.Filter?.Name);
                if (this.Filter == null && idx >= 0) {
                    this.filter = this.profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters?.FirstOrDefault(x => x.Position == idx);
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            MatchFilter();
        }

        private SwitchFilter(SwitchFilter cloneMe) : this(cloneMe.profileService, cloneMe.filterWheelMediator) {
            CopyMetaData(cloneMe);
        }

        partial void AfterClone(SwitchFilter clone) {
            clone.ComboBoxText = this.ComboBoxText;
            clone.filter = filter;
        }

        [IsExpression]
        private int xfilter;

        private IProfileService profileService;
        private IFilterWheelMediator filterWheelMediator;

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        private FilterInfo filter;

        [JsonProperty]
        public FilterInfo Filter {
            get => filter;
            set {
                filter = value;
                RaisePropertyChanged();
            }
        }

        private int selectedFilter;

        public int SelectedFilter {
            get => selectedFilter;
            set {
                if (value == 0) {
                    Filter = null;
                    ComboBoxText = "{Current}";
                } else {
                    Filter = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters[value - 1];
                    ComboBoxText = Filter.Name;
                }
            }
        }


        private List<string> iFilterNames = new List<string>();
        public List<string> FilterNames {
            get => iFilterNames;
            set {
                iFilterNames = value;
            }
        }

        private string comboBoxText = "";

        [JsonProperty]
        public string ComboBoxText {
            get {
                return comboBoxText;
            }
            set {
                comboBoxText = value;

                if (comboBoxText == "{Current}") {
                    FilterWheelInfo info = filterWheelMediator.GetInfo();
                    if (info.Connected) {
                        Filter = info.SelectedFilter;
                    }
                } else {
                    Filter = profileService.ActiveProfile?.FilterWheelSettings?.FilterWheelFilters?.FirstOrDefault(x => x.Name == comboBoxText);
                    if (Filter == null) {
                        XfilterExpression.Definition = comboBoxText;
                        if (xfilterExpression.Error == null) {
                            Filter = profileService.ActiveProfile?.FilterWheelSettings?.FilterWheelFilters?.FirstOrDefault(x => x.Position == xfilterExpression.Value);
                        }
                    } else {
                        xfilterExpression.Definition = "";
                    }
                }

                RaisePropertyChanged();
            }
        }
        

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            return Filter == null
                ? throw new SequenceItemSkippedException("Skipping SwitchFilter - No Filter was selected")
                : filterWheelMediator.ChangeFilter(Filter, token, progress);
        }

        public bool Validate() {
            var i = new List<string>();

            if (filter != null && !filterWheelMediator.GetInfo().Connected) {
                i.Add(Loc.Instance["LblFilterWheelNotConnected"]);
            } else {
                if (FilterNames.Count == 0) {
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
    }
}