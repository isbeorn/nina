using NINA.Model.MyCamera;
using NINA.Utility;
using NINA.Utility.Enum;
using NINA.Utility.Mediator;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using NINA.Utility.Profile;
using nom.tam.fits;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NINA.ViewModel {

    internal class ThumbnailVM : DockableVM {

        public ThumbnailVM(IProfileService profileService, IImagingMediator imagingMediator) : base(profileService) {
            Title = "LblImageHistory";
            ContentId = nameof(ThumbnailVM);
            CanClose = false;
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["HistorySVG"];

            this.imagingMediator = imagingMediator;

            this.imagingMediator.ImageSaved += ImagingMediator_ImageSaved;

            SelectCommand = new AsyncCommand<bool>((object o) => {
                return SelectImage((Thumbnail)o);
            });
        }

        private void ImagingMediator_ImageSaved(object sender, ImageSavedEventArgs e) {
            AddThumbnail(e);
        }

        private Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        private Task<bool> AddThumbnail(ImageSavedEventArgs msg) {
            return Task<bool>.Run(async () => {
                var factor = 100 / msg.Image.Width;

                BitmapSource scaledBitmap = new WriteableBitmap(new TransformedBitmap(msg.Image, new ScaleTransform(factor, factor)));
                scaledBitmap.Freeze();

                await _dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                    var thumbnail = new Thumbnail(profileService.ActiveProfile.ImageSettings.HistogramResolution) {
                        ThumbnailImage = scaledBitmap,
                        ImagePath = msg.PathToImage,
                        FileType = msg.FileType,
                        Duration = msg.Duration,
                        Mean = msg.Mean,
                        HFR = msg.HFR,
                        Filter = msg.Filter,
                        StatisticsId = msg.StatisticsId,
                        IsBayered = msg.IsBayered
                    };
                    Thumbnails.Add(thumbnail);
                    SelectedThumbnail = thumbnail;
                }));
                return true;
            });
        }

        private Thumbnail _selectedThumbnail;

        public Thumbnail SelectedThumbnail {
            get {
                return _selectedThumbnail;
            }
            set {
                _selectedThumbnail = value;
                RaisePropertyChanged();
            }
        }

        private ObservableLimitedSizedStack<Thumbnail> _thumbnails;
        private IImagingMediator imagingMediator;
        public ICommand SelectCommand { get; set; }

        private async Task<bool> SelectImage(Thumbnail thumbnail) {
            var iarr = await thumbnail.LoadOriginalImage();
            if (iarr != null) {
                await imagingMediator.PrepareImage(iarr, new System.Threading.CancellationToken(), false);
                return true;
            } else {
                return false;
            }
        }

        public ObservableLimitedSizedStack<Thumbnail> Thumbnails {
            get {
                if (_thumbnails == null) {
                    _thumbnails = new ObservableLimitedSizedStack<Thumbnail>(50);
                }
                return _thumbnails;
            }
            set {
                _thumbnails = value;
                RaisePropertyChanged();
            }
        }
    }

    public class Thumbnail : BaseINPC {

        public Thumbnail(int histogramResolution) {
            this.histogramResolution = histogramResolution;
        }

        public async Task<ImageArray> LoadOriginalImage() {
            ImageArray iarr = null;

            try {
                if (File.Exists(ImagePath.AbsolutePath)) {
                    if (FileType == FileTypeEnum.FITS) {
                        iarr = await LoadFits();
                    } else if (FileType == FileTypeEnum.XISF) {
                        iarr = await LoadXisf();
                    } else if (FileType == FileTypeEnum.TIFF) {
                        iarr = await LoadTiff();
                    }
                    iarr.Statistics.Id = StatisticsId;
                } else {
                    Notification.ShowError("File does not exist");
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
            }

            return iarr;
        }

        private async Task<ImageArray> LoadXisf() {
            var iarr = await XISF.LoadImageArrayFromFile(ImagePath, IsBayered, histogramResolution);
            return iarr;
        }

        private async Task<ImageArray> LoadTiff() {
            TiffBitmapDecoder TifDec = new TiffBitmapDecoder(ImagePath, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            BitmapFrame bmp = TifDec.Frames[0];
            int stride = bmp.PixelWidth * ((bmp.Format.BitsPerPixel + 7) / 8);
            int arraySize = stride * bmp.PixelHeight;
            ushort[] pixels = new ushort[(int)(bmp.Width * bmp.Height)];
            bmp.CopyPixels(pixels, stride, 0);
            var imgArr = await ImageArray.CreateInstance(pixels, (int)bmp.Width, (int)bmp.Height, IsBayered, true, histogramResolution);
            return imgArr;
        }

        private async Task<ImageArray> LoadFits() {
            Fits f = new Fits(ImagePath);
            ImageHDU hdu = (ImageHDU)f.ReadHDU();
            Array[] arr = (Array[])hdu.Data.DataArray;

            var width = hdu.Header.GetIntValue("NAXIS1");
            var height = hdu.Header.GetIntValue("NAXIS2");
            ushort[] pixels = new ushort[width * height];
            var i = 0;
            foreach (var row in arr) {
                foreach (short val in row) {
                    pixels[i++] = (ushort)(val + short.MaxValue);
                }
            }
            var imgArr = await ImageArray.CreateInstance(pixels, width, height, IsBayered, true, histogramResolution);
            return imgArr;
        }

        public BitmapSource ThumbnailImage { get; set; }

        public double Mean { get; set; }

        public double HFR { get; set; }

        public bool IsBayered { get; set; }

        public Uri ImagePath { get; set; }

        public FileTypeEnum FileType { get; set; }

        private int histogramResolution;

        public DateTime Date { get; set; } = DateTime.Now;

        public string Filter { get; set; }

        public double Duration { get; set; }

        public int StatisticsId { get; set; }
    }
}