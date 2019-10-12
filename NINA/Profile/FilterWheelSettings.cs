#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
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