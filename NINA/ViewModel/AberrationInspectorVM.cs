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

    internal class AberrationInspectorVM : BaseVM {

        public AberrationInspectorVM(IProfileService profileService, BitmapSource source) : base(profileService) {
            Columns = 3;
            SeparationSize = 4;
            CellSize = 256;
            DetermineImage(source);
        }

        private void DetermineImage(BitmapSource source) {
            var panelSize = CellSize * Columns + SeparationSize * Columns;

            if (source.Width < panelSize || source.Height < panelSize) {
                throw new Exception(string.Format("Image too small for aberration inspector. Must be at least {0}x{0}", panelSize));
            }

            var widthOffset = (source.Width - Columns * CellSize) / (Columns - 1);
            var heightOffset = (source.Height - Columns * CellSize) / (Columns - 1);

            DrawingVisual drawingVisual = new DrawingVisual();
            var pen = new Pen() { Brush = Brushes.Black, Thickness = SeparationSize };
            using (DrawingContext drawingContext = drawingVisual.RenderOpen()) {
                for (int column = 0; column < Columns; column++) {
                    for (int row = 0; row < Columns; row++) {
                        var rect = new Int32Rect(
                            (int)(column * widthOffset + column * CellSize),
                            (int)(row * heightOffset + row * CellSize),
                            CellSize,
                            CellSize
                        );

                        var crop = new WriteableBitmap(new CroppedBitmap(source, rect));

                        var panelRectangle = new System.Windows.Rect(
                            column * CellSize + column * SeparationSize,
                            row * CellSize + row * SeparationSize,
                            CellSize,
                            CellSize
                        );

                        drawingContext.DrawImage(
                            crop,
                            panelRectangle
                        );

                        if (column < Columns - 1) {
                            //Vertical Line
                            drawingContext.DrawLine(
                                pen,
                                new Point(
                                    panelRectangle.X + CellSize + SeparationSize / 2d,
                                    panelRectangle.Y
                                ),
                                new Point(
                                    panelRectangle.X + CellSize + SeparationSize / 2d,
                                    panelRectangle.Y + CellSize
                                )
                            );
                        }

                        if (row < Columns - 1) {
                            //Horizontal Line
                            drawingContext.DrawLine(
                                pen,
                                new Point(
                                    panelRectangle.X,
                                    panelRectangle.Y + CellSize + SeparationSize / 2d
                                ),
                                new Point(
                                    panelRectangle.X + CellSize,
                                    panelRectangle.Y + CellSize + SeparationSize / 2d
                                )
                            );
                        }
                    }
                }
            }

            RenderTargetBitmap bmp = new RenderTargetBitmap(panelSize, panelSize, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(drawingVisual);
            bmp.Freeze();
            this.MosaicImage = bmp;
        }

        private int separationSize;

        public int SeparationSize {
            get {
                return separationSize;
            }
            set {
                separationSize = value;
                RaisePropertyChanged();
            }
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