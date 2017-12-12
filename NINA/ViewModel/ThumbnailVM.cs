using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
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
            Title = "LblImageHistory";
            ContentId = nameof(ThumbnailVM);
            CanClose = false;
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["HistorySVG"];         

            Mediator.Instance.RegisterAsyncRequest(
                new AddThumbnailMessageHandle((AddThumbnailMessage msg) => {
                    return AddThumbnail(msg);
                })
            );
        }

        private Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        private Task<bool> AddThumbnail(AddThumbnailMessage msg) {
            return Task<bool>.Run(async () => {
                var factor = 100 / msg.Image.Width;

                BitmapSource scaledBitmap = new TransformedBitmap(msg.Image, new ScaleTransform(factor, factor));
                scaledBitmap.Freeze();
                
                await _dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                    var thumbnail = new Thumbnail() {
                        ThumbnailImage = scaledBitmap,
                        ImagePath = msg.PathToImage,
                        FileType = msg.FileType,
                        Duration = msg.Duration,
                        Mean = msg.Mean,
                        HFR = msg.HFR,
                        Filter = msg.Filter,
                        StatisticsId = msg.StatisticsId,
                        IsBayered = msg.IsBayered };
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

    }

    public class Thumbnail : BaseINPC {
        public Thumbnail() {
            SelectCommand = new AsyncCommand<bool>(() => {
                return SelectImage();
            });
        }

        private async Task<bool> SelectImage() {
            var iarr = await LoadOriginalImage();
            if(iarr != null) {
                return await Mediator.Instance.RequestAsync(new SetImageMessage() { ImageArray = iarr, Mean = Mean });
            } else {
                return false;
            }            
        }

        private async Task<ImageArray> LoadOriginalImage() {
            ImageArray iarr = null;

            try {
                if(File.Exists(ImagePath.AbsolutePath)) {
                    if(FileType == FileTypeEnum.FITS) {
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
            } catch(Exception ex) {
                Logger.Error(ex.Message, ex.StackTrace);
                Notification.ShowError(ex.Message);
            }
            
            return iarr;
        }

        private async Task<ImageArray> LoadXisf() {
            var iarr = await XISF.LoadImageArrayFromFile(ImagePath, IsBayered);
            return iarr;
        }

        private async Task<ImageArray> LoadTiff() {            
            TiffBitmapDecoder TifDec = new TiffBitmapDecoder(ImagePath, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            BitmapFrame bmp = TifDec.Frames[0];
            int stride = bmp.PixelWidth * ((bmp.Format.BitsPerPixel + 7) / 8);
            int arraySize = stride * bmp.PixelHeight;
            ushort[] pixels = new ushort[(int)(bmp.Width * bmp.Height)];
            bmp.CopyPixels(pixels, stride, 0);
            var imgArr = await ImageArray.CreateInstance(pixels, (int)bmp.Width, (int)bmp.Height, IsBayered);
            return imgArr;
        }

        private async Task<ImageArray> LoadFits() {
            Fits f = new Fits(ImagePath);
            ImageHDU hdu = (ImageHDU) f.ReadHDU();
            Array[] arr = (Array[])hdu.Data.DataArray;

            var width = hdu.Header.GetIntValue("NAXIS1");
            var height = hdu.Header.GetIntValue("NAXIS2");
            ushort[] pixels = new ushort[width * height];
            var i = 0;
            foreach (var row in arr) {
                foreach(short val in row) {
                    pixels[i++] = (ushort)(val + short.MaxValue);
                }
            }
            var imgArr = await ImageArray.CreateInstance(pixels, width, height, IsBayered);
            return imgArr;
        }

        
        public BitmapSource ThumbnailImage { get; set; }

        public double Mean { get; set; }

        public double HFR { get; set; }

        public bool IsBayered { get; set; }

        public Uri ImagePath { get; set; }

        public FileTypeEnum FileType { get; set; }

        public ICommand SelectCommand { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        public string Filter { get; set; }

        public double Duration { get; set; }

        public int StatisticsId { get; set; }
    }
}
