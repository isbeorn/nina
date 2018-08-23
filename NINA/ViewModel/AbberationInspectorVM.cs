using NINA.Utility.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINA.ViewModel {

    internal class AbberationInspectorVM : BaseVM {

        public AbberationInspectorVM(IProfileService profileService, BitmapSource source) : base(profileService) {
            DetermineImage(source);
        }

        private void DetermineImage(BitmapSource source) {
            var centerWidth = (source.Width / 2d);
            var centerHeight = (source.Height / 2d);
            var centerWidthOffset = (int)(centerWidth - (85 / 2d));
            var centerHeightOffset = (int)(centerHeight - (85 / 2d));

            var topLeft = new CroppedBitmap(source, new System.Windows.Int32Rect(0, 0, 85, 85));

            var top = new CroppedBitmap(source, new System.Windows.Int32Rect(centerWidthOffset, 0, 85, 85));

            var topRight = new CroppedBitmap(source, new System.Windows.Int32Rect(source.PixelWidth - 85, 0, 85, 85));

            var left = new CroppedBitmap(source, new System.Windows.Int32Rect(0, centerHeightOffset, 85, 85));

            var center = new CroppedBitmap(source, new System.Windows.Int32Rect(centerWidthOffset, centerHeightOffset, 85, 85));

            var right = new CroppedBitmap(source, new System.Windows.Int32Rect(source.PixelWidth - 85, centerHeightOffset, 85, 85));

            var bottomLeft = new CroppedBitmap(source, new System.Windows.Int32Rect(0, (int)source.Height - 85, 85, 85));

            var bottom = new CroppedBitmap(source, new System.Windows.Int32Rect(centerWidthOffset, (int)source.Height - 85, 85, 85));

            var bottomRight = new CroppedBitmap(source, new System.Windows.Int32Rect(source.PixelWidth - 85, (int)source.Height - 85, 85, 85));

            DrawingVisual drawingVisual = new DrawingVisual();
            var pen = new Pen() { Brush = Brushes.Black, Thickness = 1 };
            using (DrawingContext drawingContext = drawingVisual.RenderOpen()) {
                drawingContext.DrawImage(topLeft, new System.Windows.Rect(0, 0, 85, 85));
                drawingContext.DrawImage(top, new System.Windows.Rect(85, 0, 85, 85));
                drawingContext.DrawImage(topRight, new System.Windows.Rect(2 * 85, 0, 85, 85));

                drawingContext.DrawImage(left, new System.Windows.Rect(0, 85, 85, 85));
                drawingContext.DrawImage(center, new System.Windows.Rect(85, 85, 85, 85));
                drawingContext.DrawImage(right, new System.Windows.Rect(2 * 85, 85, 85, 85));

                drawingContext.DrawImage(bottomLeft, new System.Windows.Rect(0, 2 * 85, 85, 85));
                drawingContext.DrawImage(bottom, new System.Windows.Rect(85, 2 * 85, 85, 85));
                drawingContext.DrawImage(bottomRight, new System.Windows.Rect(2 * 85, 2 * 85, 85, 85));

                //Horizontal lines
                drawingContext.DrawLine(pen, new System.Windows.Point(0, 85), new System.Windows.Point(3 * 85, 85));
                drawingContext.DrawLine(pen, new System.Windows.Point(0, 2 * 85), new System.Windows.Point(3 * 85, 2 * 85));

                //Vertical lines
                drawingContext.DrawLine(pen, new System.Windows.Point(85, 0), new System.Windows.Point(85, 3 * 85));
                drawingContext.DrawLine(pen, new System.Windows.Point(2 * 85, 0), new System.Windows.Point(2 * 85, 3 * 85));
            }
            RenderTargetBitmap bmp = new RenderTargetBitmap(3 * 85, 3 * 85, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(drawingVisual);
            bmp.Freeze();
            this.Image = bmp;
        }

        private BitmapSource image;

        public BitmapSource Image {
            get {
                return image;
            }
            set {
                image = value;
                RaisePropertyChanged();
            }
        }
    }
}