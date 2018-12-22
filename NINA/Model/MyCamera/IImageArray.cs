namespace NINA.Model.MyCamera {

    public interface IImageArray {
        byte[] RAWData { get; set; }
        string RAWType { get; set; }
        IImageStatistics Statistics { get; set; }
    }
}