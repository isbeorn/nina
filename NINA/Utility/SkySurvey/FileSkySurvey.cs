using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using NINA.Model;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Profile;
using NINA.ViewModel;

namespace NINA.Utility.SkySurvey {

    internal class FileSkySurvey : ISkySurvey {

        public async Task<SkySurveyImage> GetImage(string name, Coordinates coordinates, double fieldOfView, CancellationToken ct, IProgress<int> progress) {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = Locale.Loc.Instance["LblLoadImage"];
            dialog.FileName = "";
            dialog.DefaultExt = ".tif";
            dialog.Multiselect = false;
            dialog.Filter = "Image files|*.tif;*.tiff;*.jpeg;*.jpg;*.png|TIFF files|*.tif;*.tiff;|JPEG files|*.jpeg;*.jpg|PNG Files|*.png";

            if (dialog.ShowDialog() == true) {
                BitmapSource img = null;
                switch (Path.GetExtension(dialog.FileName)) {
                    case ".tif":
                    case ".tiff":
                        img = LoadTiff(dialog.FileName);
                        break;

                    case ".png":
                        img = LoadPng(dialog.FileName);
                        break;

                    case ".jpg":
                        img = LoadJpg(dialog.FileName);
                        break;
                }

                if (img == null) {
                    return null;
                }

                return new SkySurveyImage() {
                    Name = Path.GetFileNameWithoutExtension(dialog.FileName),
                    Coordinates = null,
                    FoVHeight = double.NaN,
                    FoVWidth = double.NaN,
                    Image = img,
                    Rotation = double.NaN
                };
            } else {
                return null;
            }
        }

        private BitmapSource LoadPng(string filename) {
            PngBitmapDecoder PngDec = new PngBitmapDecoder(new Uri(filename), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            return PngDec.Frames[0];
        }

        private BitmapSource LoadJpg(string filename) {
            JpegBitmapDecoder JpgDec = new JpegBitmapDecoder(new Uri(filename), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            return JpgDec.Frames[0];
        }

        private BitmapSource LoadTiff(string filename) {
            TiffBitmapDecoder TifDec = new TiffBitmapDecoder(new Uri(filename), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            return TifDec.Frames[0];
        }
    }
}