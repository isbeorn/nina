using NINA.Model;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.SkySurvey;
using System.Linq;
using System.Windows;

namespace NINA.ViewModel.FramingAssistant {

    internal class FramingDSO : BaseINPC {
        private const int DSO_DEFAULT_SIZE = 30;

        private readonly double arcSecWidth;
        private readonly double arcSecHeight;
        private readonly double sizeWidth;
        private readonly double sizeHeight;
        private Point topLeftPoint;
        private readonly Point imageCenterPoint;

        /// <summary>
        /// Constructor for a Framing DSO.
        /// It takes a SkySurveyImage and a DeepSkyObject and calculates XY values in pixels from the top left edge of the image subtracting half of its size.
        /// Those coordinates can be used to place the DSO including its name and size in any given image.
        /// </summary>
        /// <param name="dso">The DSO including its coordinates</param>
        /// <param name="image">The image where the DSO should be placed in including the RA/Dec coordinates of the center of that image</param>
        public FramingDSO(DeepSkyObject dso, SkySurveyImage image) {
            arcSecWidth = Astrometry.ArcminToArcsec(image.FoVWidth) / image.Image.PixelWidth;
            arcSecHeight = Astrometry.ArcminToArcsec(image.FoVHeight) / image.Image.PixelHeight;

            if (dso.Size != null && dso.Size >= arcSecWidth) {
                sizeWidth = dso.Size.Value;
            } else {
                sizeWidth = DSO_DEFAULT_SIZE;
            }

            if (dso.Size != null && dso.Size >= arcSecHeight) {
                sizeHeight = dso.Size.Value;
            } else {
                sizeHeight = DSO_DEFAULT_SIZE;
            }

            Id = dso.Id;
            Name1 = dso.Name;
            Name2 = dso.AlsoKnownAs.FirstOrDefault(m => m.StartsWith("M "));
            Name3 = dso.AlsoKnownAs.FirstOrDefault(m => m.StartsWith("NGC "));

            if (Name3 != null && Name1 == Name3.Replace(" ", "")) {
                Name1 = null;
            }

            if (Name1 == null && Name2 == null) {
                Name1 = Name3;
                Name3 = null;
            }

            if (Name1 == null && Name2 != null) {
                Name1 = Name2;
                Name2 = Name3;
                Name3 = null;
            }

            //topLeftPoint = dso.Coordinates.ProjectFromCenterToXY(image.Coordinates, new Point(image.Image.PixelWidth / 2.0, image.Image.PixelHeight / 2.0),
            //  arcSecWidth, arcSecHeight, image.Rotation);

            imageCenterPoint = new Point(image.Image.PixelWidth / 2.0, image.Image.PixelHeight / 2.0);
            rotation = image.Rotation;
            coordinates = dso.Coordinates;

            RecalculateTopLeft(image.Coordinates);
        }

        private Coordinates coordinates;
        private double rotation;

        public void RecalculateTopLeft(Coordinates reference) {
            var projectedPoint = coordinates.ProjectFromCenterToXY(
                reference,
                imageCenterPoint,
                arcSecWidth,
                arcSecHeight,
                rotation
            );
            TopLeftPoint = new Point(projectedPoint.X - SizeWidth / 2, projectedPoint.Y - SizeHeight / 2);
        }

        public double SizeWidth => sizeWidth / arcSecWidth;

        public double SizeHeight => sizeHeight / arcSecHeight;

        public Point TopLeftPoint {
            get => topLeftPoint;
            private set {
                topLeftPoint = value;
                RaisePropertyChanged();
            }
        }

        public string Id { get; }
        public string Name1 { get; }
        public string Name2 { get; }
        public string Name3 { get; }
    }
}