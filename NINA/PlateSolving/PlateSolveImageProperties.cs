using NINA.Model.ImageData;
using NINA.Utility.Astrometry;

namespace NINA.PlateSolving {

    internal class PlateSolveImageProperties {
        public double? FocalLength { get; private set; }
        public double? PixelSize { get; private set; }
        public double ImageWidth { get; private set; }
        public double ImageHeight { get; private set; }

        public double? ArcSecPerPixel {
            get {
                if (!PixelSize.HasValue || !FocalLength.HasValue) {
                    return null;
                }
                return Astrometry.ArcsecPerPixel(PixelSize.Value, FocalLength.Value);
            }
        }

        public double? FoVH {
            get {
                if (!ArcSecPerPixel.HasValue) {
                    return null;
                }
                return Astrometry.ArcminToDegree(Astrometry.FieldOfView(ArcSecPerPixel.Value, ImageHeight));
            }
        }

        public double? FoVW {
            get {
                if (!ArcSecPerPixel.HasValue) {
                    return null;
                }
                return Astrometry.ArcminToDegree(Astrometry.FieldOfView(ArcSecPerPixel.Value, ImageWidth));
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