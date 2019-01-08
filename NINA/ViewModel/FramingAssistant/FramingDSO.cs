using NINA.Model;
using NINA.Utility;
using NINA.Utility.Astrometry;
using System.Linq;
using System.Windows;

namespace NINA.ViewModel.FramingAssistant {

    internal class FramingDSO : BaseINPC {
        private const int DSO_DEFAULT_SIZE = 30;

        private readonly double arcSecWidth;
        private readonly double arcSecHeight;
        private readonly double sizeWidth;
        private readonly double sizeHeight;
        private Coordinates coordinates;
        private Point topLeftPoint;

        /// <summary>
        /// Constructor for a Framing DSO.
        /// It takes a ViewportFoV and a DeepSkyObject and calculates XY values in pixels from the top left edge of the image subtracting half of its size.
        /// Those coordinates can be used to place the DSO including its name and size in any given image.
        /// </summary>
        /// <param name="dso">The DSO including its coordinates</param>
        /// <param name="viewport">The viewport of the offending DSO</param>
        public FramingDSO(DeepSkyObject dso, ViewportFoV viewport) {
            arcSecWidth = viewport.ArcSecWidth;
            arcSecHeight = viewport.ArcSecHeight;

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

            coordinates = dso.Coordinates;

            RecalculateTopLeft(viewport);
        }

        public void RecalculateTopLeft(ViewportFoV reference) {
            var projectedPoint = coordinates.GnomonicTanProjection(reference);
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