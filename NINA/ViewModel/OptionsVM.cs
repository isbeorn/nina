using NINA.Model;
using NINA.Model.MyFilterWheel;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Mediator;
using NINA.Utility.Profile;
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
            OpenSequenceTemplateDiagCommand = new RelayCommand(OpenSequenceTemplateDiag);
            OpenCygwinFileDiagCommand = new RelayCommand(OpenCygwinFileDiag);
            OpenPS2FileDiagCommand = new RelayCommand(OpenPS2FileDiag);
            ToggleColorsCommand = new RelayCommand(ToggleColors);
            DownloadIndexesCommand = new RelayCommand(DownloadIndexes);
            OpenSkyAtlasImageRepositoryDiagCommand = new RelayCommand(OpenSkyAtlasImageRepositoryDiag);
            ImportFiltersCommand = new RelayCommand(ImportFilters);
            AddFilterCommand = new RelayCommand(AddFilter);
            RemoveFilterCommand = new RelayCommand(RemoveFilter);
            AddProfileCommand = new RelayCommand(AddProfile);
            RemoveProfileCommand = new RelayCommand(RemoveProfile, (object o) => { return SelectedProfile != null && SelectedProfile.Id != ProfileManager.Instance.ActiveProfile.Id; });
            SelectProfileCommand = new RelayCommand(SelectProfile, (o) => {
                return SelectedProfile != null;
            });


            ImagePatterns = CreateImagePatternList();

            ScanForIndexFiles();

            Mediator.Instance.Register((object o) => {
                ImagePatterns = CreateImagePatternList();
            }, MediatorMessages.LocaleChanged);

            Mediator.Instance.Register((object o) => {
                CameraPixelSize = (double)o;
            }, MediatorMessages.CameraPixelSizeChanged);

            Mediator.Instance.RegisterRequest(new SetProfileByIdMessageHandle((SetProfileByIdMessage msg) =>
            {
                SelectedProfile = ProfileManager.Instance.Profiles.ProfileList.Single(p => p.Id == msg.Id);
                SelectProfile(null);
                return true;
            }));

            FilterWheelFilters.CollectionChanged += FilterWheelFilters_CollectionChanged;
        }

        private void RemoveProfile(object obj) {
            if(MyMessageBox.MyMessageBox.Show(string.Format(Locale.Loc.Instance["LblRemoveProfileText"], SelectedProfile?.Name, SelectedProfile?.Id), Locale.Loc.Instance["LblRemoveProfileCaption"], System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes) {
                ProfileManager.Instance.RemoveProfile(SelectedProfile.Id);
            }
        }

        private void SelectProfile(object obj) {
            ProfileManager.Instance.SelectProfile(SelectedProfile.Id);
            RaisePropertyChanged(nameof(ColorSchemaName));
            RaisePropertyChanged(nameof(PrimaryColor));
            RaisePropertyChanged(nameof(SecondaryColor));
            RaisePropertyChanged(nameof(BorderColor));
            RaisePropertyChanged(nameof(BackgroundColor));
            RaisePropertyChanged(nameof(ButtonBackgroundColor));
            RaisePropertyChanged(nameof(ButtonBackgroundSelectedColor));
            RaisePropertyChanged(nameof(ButtonForegroundColor));
            RaisePropertyChanged(nameof(ButtonForegroundDisabledColor));
            RaisePropertyChanged(nameof(NotificationWarningColor));
            RaisePropertyChanged(nameof(NotificationErrorColor));
            RaisePropertyChanged(nameof(AlternativeColorSchemaName));
            RaisePropertyChanged(nameof(AltPrimaryColor));
            RaisePropertyChanged(nameof(AltSecondaryColor));
            RaisePropertyChanged(nameof(AltBorderColor));
            RaisePropertyChanged(nameof(AltBackgroundColor));
            RaisePropertyChanged(nameof(AltButtonBackgroundColor));
            RaisePropertyChanged(nameof(AltButtonBackgroundSelectedColor));
            RaisePropertyChanged(nameof(AltButtonForegroundColor));
            RaisePropertyChanged(nameof(AltButtonForegroundDisabledColor));
            RaisePropertyChanged(nameof(AltNotificationWarningColor));
            RaisePropertyChanged(nameof(AltNotificationErrorColor));
            //RaisePropertyChanged(nameof(ColorSchemas));
            //RaiseAllPropertiesChanged();
            foreach (System.Reflection.PropertyInfo p in this.GetType().GetProperties()) {
                if (!p.Name.ToLower().Contains("color")) {
                    RaisePropertyChanged(p.Name);
                }
            }

        }

        private void AddProfile(object obj) {
            ProfileManager.Instance.Add();
        }

        private void RemoveFilter(object obj) {
            if (SelectedFilter == null && FilterWheelFilters.Count > 0) {
                SelectedFilter = FilterWheelFilters.Last();
            }
            FilterWheelFilters.Remove(SelectedFilter);
            if (FilterWheelFilters.Count > 0) {
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
            if (filters != null) {
                FilterWheelFilters.Clear();
                FilterWheelFilters.CollectionChanged -= FilterWheelFilters_CollectionChanged;
                var l = new List<FilterInfo>();
                foreach (FilterInfo filter in filters) {
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
            p.Add(new ImagePattern("$$TIME$$", Locale.Loc.Instance["LblTimeFormatDescription"], "12-00-00"));
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
            dialog.SelectedPath = ProfileManager.Instance.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository;

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

        private void OpenSequenceTemplateDiag(object o) {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = Locale.Loc.Instance["LblSequenceTemplate"];
            dialog.FileName = "Sequence";
            dialog.DefaultExt = ".xml";
            dialog.Filter = "XML documents|*.xml";

            if (dialog.ShowDialog() == true) {
                SequenceTemplatePath = dialog.FileName;
            }
        }

        private void OpenCygwinFileDiag(object o) {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = ProfileManager.Instance.ActiveProfile.PlateSolveSettings.CygwinLocation;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                CygwinLocation = dialog.SelectedPath;
            }
        }

        private void OpenPS2FileDiag(object o) {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = ProfileManager.Instance.ActiveProfile.PlateSolveSettings.PS2Location;

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
                Logger.Error(ex);
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

        public ICommand OpenSequenceTemplateDiagCommand { get; private set; }

        public ICommand PreviewFileCommand { get; private set; }

        public ICommand ToggleColorsCommand { get; private set; }

        public ICommand OpenSkyAtlasImageRepositoryDiagCommand { get; private set; }

        public ICommand ImportFiltersCommand { get; private set; }

        public ICommand AddFilterCommand { get; private set; }

        public ICommand RemoveFilterCommand { get; private set; }

        public ICommand AddProfileCommand { get; private set; }

        public ICommand RemoveProfileCommand { get; private set; }        

        public ICommand SelectProfileCommand { get; private set; }

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
                return ProfileManager.Instance.ActiveProfile.ApplicationSettings.Language;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ApplicationSettings.Language = value;

                System.Threading.Thread.CurrentThread.CurrentUICulture = Language;
                System.Threading.Thread.CurrentThread.CurrentCulture = Language;
                Locale.Loc.Instance.ReloadLocale(Language.Name);
                RaisePropertyChanged();
            }
        }

        public string ImageFilePath {
            get {
                return ProfileManager.Instance.ActiveProfile.ImageFileSettings.FilePath;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ImageFileSettings.FilePath = value;
                RaisePropertyChanged();
            }
        }

        public string SequenceTemplatePath {
            get {
                return ProfileManager.Instance.ActiveProfile.SequenceSettings.TemplatePath;
            }
            set {
                ProfileManager.Instance.ActiveProfile.SequenceSettings.TemplatePath = value;
                RaisePropertyChanged();
            }
        }

        public string ImageFilePattern {
            get {
                return ProfileManager.Instance.ActiveProfile.ImageFileSettings.FilePattern;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ImageFileSettings.FilePattern = value;
                RaisePropertyChanged();
            }
        }

        public string PHD2ServerUrl {
            get {
                return ProfileManager.Instance.ActiveProfile.GuiderSettings.PHD2ServerUrl;
            }
            set {
                ProfileManager.Instance.ActiveProfile.GuiderSettings.PHD2ServerUrl = value;
                RaisePropertyChanged();
            }
        }

        public double AutoStretchFactor {
            get {
                return ProfileManager.Instance.ActiveProfile.ImageSettings.AutoStretchFactor;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ImageSettings.AutoStretchFactor = value;
                RaisePropertyChanged();
            }
        }

        public bool AnnotateImage {
            get {
                return ProfileManager.Instance.ActiveProfile.ImageSettings.AnnotateImage;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ImageSettings.AnnotateImage = value;
                RaisePropertyChanged();
            }
        }

        public PlateSolverEnum PlateSolverType {
            get {
                return ProfileManager.Instance.ActiveProfile.PlateSolveSettings.PlateSolverType;
            }
            set {
                ProfileManager.Instance.ActiveProfile.PlateSolveSettings.PlateSolverType = value;
                RaisePropertyChanged();
            }
        }

        public BlindSolverEnum BlindSolverType {
            get {
                return ProfileManager.Instance.ActiveProfile.PlateSolveSettings.BlindSolverType;
            }
            set {
                ProfileManager.Instance.ActiveProfile.PlateSolveSettings.BlindSolverType = value;
                RaisePropertyChanged();
            }
        }

        public Epoch EpochType {
            get {
                return ProfileManager.Instance.ActiveProfile.AstrometrySettings.EpochType;
            }
            set {
                ProfileManager.Instance.ActiveProfile.AstrometrySettings.EpochType = value;
                RaisePropertyChanged();
            }
        }


        public Hemisphere HemisphereType {
            get {
                return ProfileManager.Instance.ActiveProfile.AstrometrySettings.HemisphereType;
            }
            set {
                ProfileManager.Instance.ActiveProfile.AstrometrySettings.HemisphereType = value;
                RaisePropertyChanged();
                Latitude = Latitude;
                Mediator.Instance.Notify(MediatorMessages.LocationChanged, null);
            }
        }

        public string CygwinLocation {
            get {
                return ProfileManager.Instance.ActiveProfile.PlateSolveSettings.CygwinLocation;
            }
            set {
                ProfileManager.Instance.ActiveProfile.PlateSolveSettings.CygwinLocation = value;
                ScanForIndexFiles();
                RaisePropertyChanged();
            }
        }

        public string PS2Location {
            get {
                return ProfileManager.Instance.ActiveProfile.PlateSolveSettings.PS2Location;
            }
            set {
                ProfileManager.Instance.ActiveProfile.PlateSolveSettings.PS2Location = value;
                RaisePropertyChanged();
            }
        }

        public double AnsvrSearchRadius {
            get {
                return ProfileManager.Instance.ActiveProfile.PlateSolveSettings.SearchRadius;
            }
            set {
                ProfileManager.Instance.ActiveProfile.PlateSolveSettings.SearchRadius = value;
                RaisePropertyChanged();
            }
        }

        public int PS2Regions {
            get {
                return ProfileManager.Instance.ActiveProfile.PlateSolveSettings.Regions;
            }
            set {
                ProfileManager.Instance.ActiveProfile.PlateSolveSettings.Regions = value;
                RaisePropertyChanged();
            }
        }

        public WeatherDataEnum WeatherDataType {
            get {
                return ProfileManager.Instance.ActiveProfile.WeatherDataSettings.WeatherDataType;
            }
            set {
                ProfileManager.Instance.ActiveProfile.WeatherDataSettings.WeatherDataType = value;
                RaisePropertyChanged();
            }
        }

        public string OpenWeatherMapAPIKey {
            get {
                return ProfileManager.Instance.ActiveProfile.WeatherDataSettings.OpenWeatherMapAPIKey;
            }
            set {
                ProfileManager.Instance.ActiveProfile.WeatherDataSettings.OpenWeatherMapAPIKey = value;
                RaisePropertyChanged();
            }
        }

        public string OpenWeatherMapUrl {
            get {
                return ProfileManager.Instance.ActiveProfile.WeatherDataSettings.OpenWeatherMapUrl;
            }
            set {
                ProfileManager.Instance.ActiveProfile.WeatherDataSettings.OpenWeatherMapUrl = value;
                RaisePropertyChanged();
            }
        }

        public string AstrometryAPIKey {
            get {
                return ProfileManager.Instance.ActiveProfile.PlateSolveSettings.AstrometryAPIKey;
            }
            set {
                ProfileManager.Instance.ActiveProfile.PlateSolveSettings.AstrometryAPIKey = value;
                RaisePropertyChanged();
            }
        }

        public int PHD2ServerPort {
            get {
                return ProfileManager.Instance.ActiveProfile.GuiderSettings.PHD2ServerPort;
            }
            set {
                ProfileManager.Instance.ActiveProfile.GuiderSettings.PHD2ServerPort = value;
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
                return ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.ColorSchemaName;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.ColorSchemaName = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(PrimaryColor));
                RaisePropertyChanged(nameof(SecondaryColor));
                RaisePropertyChanged(nameof(BorderColor));
                RaisePropertyChanged(nameof(BackgroundColor));
                RaisePropertyChanged(nameof(ButtonBackgroundColor));
                RaisePropertyChanged(nameof(ButtonBackgroundSelectedColor));
                RaisePropertyChanged(nameof(ButtonForegroundColor));
                RaisePropertyChanged(nameof(ButtonForegroundDisabledColor));
                RaisePropertyChanged(nameof(NotificationWarningColor));
                RaisePropertyChanged(nameof(NotificationErrorColor));
            }
        }

        public ColorSchemas ColorSchemas {
            get {
                return ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.ColorSchemas;
            }
        }

        public string AlternativeColorSchemaName {
            get {
                return ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.AltColorSchemaName;
            }
            set {

                ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.AltColorSchemaName = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(AltPrimaryColor));
                RaisePropertyChanged(nameof(AltSecondaryColor));
                RaisePropertyChanged(nameof(AltBorderColor));
                RaisePropertyChanged(nameof(AltBackgroundColor));
                RaisePropertyChanged(nameof(AltButtonBackgroundColor));
                RaisePropertyChanged(nameof(AltButtonBackgroundSelectedColor));
                RaisePropertyChanged(nameof(AltButtonForegroundColor));
                RaisePropertyChanged(nameof(AltButtonForegroundDisabledColor));
                RaisePropertyChanged(nameof(AltNotificationWarningColor));
                RaisePropertyChanged(nameof(AltNotificationErrorColor));
            }
        }


        public Color PrimaryColor {
            get {
                return ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.PrimaryColor;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.PrimaryColor = value;
                RaisePropertyChanged();
            }

        }
        public Color SecondaryColor {
            get {
                return ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.SecondaryColor;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.SecondaryColor = value;
                RaisePropertyChanged();
            }

        }
        public Color BorderColor {
            get {
                return ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.BorderColor;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.BorderColor = value;
                RaisePropertyChanged();
            }

        }
        public Color BackgroundColor {
            get {
                return ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.BackgroundColor;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.BackgroundColor = value;
                RaisePropertyChanged();
            }

        }

        public FileTypeEnum FileType {
            get {
                return ProfileManager.Instance.ActiveProfile.ImageFileSettings.FileType;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ImageFileSettings.FileType = value;
                RaisePropertyChanged();
            }
        }

        public Color ButtonBackgroundColor {
            get {
                return ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.ButtonBackgroundColor;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.ButtonBackgroundColor = value;
                RaisePropertyChanged();
            }

        }
        public Color ButtonBackgroundSelectedColor {
            get {
                return ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.ButtonBackgroundSelectedColor;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.ButtonBackgroundSelectedColor = value;
                RaisePropertyChanged();
            }

        }

        public Color ButtonForegroundColor {
            get {
                return ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.ButtonForegroundColor;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.ButtonForegroundColor = value;
                RaisePropertyChanged();
            }

        }

        public Color ButtonForegroundDisabledColor {
            get {
                return ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.ButtonForegroundDisabledColor;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.ButtonForegroundDisabledColor = value;
                RaisePropertyChanged();
            }

        }

        public double DitherPixels {
            get {
                return ProfileManager.Instance.ActiveProfile.GuiderSettings.DitherPixels;
            }
            set {
                ProfileManager.Instance.ActiveProfile.GuiderSettings.DitherPixels = value;
                RaisePropertyChanged();
            }

        }

        public bool DitherRAOnly {
            get {
                return ProfileManager.Instance.ActiveProfile.GuiderSettings.DitherRAOnly;
            }
            set {
                ProfileManager.Instance.ActiveProfile.GuiderSettings.DitherRAOnly = value;
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
                return ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.AltPrimaryColor;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.AltPrimaryColor = value;
                RaisePropertyChanged();
            }

        }
        public Color AltSecondaryColor {
            get {
                return ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.AltSecondaryColor;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.AltSecondaryColor = value;
                RaisePropertyChanged();
            }

        }
        public Color AltBorderColor {
            get {
                return ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.AltBorderColor;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.AltBorderColor = value;
                RaisePropertyChanged();
            }

        }
        public Color AltBackgroundColor {
            get {
                return ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.AltBackgroundColor;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.AltBackgroundColor = value;
                RaisePropertyChanged();
            }

        }
        public Color AltButtonBackgroundColor {
            get {
                return ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.AltButtonBackgroundColor;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.AltButtonBackgroundColor = value;
                RaisePropertyChanged();
            }

        }
        public Color AltButtonBackgroundSelectedColor {
            get {
                return ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.AltButtonBackgroundSelectedColor;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.AltButtonBackgroundSelectedColor = value;
                RaisePropertyChanged();
            }

        }

        public Color AltButtonForegroundColor {
            get {
                return ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.AltButtonForegroundColor;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.AltButtonForegroundColor = value;
                RaisePropertyChanged();
            }

        }

        public Color AltButtonForegroundDisabledColor {
            get {
                return ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.AltButtonForegroundDisabledColor;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.AltButtonForegroundDisabledColor = value;
                RaisePropertyChanged();
            }

        }

        public bool AutoMeridianFlip {
            get {
                return ProfileManager.Instance.ActiveProfile.MeridianFlipSettings.Enabled;
            }
            set {
                ProfileManager.Instance.ActiveProfile.MeridianFlipSettings.Enabled = value;
                RaisePropertyChanged();
            }
        }

        public double MinutesAfterMeridian {
            get {
                return ProfileManager.Instance.ActiveProfile.MeridianFlipSettings.MinutesAfterMeridian;
            }
            set {
                ProfileManager.Instance.ActiveProfile.MeridianFlipSettings.MinutesAfterMeridian = value;
                RaisePropertyChanged();
            }
        }

        public double PauseTimeBeforeMeridian {
            get {
                return ProfileManager.Instance.ActiveProfile.MeridianFlipSettings.PauseTimeBeforeMeridian;
            }
            set {
                ProfileManager.Instance.ActiveProfile.MeridianFlipSettings.PauseTimeBeforeMeridian = value;
                RaisePropertyChanged();
            }
        }

        public int MeridianFlipSettleTime {
            get {
                return ProfileManager.Instance.ActiveProfile.MeridianFlipSettings.SettleTime;
            }
            set {
                ProfileManager.Instance.ActiveProfile.MeridianFlipSettings.SettleTime = value;
                RaisePropertyChanged();
            }
        }


        public bool RecenterAfterFlip {
            get {
                return ProfileManager.Instance.ActiveProfile.MeridianFlipSettings.Recenter;
            }
            set {
                ProfileManager.Instance.ActiveProfile.MeridianFlipSettings.Recenter = value;
                RaisePropertyChanged();
            }
        }

        public double Latitude {
            get {
                return ProfileManager.Instance.ActiveProfile.AstrometrySettings.Latitude;
            }
            set {
                if ((HemisphereType == Hemisphere.SOUTHERN && value > 0) || (HemisphereType == Hemisphere.NORTHERN && value < 0)) {
                    value = -value;
                }
                ProfileManager.Instance.ActiveProfile.AstrometrySettings.Latitude = value;
                RaisePropertyChanged();
                Mediator.Instance.Notify(MediatorMessages.LocationChanged, null);
            }
        }

        public double Longitude {
            get {
                return ProfileManager.Instance.ActiveProfile.AstrometrySettings.Longitude;
            }
            set {
                ProfileManager.Instance.ActiveProfile.AstrometrySettings.Longitude = value;
                RaisePropertyChanged();
                Mediator.Instance.Notify(MediatorMessages.LocationChanged, null);
            }
        }

        public string SkyAtlasImageRepository {
            get {
                return ProfileManager.Instance.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository = value;
                RaisePropertyChanged();
            }
        }

        public Color NotificationWarningColor {
            get {
                return ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.NotificationWarningColor;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.NotificationWarningColor = value;
                RaisePropertyChanged();
            }

        }
        public Color NotificationErrorColor {
            get {
                return ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.NotificationErrorColor;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.NotificationErrorColor = value;
                RaisePropertyChanged();
            }

        }
        public Color AltNotificationWarningColor {
            get {
                return ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.AltNotificationWarningColor;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.AltNotificationWarningColor = value;
                RaisePropertyChanged();
            }

        }
        public Color AltNotificationErrorColor {
            get {
                return ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.AltNotificationErrorColor;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ColorSchemaSettings.AltNotificationErrorColor = value;
                RaisePropertyChanged();
            }

        }

        public bool FocuserUseFilterWheelOffsets {
            get {
                return ProfileManager.Instance.ActiveProfile.FocuserSettings.UseFilterWheelOffsets;
            }
            set {
                ProfileManager.Instance.ActiveProfile.FocuserSettings.UseFilterWheelOffsets = value;
                RaisePropertyChanged();
            }

        }

        public int FocuserAutoFocusInitialOffsetSteps {
            get {
                return ProfileManager.Instance.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps;
            }
            set {
                ProfileManager.Instance.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps = value;
                RaisePropertyChanged();
            }

        }

        public int FocuserAutoFocusStepSize {
            get {
                return ProfileManager.Instance.ActiveProfile.FocuserSettings.AutoFocusStepSize;
            }
            set {
                ProfileManager.Instance.ActiveProfile.FocuserSettings.AutoFocusStepSize = value;
                RaisePropertyChanged();
            }
        }

        public int FocuserAutoFocusExposureTime {
            get {
                return ProfileManager.Instance.ActiveProfile.FocuserSettings.AutoFocusExposureTime;
            }
            set {
                ProfileManager.Instance.ActiveProfile.FocuserSettings.AutoFocusExposureTime = value;
                RaisePropertyChanged();
            }
        }

        public string TelescopeSnapPortStart {
            get {
                return ProfileManager.Instance.ActiveProfile.TelescopeSettings.SnapPortStart;
            }
            set {
                ProfileManager.Instance.ActiveProfile.TelescopeSettings.SnapPortStart = value;
                RaisePropertyChanged();
            }
        }

        public string TelescopeSnapPortStop {
            get {
                return ProfileManager.Instance.ActiveProfile.TelescopeSettings.SnapPortStop;
            }
            set {
                ProfileManager.Instance.ActiveProfile.TelescopeSettings.SnapPortStop = value;
                RaisePropertyChanged();
            }
        }

        public double DevicePollingInterval {
            get {
                return ProfileManager.Instance.ActiveProfile.ApplicationSettings.DevicePollingInterval;
            }
            set {
                if (value > 0) {
                    ProfileManager.Instance.ActiveProfile.ApplicationSettings.DevicePollingInterval = value;
                    RaisePropertyChanged();
                }
            }
        }

        public LogLevelEnum LogLevel {
            get {
                return ProfileManager.Instance.ActiveProfile.ApplicationSettings.LogLevel;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ApplicationSettings.LogLevel = value;
                RaisePropertyChanged();
            }
        }

        public CameraBulbModeEnum CameraBulbMode {
            get {
                return ProfileManager.Instance.ActiveProfile.CameraSettings.BulbMode;
            }
            set {
                ProfileManager.Instance.ActiveProfile.CameraSettings.BulbMode = value;
                RaisePropertyChanged();
            }
        }

        public string CameraSerialPort {
            get {
                return ProfileManager.Instance.ActiveProfile.CameraSettings.SerialPort;
            }
            set {
                ProfileManager.Instance.ActiveProfile.CameraSettings.SerialPort = value;
                RaisePropertyChanged();
            }
        }

        public double CameraPixelSize {
            get {
                return ProfileManager.Instance.ActiveProfile.CameraSettings.PixelSize;
            }
            set {
                ProfileManager.Instance.ActiveProfile.CameraSettings.PixelSize = value;
                RaisePropertyChanged();
            }
        }

        public int TelescopeFocalLength {
            get {
                return ProfileManager.Instance.ActiveProfile.TelescopeSettings.FocalLength;
            }
            set {
                ProfileManager.Instance.ActiveProfile.TelescopeSettings.FocalLength = value;
                RaisePropertyChanged();
            }
        }

        public ObserveAllCollection<FilterInfo> FilterWheelFilters {
            get {
                return ProfileManager.Instance.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
            }
            set {
                ProfileManager.Instance.ActiveProfile.FilterWheelSettings.FilterWheelFilters = value;
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

        public int HistogramResolution {
            get {
                return ProfileManager.Instance.ActiveProfile.ImageSettings.HistogramResolution;
            }
            set {
                ProfileManager.Instance.ActiveProfile.ImageSettings.HistogramResolution = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HistogramMajorStep));
                RaisePropertyChanged(nameof(HistogramMinorStep));
            }
        }


        public double HistogramMajorStep {
            get {
                return HistogramResolution / 2;
            }
        }
        public double HistogramMinorStep {
            get {
                return HistogramResolution / 4;
            }
        }

        public int GuiderSettleTime {
            get {
                return ProfileManager.Instance.ActiveProfile.GuiderSettings.SettleTime;
            }
            set {
                ProfileManager.Instance.ActiveProfile.GuiderSettings.SettleTime = value;
                RaisePropertyChanged();
            }
        }

        private Profile _selectedProfile;
        public Profile SelectedProfile {
            get {
                return _selectedProfile;
            }
            set {
                _selectedProfile = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<Profile> Profiles {
            get {
                return ProfileManager.Instance.Profiles.ProfileList;
            }
        }
    }
}
