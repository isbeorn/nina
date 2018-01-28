using NINA.Model;
using NINA.Model.MyFilterWheel;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace NINA.ViewModel {
    public class OptionsVM : DockableVM {
        public OptionsVM() {
            Title = "LblOptions";
            ContentId = nameof(OptionsVM);
            CanClose = false;
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["SettingsSVG"];
            PreviewFileCommand = new RelayCommand(PreviewFile);
            OpenImageFileDiagCommand = new RelayCommand(OpenImageFileDiag);
            OpenCygwinFileDiagCommand = new RelayCommand(OpenCygwinFileDiag);
            OpenPS2FileDiagCommand = new RelayCommand(OpenPS2FileDiag);
            ToggleColorsCommand = new RelayCommand(ToggleColors);
            DownloadIndexesCommand = new RelayCommand(DownloadIndexes);
            OpenSkyAtlasImageRepositoryDiagCommand = new RelayCommand(OpenSkyAtlasImageRepositoryDiag);
            ImportFiltersCommand = new RelayCommand(ImportFilters);
            AddFilterCommand = new RelayCommand(AddFilter);
            RemoveFilterCommand = new RelayCommand(RemoveFilter);


            ImagePatterns = CreateImagePatternList();

            ScanForIndexFiles();

            Mediator.Instance.Register((object o) => {
                ImagePatterns = CreateImagePatternList();
            }, MediatorMessages.LocaleChanged);

            FilterWheelFilters.CollectionChanged += FilterWheelFilters_CollectionChanged;
        }

        private void RemoveFilter(object obj) {
            if(SelectedFilter == null && FilterWheelFilters.Count > 0) {
                SelectedFilter = FilterWheelFilters.Last();
            }
            FilterWheelFilters.Remove(SelectedFilter);
            if(FilterWheelFilters.Count > 0) { 
                SelectedFilter = FilterWheelFilters.Last();
            }
        }

        private void AddFilter(object obj) {
            var pos = FilterWheelFilters.Count;
            var filter = new FilterInfo(Locale.Loc.Instance["LblFilter"] + (pos + 1), 0, (short)pos, 0);
            FilterWheelFilters.Add(filter);
            SelectedFilter = filter;
        }

        private void ImportFilters(object obj) {
            var filters = Mediator.Instance.Request(new GetAllFiltersMessage());
            if(filters != null) {
                FilterWheelFilters.Clear();
                FilterWheelFilters.CollectionChanged -= FilterWheelFilters_CollectionChanged;
                var l = new List<FilterInfo>();
                foreach(FilterInfo filter in filters) {
                    l.Add(filter);                    
                }
                FilterWheelFilters = new ObserveAllCollection<FilterInfo>(l.OrderBy((x) => x.Position));
                FilterWheelFilters.CollectionChanged += FilterWheelFilters_CollectionChanged;
            }
        }

        private void FilterWheelFilters_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            FilterWheelFilters = FilterWheelFilters;
        }

        private HashSet<ImagePattern> CreateImagePatternList() {
            HashSet<ImagePattern> p = new HashSet<ImagePattern>();
            p.Add(new ImagePattern("$$FILTER$$", Locale.Loc.Instance["LblFilternameDescription"], "L"));
            p.Add(new ImagePattern("$$DATE$$", Locale.Loc.Instance["LblDateFormatDescription"], "2016-01-01"));
            p.Add(new ImagePattern("$$DATETIME$$", Locale.Loc.Instance["LblDateTimeFormatDescription"], "2016-01-01_12-00-00"));
            p.Add(new ImagePattern("$$FRAMENR$$", Locale.Loc.Instance["LblFrameNrDescription"], "0001"));
            p.Add(new ImagePattern("$$IMAGETYPE$$", Locale.Loc.Instance["LblImageTypeDescription"], "Light"));
            p.Add(new ImagePattern("$$BINNING$$", Locale.Loc.Instance["LblBinningDescription"], "1x1"));
            p.Add(new ImagePattern("$$SENSORTEMP$$", Locale.Loc.Instance["LblTemperatureDescription"], "-15"));
            p.Add(new ImagePattern("$$EXPOSURETIME$$", Locale.Loc.Instance["LblExposureTimeDescription"], string.Format("{0:0.00}", 10.21234)));
            p.Add(new ImagePattern("$$TARGETNAME$$", Locale.Loc.Instance["LblTargetNameDescription"], "M33"));
            p.Add(new ImagePattern("$$GAIN$$", Locale.Loc.Instance["LblGainDescription"], "1600"));
            return p;
        }

        private void OpenSkyAtlasImageRepositoryDiag(object obj) {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = Settings.SkyAtlasImageRepository;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                SkyAtlasImageRepository = dialog.SelectedPath;
            }
        }

        private void DownloadIndexes(object obj) {
            AstrometryIndexDownloader.AstrometryIndexDownloaderVM.Show(CygwinLocation);
            ScanForIndexFiles();
        }

        private void OpenImageFileDiag(object o) {
            var diag = new System.Windows.Forms.FolderBrowserDialog();
            diag.SelectedPath = ImageFilePath;
            System.Windows.Forms.DialogResult result = diag.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) {
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
            } catch (Exception ex) {
                Logger.Error(ex.Message, ex.StackTrace);
            }

        }



        private ObservableCollection<string> _indexfiles;
        public ObservableCollection<string> IndexFiles {
            get {
                if (_indexfiles == null) {
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

        public ICommand OpenSkyAtlasImageRepositoryDiagCommand { get; private set; }

        public ICommand ImportFiltersCommand { get; private set; }

        public ICommand AddFilterCommand { get; private set; }

        public ICommand RemoveFilterCommand { get; private set; }

        private void PreviewFile(object o) {
            MyMessageBox.MyMessageBox.Show(Utility.Utility.GetImageFileString(ImagePatterns), Locale.Loc.Instance["LblFileExample"], System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxResult.OK);
        }

        private ObservableCollection<CultureInfo> _availableLanguages = new ObservableCollection<CultureInfo>() {
            new CultureInfo("en-GB"),
            new CultureInfo("en-US"),
            new CultureInfo("de-DE")
        };
        public ObservableCollection<CultureInfo> AvailableLanguages {
            get {
                return _availableLanguages;
            }
            set {
                _availableLanguages = value;
                RaisePropertyChanged();
            }
        }

        public CultureInfo Language {
            get {
                return Settings.Language;
            }
            set {
                Settings.Language = value;
                RaisePropertyChanged();
            }
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
                Settings.ImageFilePattern = value;
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

        public double AutoStretchFactor {
            get {
                return Settings.AutoStretchFactor;
            }
            set {
                Settings.AutoStretchFactor = value;
                RaisePropertyChanged();
            }
        }

        public bool AnnotateImage {
            get {
                return Settings.AnnotateImage;
            }
            set {
                Settings.AnnotateImage = value;
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
                Latitude = Latitude;
                Mediator.Instance.Notify(MediatorMessages.LocationChanged, null);
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
            }
            set {
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

        public double PauseTimeBeforeMeridian {
            get {
                return Settings.PauseTimeBeforeMeridian;
            }
            set {
                Settings.PauseTimeBeforeMeridian = value;
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

        public double Latitude {
            get {
                return Settings.Latitude;
            }
            set {
                if ((HemisphereType == Hemisphere.SOUTHERN && value > 0) || (HemisphereType == Hemisphere.NORTHERN && value < 0)) {
                    value = -value;
                }
                Settings.Latitude = value;
                RaisePropertyChanged();
                Mediator.Instance.Notify(MediatorMessages.LocationChanged, null);
            }
        }

        public double Longitude {
            get {
                return Settings.Longitude;
            }
            set {
                Settings.Longitude = value;
                RaisePropertyChanged();
                Mediator.Instance.Notify(MediatorMessages.LocationChanged, null);
            }
        }

        public string SkyAtlasImageRepository {
            get {
                return Settings.SkyAtlasImageRepository;
            }
            set {
                Settings.SkyAtlasImageRepository = value;
                RaisePropertyChanged();
            }
        }

        public Color NotificationWarningColor {
            get {
                return Settings.NotificationWarningColor;
            }
            set {
                Settings.NotificationWarningColor = value;
                RaisePropertyChanged();
            }

        }
        public Color NotificationErrorColor {
            get {
                return Settings.NotificationErrorColor;
            }
            set {
                Settings.NotificationErrorColor = value;
                RaisePropertyChanged();
            }

        }
        public Color AltNotificationWarningColor {
            get {
                return Settings.AltNotificationWarningColor;
            }
            set {
                Settings.AltNotificationWarningColor = value;
                RaisePropertyChanged();
            }

        }
        public Color AltNotificationErrorColor {
            get {
                return Settings.AltNotificationErrorColor;
            }
            set {
                Settings.AltNotificationErrorColor = value;
                RaisePropertyChanged();
            }

        }

        public bool FocuserUseFilterWheelOffsets {
            get {
                return Settings.FocuserUseFilterWheelOffsets;
            }
            set {
                Settings.FocuserUseFilterWheelOffsets = value;
                RaisePropertyChanged();
            }

        }

        public int FocuserAutoFocusInitialOffsetSteps {
            get {
                return Settings.FocuserAutoFocusInitialOffsetSteps;
            }
            set {
                Settings.FocuserAutoFocusInitialOffsetSteps = value;
                RaisePropertyChanged();
            }

        }

        public int FocuserAutoFocusStepSize {
            get {
                return Settings.FocuserAutoFocusStepSize;
            }
            set {
                Settings.FocuserAutoFocusStepSize = value;
                RaisePropertyChanged();
            }
        }

        public int FocuserAutoFocusExposureTime {
            get {
                return Settings.FocuserAutoFocusExposureTime;
            }
            set {
                Settings.FocuserAutoFocusExposureTime = value;
                RaisePropertyChanged();
            }
        }

        public string TelescopeSnapPortStart {
            get {
                return Settings.TelescopeSnapPortStart;
            }
            set {
                Settings.TelescopeSnapPortStart = value;
                RaisePropertyChanged();
            }
        }

        public string TelescopeSnapPortStop {
            get {
                return Settings.TelescopeSnapPortStop;
            }
            set {
                Settings.TelescopeSnapPortStop = value;
                RaisePropertyChanged();
            }
        }

        public double DevicePollingInterval {
            get {
                return Settings.DevicePollingInterval;
            }
            set {
                if (value > 0) {
                    Settings.DevicePollingInterval = value;
                    RaisePropertyChanged();
                }
            }
        }

        public LogLevelEnum LogLevel {
            get {
                return (LogLevelEnum)Settings.LogLevel;
            }
            set {
                Settings.LogLevel = (int)value;
                RaisePropertyChanged();
            }
        }

        public CameraBulbModeEnum CameraBulbMode {
            get {
                return Settings.CameraBulbMode;
            }
            set {
                Settings.CameraBulbMode = value;
                RaisePropertyChanged();
            }
        }

        public string CameraSerialPort {
            get {
                return Settings.CameraSerialPort;
            }
            set {
                Settings.CameraSerialPort = value;
                RaisePropertyChanged();
            }
        }

        public double CameraPixelSize {
            get {
                return Settings.CameraPixelSize;
            }
            set {
                Settings.CameraPixelSize = value;
                RaisePropertyChanged();
            }
        }

        public int TelescopeFocalLength {
            get {
                return Settings.TelescopeFocalLength;
            }
            set {
                Settings.TelescopeFocalLength = value;
                RaisePropertyChanged();
            }
        }

        public ObserveAllCollection<FilterInfo> FilterWheelFilters{
            get {
                return Settings.FilterWheelFilters;
            }
            set {
                Settings.FilterWheelFilters = value;
                RaisePropertyChanged();
            }
        }

        private FilterInfo _selectedFilter;
        public FilterInfo SelectedFilter {
            get {
                return _selectedFilter;
            }
            set {
                _selectedFilter = value;
                RaisePropertyChanged();
            }
        }
    }
}
