using NINA.Model.MyFilterWheel;

namespace NINA.Utility.Profile {

    public interface IFilterWheelSettings : ISettings {
        ObserveAllCollection<FilterInfo> FilterWheelFilters { get; set; }
        string Id { get; set; }
    }
}