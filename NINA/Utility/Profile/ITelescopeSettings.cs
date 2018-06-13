namespace NINA.Utility.Profile {
    public interface ITelescopeSettings {
        int FocalLength { get; set; }
        string Id { get; set; }
        int SettleTime { get; set; }
        string SnapPortStart { get; set; }
        string SnapPortStop { get; set; }
    }
}