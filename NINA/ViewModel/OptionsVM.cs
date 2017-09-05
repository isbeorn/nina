using NINA.Model;
using NINA.Utility;
using NINA.Utility.Astrometry;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace NINA.ViewModel {
    public  class OptionsVM : DockableVM {
        public OptionsVM() {
            Title = Locale.Loc.Instance["LblOptions"];
            ContentId = nameof(OptionsVM);
            CanClose = false;
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["SettingsSVG"];
            PreviewFileCommand = new RelayCommand(PreviewFile);
            OpenImageFileDiagCommand = new RelayCommand(OpenImageFileDiag);
            OpenCygwinFileDiagCommand = new RelayCommand(OpenCygwinFileDiag);
            OpenPS2FileDiagCommand = new RelayCommand(OpenPS2FileDiag);
            ToggleColorsCommand = new RelayCommand(ToggleColors);
            DownloadIndexesCommand = new RelayCommand(DownloadIndexes);

            

            HashSet<ImagePattern> p = new HashSet<ImagePattern>();
            p.Add(new ImagePattern("$$FILTER$$", "Filtername", "L"));
            p.Add(new ImagePattern("$$DATE$$", "Date with format YYYY-MM-DD", "2016-01-01"));
            p.Add(new ImagePattern("$$DATETIME$$", "Date with format YYYY-MM-DD_HH-mm-ss", "2016-01-01_12-00-00"));
            p.Add(new ImagePattern("$$FRAMENR$$", "# of the Frame with format ####", "0001"));
            p.Add(new ImagePattern("$$IMAGETYPE$$", "Light, Flat, Dark, Bias", "Light"));
            p.Add(new ImagePattern("$$BINNING$$", "Binning of the camera", "1x1"));
            p.Add(new ImagePattern("$$SENSORTEMP$$", "Temperature of the Camera", "-15"));
            p.Add(new ImagePattern("$$EXPOSURETIME$$", "Exposure Time in seconds", string.Format("{0:0.00}", 10.21234)));
            ImagePatterns = p;

            ScanForIndexFiles();
        }

        private void DownloadIndexes(object obj) {
            AstrometryIndexDownloader.AstrometryIndexDownloaderVM.Show(CygwinLocation);
            ScanForIndexFiles();
        }

        private void OpenImageFileDiag(object o) {
            var diag = new System.Windows.Forms.FolderBrowserDialog();
            diag.SelectedPath = ImageFilePath;
            System.Windows.Forms.DialogResult result = diag.ShowDialog();
            if(result == System.Windows.Forms.DialogResult.OK) {
                ImageFilePath = diag.SelectedPath + "\\";
            }
        }

        private void OpenCygwinFileDiag(object o) {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = Settings.CygwinLocation;
            
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                CygwinLocation = dialog.SelectedPath;                
            }            
        }

        private void OpenPS2FileDiag(object o) {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = Settings.PS2Location;

            if (dialog.ShowDialog() == true) {
                PS2Location = dialog.FileName;
            }
        }

        private void ScanForIndexFiles() {
            IndexFiles.Clear();
            try {
                DirectoryInfo di = new DirectoryInfo(CygwinLocation + @"\usr\share\astrometry\data");
                if (di.Exists) {
                    foreach (FileInfo f in di.GetFiles("*.fits")) {
                        IndexFiles.Add(f.Name);
                    }
                }
            } catch(Exception ex) {
                Logger.Error(ex.Message, ex.StackTrace);
            }
            
        }
        
        

        private ObservableCollection<string> _indexfiles;
        public ObservableCollection<string> IndexFiles {
            get {
                if(_indexfiles == null) {
                    _indexfiles = new ObservableCollection<string>();
                }
                return _indexfiles;
            }
            set {
                _indexfiles = value;
                RaisePropertyChanged();
            }
        }

        public ICommand DownloadIndexesCommand { get; private set; }

        public ICommand OpenCygwinFileDiagCommand { get; private set; }
        
        public ICommand OpenPS2FileDiagCommand { get; private set; }
        
        public ICommand OpenImageFileDiagCommand { get; private set; }

        public ICommand PreviewFileCommand { get; private set; }

        public ICommand ToggleColorsCommand { get; private set; }

        private void PreviewFile(object o) {
            MyMessageBox.MyMessageBox.Show(Utility.Utility.GetImageFileString(ImagePatterns), "Example File Name", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxResult.OK);
        }

        public string ImageFilePath {
            get {
                return Settings.ImageFilePath;
            }
            set {
                Settings.ImageFilePath = value;
                RaisePropertyChanged();
            }
        }

        
        public string ImageFilePattern {
            get {
                return Settings.ImageFilePattern;                
            }
            set {
                Settings.ImageFilePattern  = value;
                RaisePropertyChanged();
            }
        }
        
        public string PHD2ServerUrl {
            get {
                return Settings.PHD2ServerUrl;
            }
            set {
                Settings.PHD2ServerUrl = value;
                RaisePropertyChanged();
            }
        }

        public PlateSolverEnum PlateSolverType {
            get {
                return Settings.PlateSolverType;
            }
            set {
                Settings.PlateSolverType = value;
                RaisePropertyChanged();
            }
        }

        public Epoch EpochType {
            get {
                return Settings.EpochType;
            }
            set {
                Settings.EpochType = value;
                RaisePropertyChanged();
            }
        }


        public Hemisphere HemisphereType {
            get {
                return Settings.HemisphereType;
            }
            set {
                Settings.HemisphereType = value;
                RaisePropertyChanged();
            }
        }

        public int AnsvrFocalLength {
            get {
                return Settings.AnsvrFocalLength;
            }
            set {
                Settings.AnsvrFocalLength = value;
                RaisePropertyChanged();
            }
        }

        public int PS2FocalLength {
            get {
                return Settings.PS2FocalLength;
            }
            set {
                Settings.PS2FocalLength = value;
                RaisePropertyChanged();
            }
        }

        public string CygwinLocation {
            get {
                return Settings.CygwinLocation;
            }
            set {
                Settings.CygwinLocation = value;
                ScanForIndexFiles();
                RaisePropertyChanged();
            }
        }

        public string PS2Location {
            get {
                return Settings.PS2Location;
            }
            set {
                Settings.PS2Location = value;                
                RaisePropertyChanged();
            }
        }

        public double AnsvrPixelSize {
            get {
                return Settings.AnsvrPixelSize;
            }
            set {
                Settings.AnsvrPixelSize = value;
                RaisePropertyChanged();
            }
        }

        public double PS2PixelSize {
            get {
                return Settings.PS2PixelSize;
            }
            set {
                Settings.PS2PixelSize = value;
                RaisePropertyChanged();
            }
        }

        public double AnsvrSearchRadius {
            get {
                return Settings.AnsvrSearchRadius;
            }
            set {
                Settings.AnsvrSearchRadius = value;
                RaisePropertyChanged();
            }
        }

        public int PS2Regions {
            get {
                return Settings.PS2Regions;
            }
            set {
                Settings.PS2Regions = value;
                RaisePropertyChanged();
            }
        }

        public WeatherDataEnum WeatherDataType {
            get {
                return Settings.WeatherDataType;
            }
            set {
                Settings.WeatherDataType = value;
                RaisePropertyChanged();
            }
        }

        public string OpenWeatherMapAPIKey {
            get {
                return Settings.OpenWeatherMapAPIKey;
            }
            set {
                Settings.OpenWeatherMapAPIKey = value;
                RaisePropertyChanged();
            }
        }

        public string OpenWeatherMapUrl {
            get {
                return Settings.OpenWeatherMapUrl;
            }
            set {
                Settings.OpenWeatherMapUrl = value;
                RaisePropertyChanged();
            }
        }

        public string OpenWeatherMapLocation {
            get {
                return Settings.OpenWeatherMapLocation;
            }
            set {
                Settings.OpenWeatherMapLocation = value;
                RaisePropertyChanged();
            }
        }

        public string AstrometryAPIKey {
            get {
                return Settings.AstrometryAPIKey;
            }
            set {
                Settings.AstrometryAPIKey = value;
                RaisePropertyChanged();
            }
        }        

        public int PHD2ServerPort {
            get {
                return Settings.PHD2ServerPort;
            }
            set {
                Settings.PHD2ServerPort = value;
                RaisePropertyChanged();
            }
        }
    
        private void ToggleColors(object o) {
            var s = ColorSchemaName;
            var a = PrimaryColor;
            var b = SecondaryColor;
            var c = BorderColor;
            var d = BackgroundColor;
            var e = ButtonBackgroundColor;
            var f = ButtonBackgroundSelectedColor;
            var g = ButtonForegroundColor;
            var h = ButtonForegroundDisabledColor;

            ColorSchemaName = AlternativeColorSchemaName;
            PrimaryColor = AltPrimaryColor;
            SecondaryColor = AltSecondaryColor;
            BorderColor = AltBorderColor;
            BackgroundColor = AltBackgroundColor;
            ButtonBackgroundColor = AltButtonBackgroundColor;
            ButtonBackgroundSelectedColor = AltButtonBackgroundSelectedColor;
            ButtonForegroundColor = AltButtonForegroundColor;
            ButtonForegroundDisabledColor = AltButtonForegroundDisabledColor;

            AlternativeColorSchemaName = s;
            AltPrimaryColor = a;
            AltSecondaryColor = b;
            AltBorderColor = c;
            AltBackgroundColor = d;
            AltButtonBackgroundColor = e;
            AltButtonBackgroundSelectedColor = f;
            AltButtonForegroundColor = g;
            AltButtonForegroundDisabledColor = h;
        }

        public string ColorSchemaName { 
            get {
                return Settings.ColorSchemaName;
            }
            set {               
                Settings.ColorSchemaName = value;                
                RaiseAllPropertiesChanged();
            }
        }

        public ColorSchemas ColorSchemas {
            get {
                return Settings.ColorSchemas;
            }
        }

        public string AlternativeColorSchemaName {
            get {
                return Settings.AltColorSchemaName;
            }
            set {
                
                Settings.AltColorSchemaName = value;
                RaiseAllPropertiesChanged();
            }
        }
        

        public Color PrimaryColor {
            get {
                return Settings.PrimaryColor;
            }
            set {
                Settings.PrimaryColor = value;
                RaisePropertyChanged();
            }
            
        }
        public Color SecondaryColor {
            get {
                return Settings.SecondaryColor;
            }
            set {
                Settings.SecondaryColor = value;
                RaisePropertyChanged();
            }

        }
        public Color BorderColor {
            get {
                return Settings.BorderColor;
            }
            set {
                Settings.BorderColor = value;
                RaisePropertyChanged();
            }

        }
        public Color BackgroundColor {
            get {
                return Settings.BackgroundColor;
            }
            set {
                Settings.BackgroundColor = value;
                RaisePropertyChanged();
            }

        }

        public FileTypeEnum FileType {
            get {
                return Settings.FileType;
            } set {
                Settings.FileType = value;
                RaisePropertyChanged();
            }
        }

        public Color ButtonBackgroundColor {
            get {
                return Settings.ButtonBackgroundColor;
            }
            set {
                Settings.ButtonBackgroundColor = value;
                RaisePropertyChanged();
            }

        }
        public Color ButtonBackgroundSelectedColor {
            get {
                return Settings.ButtonBackgroundSelectedColor;
            }
            set {
                Settings.ButtonBackgroundSelectedColor = value;
                RaisePropertyChanged();
            }

        }

        public Color ButtonForegroundColor {
            get {
                return Settings.ButtonForegroundColor;
            }
            set {
                Settings.ButtonForegroundColor = value;
                RaisePropertyChanged();
            }

        }

        public Color ButtonForegroundDisabledColor {
            get {
                return Settings.ButtonForegroundDisabledColor;
            }
            set {
                Settings.ButtonForegroundDisabledColor = value;
                RaisePropertyChanged();
            }

        }

        public double DitherPixels {
            get {
                return Settings.DitherPixels;
            }
            set {
                Settings.DitherPixels = value;
                RaisePropertyChanged();
            }

        }

        public bool DitherRAOnly {
            get {
                return Settings.DitherRAOnly;
            }
            set {
                Settings.DitherRAOnly = value;
                RaisePropertyChanged();
            }

        }

        private HashSet<ImagePattern> _imagePatterns;
        public HashSet<ImagePattern> ImagePatterns {
            get {
                return _imagePatterns;
            }
            set {
                _imagePatterns = value;
                RaisePropertyChanged();
            }
        }

        public class ImagePattern {
            public ImagePattern(string k, string d, string v) {
                Key = k;
                Description = d;
                Value = v;
            }
            private string _key;
            private string _description;
            private string _value;
            public string Value {
                get {
                    return _value;
                }
                set {
                    _value = value;
                }
            }
            public string Key {
                get {
                    return _key;
                }

                set {
                    _key = value;
                }
            }

            public string Description {
                get {
                    return _description;
                }

                set {
                    _description = value;
                }
            }
        }




        public Color AltPrimaryColor {
            get {
                return Settings.AltPrimaryColor;
            }
            set {
                Settings.AltPrimaryColor = value;
                RaisePropertyChanged();
            }

        }
        public Color AltSecondaryColor {
            get {
                return Settings.AltSecondaryColor;
            }
            set {
                Settings.AltSecondaryColor = value;
                RaisePropertyChanged();
            }

        }
        public Color AltBorderColor {
            get {
                return Settings.AltBorderColor;
            }
            set {
                Settings.AltBorderColor = value;
                RaisePropertyChanged();
            }

        }
        public Color AltBackgroundColor {
            get {
                return Settings.AltBackgroundColor;
            }
            set {
                Settings.AltBackgroundColor = value;
                RaisePropertyChanged();
            }

        }
        public Color AltButtonBackgroundColor {
            get {
                return Settings.AltButtonBackgroundColor;
            }
            set {
                Settings.AltButtonBackgroundColor = value;
                RaisePropertyChanged();
            }

        }
        public Color AltButtonBackgroundSelectedColor {
            get {
                return Settings.AltButtonBackgroundSelectedColor;
            }
            set {
                Settings.AltButtonBackgroundSelectedColor = value;
                RaisePropertyChanged();
            }

        }

        public Color AltButtonForegroundColor {
            get {
                return Settings.AltButtonForegroundColor;
            }
            set {
                Settings.AltButtonForegroundColor = value;
                RaisePropertyChanged();
            }

        }

        public Color AltButtonForegroundDisabledColor {
            get {
                return Settings.AltButtonForegroundDisabledColor;
            }
            set {
                Settings.AltButtonForegroundDisabledColor = value;
                RaisePropertyChanged();
            }

        }

        public bool AutoMeridianFlip {
            get {
                return Settings.AutoMeridianFlip;
            }
            set {
                Settings.AutoMeridianFlip = value;
                RaisePropertyChanged();
            }
        }

        public double MinutesAfterMeridian {
            get {
                return Settings.MinutesAfterMeridian;
            }
            set {
                Settings.MinutesAfterMeridian = value;
                RaisePropertyChanged();
            }
        }

        public int MeridianFlipSettleTime {
            get {
                return Settings.MeridianFlipSettleTime;
            }
            set {
                Settings.MeridianFlipSettleTime = value;
                RaisePropertyChanged();
            }
        }
        

        public bool RecenterAfterFlip {
            get {
                return Settings.RecenterAfterFlip;
            }
            set {
                Settings.RecenterAfterFlip = value;
                RaisePropertyChanged();
            }
        }


    }
}
