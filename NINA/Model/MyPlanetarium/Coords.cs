namespace NINA.Model.MyPlanetarium {

    /// <summary>
    /// This class is a glorified double[3]
    /// the name is NOT Coordinates to avoid confusion with Astrometry.Coordinates
    /// </summary>
    internal class Coords {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Elevation { get; set; }
    }
}