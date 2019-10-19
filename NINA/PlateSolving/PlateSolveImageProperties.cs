using NINA.Model.ImageData;
using NINA.Utility.Astrometry;

namespace NINA.PlateSolving {

    internal class PlateSolveImageProperties {
        public double FocalLength { get; private set; }
        public double PixelSize { get; private set; }
        public double ImageWidth { get; private set; }
        public double ImageHeight { get; private set; }

        public double ArcSecPerPixel {
            get {
                return Astrometry.ArcsecPerPixel(PixelSize, FocalLength);
            }
        }

        public double FoVH {
            get {
                return Astrometry.ArcminToDegree(Astrometry.FieldOfView(ArcSecPerPixel, ImageHeight));
            }
        }

        public double FoVW {
            get {
                return Astrometry.ArcminToDegree(Astrometry.FieldOfView(ArcSecPerPixel, ImageWidth));
            }
        }

        public static PlateSolveImageProperties Create(PlateSolveParameter parameter, IImageData source) {
            return new PlateSolveImageProperties() {
                FocalLength = parameter.FocalLength,
                PixelSize = parameter.PixelSize,
                ImageWidth = source.Properties.Width,
                ImageHeight = source.Properties.Height
            };
        }
    }
}