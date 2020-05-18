#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.MyFilterWheel;
using NINA.Utility;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace NINA.Profile {

    [Serializable()]
    [DataContract]
    public class FilterWheelSettings : Settings, IFilterWheelSettings {

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            SetDefaultValues();
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            if (filterWheelFilters != null) {
                // set default flatwizardsettings
                foreach (FilterInfo filter in filterWheelFilters.Where(f => f.FlatWizardFilterSettings == null)) {
                    filter.FlatWizardFilterSettings = new FlatWizardFilterSettings();
                }
            } else {
                filterWheelFilters = new ObserveAllCollection<FilterInfo>();
            }
        }

        protected override void SetDefaultValues() {
            id = "No_Device";
            filterWheelFilters = new ObserveAllCollection<FilterInfo>();
        }

        private string id;

        [DataMember]
        public string Id {
            get {
                return id;
            }
            set {
                if (id != value) {
                    id = value;
                    RaisePropertyChanged();
                }
            }
        }

        private void FilterWheelFilters_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            RaisePropertyChanged(nameof(FilterWheelFilters));
        }

        private ObserveAllCollection<FilterInfo> filterWheelFilters;

        [DataMember]
        public ObserveAllCollection<FilterInfo> FilterWheelFilters {
            get {
                return filterWheelFilters;
            }
            set {
                if (filterWheelFilters != value) {
                    if (filterWheelFilters != null) {
                        filterWheelFilters.CollectionChanged -= FilterWheelFilters_CollectionChanged;
                    }
                    filterWheelFilters = value;
                    if (filterWheelFilters != null) {
                        filterWheelFilters.CollectionChanged += FilterWheelFilters_CollectionChanged;
                    }
                    RaisePropertyChanged();
                }
            }
        }
    }
}
