namespace NINA.Profile {

    public interface IFlatDeviceSettings : ISettings {
        string Id { get; set; }
        string Name { get; set; }
        string PortName { get; set; }
    }
}