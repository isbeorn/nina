using NINA.Utility.Astrometry;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINA.Utility.SkySurvey {

    internal class SkyAtlasSkySurvey : ISkySurvey {

        public async Task<SkySurveyImage> GetImage(string name, Coordinates coordinates, double fieldOfView, CancellationToken ct, IProgress<int> progress) {
            byte[] arr = new byte[2048 * 2048];
            for (int i = 0; i < arr.Length; i++) {
                arr[i] = 30;
            }

            BitmapSource bitmap = BitmapSource.Create(2048, 2048, 96, 96, PixelFormats.Gray8, BitmapPalettes.Gray256, arr, 2048);

            bitmap.Freeze();

            return new SkySurveyImage {
                Name = name,
                Source = nameof(SkyAtlasSkySurvey),
                Image = bitmap,
                FoVHeight = fieldOfView,
                FoVWidth = fieldOfView,
                Rotation = 0,
                Coordinates = coordinates
            };
        }
    }
}