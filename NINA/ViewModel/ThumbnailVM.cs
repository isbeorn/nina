using NINA.Utility;
using NINA.Utility.Mediator;
using NINA.Utility.Notification;
using nom.tam.fits;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NINA.ViewModel {
    class ThumbnailVM : DockableVM {
        public ThumbnailVM() : base() {
            Title = "LblThumbnail";
            ContentId = nameof(ThumbnailVM);
            CanClose = false;
            //ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ThumbnailSVG"];         

            Mediator.Instance.RegisterAsyncRequest(
                new AddThumbnailMessageHandle((AddThumbnailMessage msg) => {
                    return AddThumbnail(msg.Image, msg.PathToImage, msg.FileType);
                })
            );
        }

        private Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        private Task<bool> AddThumbnail(BitmapSource image, Uri pathToImage, FileTypeEnum fileType) {
            return Task<bool>.Run(async () => {
                var scaledBitmap = CreateResizedImage(image, 100, 100, 0);
                scaledBitmap.Freeze();
                await _dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                    Thumbnails.Add(new Thumbnail() { ThumbnailImage = scaledBitmap, ImagePath = pathToImage, FileType = fileType });
                }));                
                return true;
            });            
        }

        private BitmapSource _selectedThumbnail;
        public BitmapSource SelectedThumbnail {
            get {
                return _selectedThumbnail;
            }
            set {
                _selectedThumbnail = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<Thumbnail> _thumbnails;
        public ObservableCollection<Thumbnail> Thumbnails {
            get {
                if(_thumbnails == null) {
                    _thumbnails = new ObservableCollection<Thumbnail>();
                }
                return _thumbnails;
            }
            set {
                _thumbnails = value;
                RaisePropertyChanged();                
            }
        }

        private static BitmapFrame CreateResizedImage(ImageSource source, int width, int height, int margin) {
            var rect = new Rect(margin, margin, width - margin * 2, height - margin * 2);

            var group = new DrawingGroup();
            RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.HighQuality);
            group.Children.Add(new ImageDrawing(source, rect));

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
                drawingContext.DrawDrawing(group);

            var resizedImage = new RenderTargetBitmap(
                width, height,         // Resized dimensions
                96, 96,                // Default DPI values
                PixelFormats.Default); // Default pixel format
            resizedImage.Render(drawingVisual);

            return BitmapFrame.Create(resizedImage);
        }

    }

    public class Thumbnail : BaseINPC {
        public Thumbnail() {
            SelectCommand = new AsyncCommand<bool>(() => {
                return SelectImage();
            });
        }

        private async Task<bool> SelectImage() {
            var img = LoadOriginalImage();
            if(img != null) {
                return await Mediator.Instance.RequestAsync(new SetImageMessage() { Image = img });
            } else {
                return false;
            }            
        }

        private BitmapSource LoadOriginalImage() {
            BitmapSource source = null;

            try {
                if(File.Exists(ImagePath.AbsolutePath)) {
                    if(FileType == FileTypeEnum.FITS) {                        
                        Notification.ShowWarning("Fits Not yet supported");
                    } else if (FileType == FileTypeEnum.XISF) {
                        Notification.ShowWarning("Xisf Not yet supported");
                    } else if (FileType == FileTypeEnum.TIFF) {
                        source = LoadTiff();
                    }
                    
                    source.Freeze();
                } else {
                    Notification.ShowError("File does not exist");
                }                
            } catch(Exception ex) {
                Logger.Error(ex.Message, ex.StackTrace);
                Notification.ShowError(ex.Message);
            }
            
            return source;
        }

        private BitmapSource LoadTiff() {            
            TiffBitmapDecoder TifDec = new TiffBitmapDecoder(ImagePath, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            BitmapFrame bmp = TifDec.Frames[0];
                 
            return bmp;
        }

        private BitmapSource _thumbnailImage;
        public BitmapSource ThumbnailImage {
            get {
                return _thumbnailImage;
            }
            set {
                _thumbnailImage = value;
                RaisePropertyChanged();
            }
        }

        private Uri _imagePath;
        public Uri ImagePath {
            get {
                return _imagePath;
            }
            set {
                _imagePath = value;
                RaisePropertyChanged();
            }
        }

        public FileTypeEnum FileType { get; set; }

        public ICommand SelectCommand { get; set; }
    }
}
