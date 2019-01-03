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
using NINA.Utility.Mediator;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace NINA.Utility.Profile {

    [Serializable()]
    [DataContract]
    public class FilterWheelSettings : Settings, IFilterWheelSettings {
        private string id = "No_Device";

        [DataMember]
        public string Id {
            get {
                return id;
            }
            set {
                id = value;
                RaisePropertyChanged();
            }
        }

        [OnDeserialized]
        public void Deserializing(StreamingContext context) {
            // set default flatwizardsettings
            foreach (FilterInfo filter in filterWheelFilters.Where(f => f.FlatWizardFilterSettings == null)) {
                filter.FlatWizardFilterSettings = new FlatWizardFilterSettings();
            }
        }

        private ObserveAllCollection<FilterInfo> filterWheelFilters;

        [DataMember]
        public ObserveAllCollection<FilterInfo> FilterWheelFilters {
            get {
                if (filterWheelFilters == null) {
                    filterWheelFilters = new ObserveAllCollection<FilterInfo>();
                    /*for (short i = 0; i < 8; i++) {
                        filterWheelFilters.Add(new FilterInfo(Locale.Loc.Instance["LblFilter"] + (i + 1), 0, i, 0));
                    }*/
                }
                return filterWheelFilters;
            }
            set {
                filterWheelFilters = value;
                RaisePropertyChanged();
            }
        }
    }
}