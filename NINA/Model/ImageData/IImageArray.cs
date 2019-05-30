namespace NINA.Model.ImageData {

    public interface IImageArray {
        ushort[] FlatArray { get; }
        byte[] RAWData { get; set; }
        string RAWType { get; set; }
    }
}