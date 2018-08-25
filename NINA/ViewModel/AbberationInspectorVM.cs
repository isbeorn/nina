using NINA.Utility.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINA.ViewModel {

    internal class AbberationInspectorVM : BaseVM {

        public AbberationInspectorVM(IProfileService profileService, BitmapSource source) : base(profileService) {
            Columns = 3;
            CellSize = 256;
            DetermineImage(source);
        }

        private void DetermineImage(BitmapSource source) {
            var panelSize = cellSize * columns;

            if (source.Width < panelSize || source.Height < panelSize) {
                throw new Exception(string.Format("Image too small for aberration inspector. Must be at least {0}x{0}", panelSize));
            }

            var widthOffset = (source.Width - columns * cellSize) / (columns - 1);
            var heightOffset = (source.Height - columns * cellSize) / (columns - 1);

            DrawingVisual drawingVisual = new DrawingVisual();
            var pen = new Pen() { Brush = Brushes.Black, Thickness = 1 };
            using (DrawingContext drawingContext = drawingVisual.RenderOpen()) {
                for (int column = 0; column < columns; column++) {
                    for (int row = 0; row < columns; row++) {
                        var rect = new Int32Rect(
                            (int)(column * widthOffset + column * cellSize),
                            (int)(row * heightOffset + row * cellSize),
                            cellSize,
                            cellSize
                        );

                        var crop = new CroppedBitmap(source, rect);

                        drawingContext.DrawImage(
                            crop,
                            new System.Windows.Rect(
                                column * cellSize,
                                row * cellSize,
                                cellSize,
                                cellSize
                            )
                        );
                    }
                }
            }

            RenderTargetBitmap bmp = new RenderTargetBitmap(panelSize, panelSize, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(drawingVisual);
            bmp.Freeze();
            this.MosaicImage = bmp;
        }

        private int columns;

        public int Columns {
            get {
                return columns;
            }
            set {
                columns = value;
                RaisePropertyChanged();
            }
        }

        private int cellSize;

        public int CellSize {
            get {
                return cellSize;
            }
            set {
                cellSize = value;
                RaisePropertyChanged();
            }
        }

        private BitmapSource mosaicImage;

        public BitmapSource MosaicImage {
            get {
                return mosaicImage;
            }
            set {
                mosaicImage = value;
                RaisePropertyChanged();
            }
        }
    }
}