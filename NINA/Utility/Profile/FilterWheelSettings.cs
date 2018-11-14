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