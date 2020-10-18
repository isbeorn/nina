#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyPlanetarium;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Enum;
using NINA.Utility.Exceptions;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Notification;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NINA.ViewModel {

    internal class OptionsVM : DockableVM {

        public OptionsVM(IProfileService profileService, IFilterWheelMediator filterWheelMediator) : base(profileService) {
            Title = "LblOptions";
            CanClose = false;
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["SettingsSVG"];

            this.filterWheelMediator = filterWheelMediator;
            OpenWebRequestCommand = new RelayCommand(OpenWebRequest);
            OpenImageFileDiagCommand = new RelayCommand(OpenImageFileDiag);
            OpenSharpCapSensorAnalysisFolderDiagCommand = new RelayCommand(OpenSharpCapSensorAnalysisFolderDiag);
            OpenSequenceTemplateDiagCommand = new RelayCommand(OpenSequenceTemplateDiag);
            OpenSequenceCommandAtCompletionDiagCommand = new RelayCommand(OpenSequenceCommandAtCompletionDiag);
            OpenSequenceFolderDiagCommand = new RelayCommand(OpenSequenceFolderDiag);
            OpenCygwinFileDiagCommand = new RelayCommand(OpenCygwinFileDiag);
            OpenPS2FileDiagCommand = new RelayCommand(OpenPS2FileDiag);
            OpenPHD2DiagCommand = new RelayCommand(OpenPHD2FileDiag);
            OpenASPSFileDiagCommand = new RelayCommand(OpenASPSFileDiag);
            OpenASTAPFileDiagCommand = new RelayCommand(OpenASTAPFileDiag);
            OpenLogFolderCommand = new RelayCommand(OpenLogFolder);
            ToggleColorsCommand = new RelayCommand(ToggleColors);
            DownloadIndexesCommand = new RelayCommand(DownloadIndexes);
            OpenSkyAtlasImageRepositoryDiagCommand = new RelayCommand(OpenSkyAtlasImageRepositoryDiag);
            OpenSkySurveyCacheDirectoryDiagCommand = new RelayCommand(OpenSkySurveyCacheDirectoryDiag);
            ImportFiltersCommand = new RelayCommand(ImportFilters);
            AddFilterCommand = new RelayCommand(AddFilter);
            SetAutoFocusFilterCommand = new RelayCommand(SetAutoFocusFilter);
            RemoveFilterCommand = new RelayCommand(RemoveFilter);
            AddProfileCommand = new RelayCommand(AddProfile);
            CloneProfileCommand = new RelayCommand(CloneProfile, (object o) => { return SelectedProfile != null; });
            RemoveProfileCommand = new RelayCommand(RemoveProfile, (object o) => { return SelectedProfile != null && SelectedProfile.Id != profileService.ActiveProfile.Id; });
            SelectProfileCommand = new RelayCommand(SelectProfile, (o) => {
                return SelectedProfile != null;
            });

            CopyToCustomSchemaCommand = new RelayCommand(CopyToCustomSchema, (object o) => ActiveProfile.ColorSchemaSettings.ColorSchema?.Name != "Custom");
            CopyToAlternativeCustomSchemaCommand = new RelayCommand(CopyToAlternativeCustomSchema, (object o) => ActiveProfile.ColorSchemaSettings.ColorSchema?.Name != "Alternative Custom");
            SiteFromGPSCommand = new AsyncCommand<bool>(() => Task.Run(SiteFromGPS));
            SiteFromPlanetariumCommand = new AsyncCommand<bool>(() => Task.Run(SiteFromPlanetarium));
            ImagePatterns = ImagePatterns.CreateExample();

            ScanForIndexFiles();

            profileService.LocaleChanged += (object sender, EventArgs e) => {
                ImagePatterns = ImagePatterns.CreateExample();
                RaisePropertyChanged(nameof(FileTypes));
            };

            profileService.LocationChanged += (object sender, EventArgs e) => {
                RaisePropertyChanged(nameof(Latitude));
                RaisePropertyChanged(nameof(Longitude));
            };

            profileService.ProfileChanged += (object sender, EventArgs e) => {
                ProfileChanged();
            };
        }

        private void OpenLogFolder(object obj) {
            var path = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\NINA\Logs");
            Process.Start(path);
        }

        private void OpenWebRequest(object obj) {
            var url = new Uri(obj.ToString());
            Process.Start(new ProcessStartInfo(url.AbsoluteUri));
        }

        public RelayCommand OpenPHD2DiagCommand { get; set; }

        private async Task<bool> SiteFromGPS() {
            bool loc = false; // if location was acquired
            using (var gps = new Model.MyGPS.NMEAGps(0, profileService)) {
                gps.Initialize();
                if (gps.AutoDiscover()) {
                    loc = await gps.Connect(new System.Threading.CancellationToken());
                    if (loc) {
                        Latitude = gps.Coords[1];
                        Longitude = gps.Coords[0];
                    }
                }
                return loc;
            }
        }

        private async Task<bool> SiteFromPlanetarium() {
            IPlanetarium s = PlanetariumFactory.GetPlanetarium(profileService);
            Coords loc = null;

            try {
                loc = await s.GetSite();

                if (loc != null) {
                    Latitude = loc.Latitude;
                    Longitude = loc.Longitude;
                    Notification.ShowSuccess(String.Format(Locale.Loc.Instance["LblPlanetariumCoordsOk"], s.Name));
                }
            } catch (PlanetariumFailedToConnect ex) {
                Logger.Error($"Unable to connect to {s.Name}: {ex}");
                Notification.ShowError(string.Format(Locale.Loc.Instance["LblPlanetariumFailedToConnect"], s.Name));
            } catch (Exception ex) {
                Logger.Error($"Failed to get coordinates from {s.Name}: {ex}");
                Notification.ShowError(string.Format(Locale.Loc.Instance["LblPlanetariumCoordsError"], s.Name));
            }

            return (loc != null);
        }

        private void CopyToCustomSchema(object obj) {
            ActiveProfile.ColorSchemaSettings.CopyToCustom();
        }

        private void CopyToAlternativeCustomSchema(object obj) {
            ActiveProfile.ColorSchemaSettings.CopyToAltCustom();
        }

        private void CloneProfile(object obj) {
            if (!profileService.Clone(SelectedProfile)) {
                Notification.ShowWarning(Locale.Loc.Instance["LblLoadProfileInUseWarning"]);
            }
        }

        private void RemoveProfile(object obj) {
            if (MyMessageBox.MyMessageBox.Show(string.Format(Locale.Loc.Instance["LblRemoveProfileText"], SelectedProfile?.Name, SelectedProfile?.Id), Locale.Loc.Instance["LblRemoveProfileCaption"], System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes) {
                if (!profileService.RemoveProfile(SelectedProfile)) {
                    Notification.ShowWarning(Locale.Loc.Instance["LblDeleteProfileInUseWarning"]);
                }
            }
        }

        public IProfile ActiveProfile {
            get {
                return profileService.ActiveProfile;
            }
        }

        private void ProfileChanged() {
            RaisePropertyChanged(nameof(ActiveProfile));
            RaisePropertyChanged(nameof(IndexFiles));

            foreach (System.Reflection.PropertyInfo p in this.GetType().GetProperties()) {
                if (!p.Name.ToLower().Contains("color")) {
                    RaisePropertyChanged(p.Name);
                }
            }
        }

        private void SelectProfile(object obj) {
            if (profileService.SelectProfile(SelectedProfile)) {
                ProfileChanged();
            } else {
                Notification.ShowWarning(Locale.Loc.Instance["LblLoadProfileInUseWarning"]);
            }
        }

        private void AddProfile(object obj) {
            profileService.Add();
        }

        private void RemoveFilter(object obj) {
            if (SelectedFilter == null && ActiveProfile.FilterWheelSettings.FilterWheelFilters.Count > 0) {
                SelectedFilter = ActiveProfile.FilterWheelSettings.FilterWheelFilters.Last();
            }
            ActiveProfile.FilterWheelSettings.FilterWheelFilters.Remove(SelectedFilter);
            if (ActiveProfile.FilterWheelSettings.FilterWheelFilters.Count > 0) {
                SelectedFilter = ActiveProfile.FilterWheelSettings.FilterWheelFilters.Last();
            }
        }

        private void AddFilter(object obj) {
            var pos = ActiveProfile.FilterWheelSettings.FilterWheelFilters.Count;
            var filter = new FilterInfo(Locale.Loc.Instance["LblFilter"] + (pos + 1), 0, (short)pos, 0);
            ActiveProfile.FilterWheelSettings.FilterWheelFilters.Add(filter);
            SelectedFilter = filter;
        }

        private void SetAutoFocusFilter(object obj) {
            if (SelectedFilter != null) {
                foreach (FilterInfo filter in ActiveProfile.FilterWheelSettings.FilterWheelFilters) {
                    if (filter != SelectedFilter) {
                        filter.AutoFocusFilter = false;
                    } else {
                        SelectedFilter.AutoFocusFilter = !SelectedFilter.AutoFocusFilter;
                    }
                }
            }
        }

        private void ImportFilters(object obj) {
            var filters = filterWheelMediator.GetAllFilters();
            if (filters?.Count > 0) {
                ActiveProfile.FilterWheelSettings.FilterWheelFilters.Clear();
                var l = filters.OrderBy(x => x.Position);
                foreach (var filter in l) {
                    ActiveProfile.FilterWheelSettings.FilterWheelFilters.Add(filter);
                }
            }
        }

        private void OpenSkyAtlasImageRepositoryDiag(object obj) {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog()) {
                dialog.SelectedPath = ActiveProfile.ApplicationSettings.SkyAtlasImageRepository;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    ActiveProfile.ApplicationSettings.SkyAtlasImageRepository = dialog.SelectedPath;
                }
            }
        }

        private void OpenSkySurveyCacheDirectoryDiag(object obj) {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog()) {
                dialog.SelectedPath = ActiveProfile.ApplicationSettings.SkySurveyCacheDirectory;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    ActiveProfile.ApplicationSettings.SkySurveyCacheDirectory = dialog.SelectedPath;
                }
            }
        }

        private void DownloadIndexes(object obj) {
            AstrometryIndexDownloader.AstrometryIndexDownloaderVM.Show(ActiveProfile.PlateSolveSettings.CygwinLocation);
            ScanForIndexFiles();
        }

        private void OpenImageFileDiag(object o) {
            using (var diag = new System.Windows.Forms.FolderBrowserDialog()) {
                diag.SelectedPath = ActiveProfile.ImageFileSettings.FilePath;
                System.Windows.Forms.DialogResult result = diag.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK) {
                    ActiveProfile.ImageFileSettings.FilePath = diag.SelectedPath + "\\";
                }
            }
        }

        private void OpenSharpCapSensorAnalysisFolderDiag(object o) {
            using (var diag = new System.Windows.Forms.FolderBrowserDialog()) {
                diag.SelectedPath = ActiveProfile.ImageSettings.SharpCapSensorAnalysisFolder;
                System.Windows.Forms.DialogResult result = diag.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK) {
                    ActiveProfile.ImageSettings.SharpCapSensorAnalysisFolder = diag.SelectedPath + "\\";
                    var vm = (ApplicationVM)Application.Current.Resources["AppVM"];
                    var sensorAnalysisData = vm.ExposureCalculatorVM.LoadSensorAnalysisData(ActiveProfile.ImageSettings.SharpCapSensorAnalysisFolder);
                    Notification.ShowInformation(String.Format(Locale.Loc.Instance["LblSharpCapSensorAnalysisLoadedFormat"], sensorAnalysisData.Count));
                }
            }
        }

        private void OpenSequenceTemplateDiag(object o) {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = Locale.Loc.Instance["LblSequenceTemplate"];
            dialog.FileName = "Sequence";
            dialog.DefaultExt = ".xml";
            dialog.Filter = "XML documents|*.xml";

            if (dialog.ShowDialog() == true) {
                ActiveProfile.SequenceSettings.TemplatePath = dialog.FileName;
            }
        }

        private void OpenSequenceCommandAtCompletionDiag(object o) {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = Locale.Loc.Instance["LblSequenceCommandAtCompletionTitle"];
            dialog.FileName = "SequenceCompleteCommand";
            dialog.DefaultExt = ".*";
            dialog.Filter = "Any executable command |*.*";

            if (dialog.ShowDialog() == true) {
                ActiveProfile.SequenceSettings.SequenceCompleteCommand = dialog.FileName;
            }
        }

        private void OpenSequenceFolderDiag(object o) {
            using (var diag = new System.Windows.Forms.FolderBrowserDialog()) {
                diag.SelectedPath = ActiveProfile.SequenceSettings.DefaultSequenceFolder;
                System.Windows.Forms.DialogResult result = diag.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK) {
                    ActiveProfile.SequenceSettings.DefaultSequenceFolder = diag.SelectedPath + "\\";
                }
            }
        }

        private void OpenCygwinFileDiag(object o) {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog()) {
                dialog.SelectedPath = profileService.ActiveProfile.PlateSolveSettings.CygwinLocation;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    ActiveProfile.PlateSolveSettings.CygwinLocation = dialog.SelectedPath;
                }
            }
        }

        private void OpenPS2FileDiag(object o) {
            var dialog = GetFilteredFileDialog(profileService.ActiveProfile.PlateSolveSettings.PS2Location, "PlateSolve2.exe", "PlateSolve2|PlateSolve2.exe");
            if (dialog.ShowDialog() == true) {
                ActiveProfile.PlateSolveSettings.PS2Location = dialog.FileName;
            }
        }

        private void OpenPHD2FileDiag(object o) {
            var dialog = GetFilteredFileDialog(profileService.ActiveProfile.GuiderSettings.PHD2Path, "phd2.exe", "PHD2|phd2.exe");
            if (dialog.ShowDialog() == true) {
                ActiveProfile.GuiderSettings.PHD2Path = dialog.FileName;
            }
        }

        private void OpenASPSFileDiag(object o) {
            var dialog = GetFilteredFileDialog(profileService.ActiveProfile.PlateSolveSettings.AspsLocation, "PlateSolver.exe", "ASPS|PlateSolver.exe");
            if (dialog.ShowDialog() == true) {
                ActiveProfile.PlateSolveSettings.AspsLocation = dialog.FileName;
            }
        }

        private void OpenASTAPFileDiag(object o) {
            var dialog = GetFilteredFileDialog(profileService.ActiveProfile.PlateSolveSettings.ASTAPLocation, "astap.exe", "ASTAP|astap.exe");
            if (dialog.ShowDialog() == true) {
                ActiveProfile.PlateSolveSettings.ASTAPLocation = dialog.FileName;
            }
        }

        private Microsoft.Win32.OpenFileDialog GetFilteredFileDialog(string path, string filename, string filter) {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();

            if (File.Exists(path)) {
                dialog.InitialDirectory = Path.GetDirectoryName(path);
            }
            dialog.FileName = filename;
            dialog.Filter = filter;
            return dialog;
        }

        private void ScanForIndexFiles() {
            IndexFiles.Clear();
            try {
                DirectoryInfo di = new DirectoryInfo(ActiveProfile.PlateSolveSettings.CygwinLocation + @"\usr\share\astrometry\data");
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

        public ICommand OpenASPSFileDiagCommand { get; private set; }

        public ICommand OpenASTAPFileDiagCommand { get; private set; }

        public ICommand OpenImageFileDiagCommand { get; private set; }
        public ICommand OpenSharpCapSensorAnalysisFolderDiagCommand { get; private set; }
        public ICommand SensorAnalysisFolderChangedCommand { get; private set; }

        public ICommand OpenSequenceTemplateDiagCommand { get; private set; }

        public ICommand OpenSequenceCommandAtCompletionDiagCommand { get; private set; }
        public ICommand OpenSequenceFolderDiagCommand { get; private set; }

        public ICommand OpenWebRequestCommand { get; private set; }

        public ICommand ToggleColorsCommand { get; private set; }

        public ICommand OpenSkyAtlasImageRepositoryDiagCommand { get; private set; }
        public ICommand OpenSkySurveyCacheDirectoryDiagCommand { get; private set; }

        public ICommand ImportFiltersCommand { get; private set; }

        public ICommand AddFilterCommand { get; private set; }
        public ICommand SetAutoFocusFilterCommand { get; private set; }

        public ICommand RemoveFilterCommand { get; private set; }

        public ICommand AddProfileCommand { get; private set; }
        public ICommand CloneProfileCommand { get; private set; }
        public ICommand RemoveProfileCommand { get; private set; }
        public ICommand OpenLogFolderCommand { get; private set; }
        public ICommand CopyToCustomSchemaCommand { get; private set; }
        public ICommand CopyToAlternativeCustomSchemaCommand { get; private set; }
        public ICommand SiteFromGPSCommand { get; private set; }
        public ICommand SiteFromPlanetariumCommand { get; private set; }

        public ICommand SelectProfileCommand { get; private set; }

        private ObservableCollection<CultureInfo> _availableLanguages = new ObservableCollection<CultureInfo>() {
            new CultureInfo("en-GB"),
            new CultureInfo("en-US"),
            new CultureInfo("de-DE"),
            new CultureInfo("it-IT"),
            new CultureInfo("es-ES"),
            new CultureInfo("gl-ES"),
            new CultureInfo("zh-CN"),
            new CultureInfo("zh-HK"),
            new CultureInfo("zh-TW"),
            new CultureInfo("fr-FR"),
            new CultureInfo("ru-RU"),
            new CultureInfo("pl-PL"),
            new CultureInfo("nl-NL"),
            new CultureInfo("ja-JP"),
            new CultureInfo("tr-TR"),
            new CultureInfo("pt-PT"),
            new CultureInfo("el-GR")
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
                return profileService.ActiveProfile.ApplicationSettings.Language;
            }
            set {
                profileService.ChangeLocale(value);
                RaisePropertyChanged();
            }
        }

        private void ToggleColors(object o) {
            ActiveProfile.ColorSchemaSettings.ToggleSchema();
        }

#pragma warning disable CS0612 // Type or member is obsolete

        public FileTypeEnum[] FileTypes => Enum.GetValues(typeof(FileTypeEnum))
                    .Cast<FileTypeEnum>()
                    .Where(p => p != FileTypeEnum.RAW)
                    .Where(p => p != FileTypeEnum.TIFF_LZW)
                    .Where(p => p != FileTypeEnum.TIFF_ZIP)
                    .ToArray();

#pragma warning restore CS0612 // Type or member is obsolete

        public TIFFCompressionTypeEnum[] TIFFCompressionTypes {
            get {
                return Enum.GetValues(typeof(TIFFCompressionTypeEnum))
                    .Cast<TIFFCompressionTypeEnum>()
                    .ToArray();
            }
        }

        public XISFCompressionTypeEnum[] XISFCompressionTypes {
            get {
                return Enum.GetValues(typeof(XISFCompressionTypeEnum))
                    .Cast<XISFCompressionTypeEnum>()
                    .ToArray();
            }
        }

        public XISFChecksumTypeEnum[] XISFChecksumTypes {
            get {
                /*
                 * NOTE: PixInsight does not yet support opening files with SHA3 checksums, despite then
                 * being defined as part of the XISF 1.0 specification. We will not permit the user to choose
                 * these as a checksum type until PixInsight also supports them, which is supposed to be in early
                 * 2020.
                 */
                return Enum.GetValues(typeof(XISFChecksumTypeEnum))
                    .Cast<XISFChecksumTypeEnum>()
                    .Where(p => p != XISFChecksumTypeEnum.SHA3_256)
                    .Where(p => p != XISFChecksumTypeEnum.SHA3_512)
                    .ToArray();
            }
        }

        private ImagePatterns _imagePatterns;

        public ImagePatterns ImagePatterns {
            get {
                return _imagePatterns;
            }
            set {
                _imagePatterns = value;
                RaisePropertyChanged();
            }
        }

        public double Latitude {
            get {
                return profileService.ActiveProfile.AstrometrySettings.Latitude;
            }
            set {
                profileService.ChangeLatitude(value);
                RaisePropertyChanged();
            }
        }

        public double Longitude {
            get {
                return profileService.ActiveProfile.AstrometrySettings.Longitude;
            }
            set {
                profileService.ChangeLongitude(value);
                RaisePropertyChanged();
            }
        }

        public AutoUpdateSourceEnum AutoUpdateSource {
            get {
                return (AutoUpdateSourceEnum)NINA.Properties.Settings.Default.AutoUpdateSource;
            }
            set {
                NINA.Properties.Settings.Default.AutoUpdateSource = (int)value;
                NINA.Properties.Settings.Default.Save();
                RaisePropertyChanged();
            }
        }

        public LogLevelEnum LogLevel {
            get {
                return profileService.ActiveProfile.ApplicationSettings.LogLevel;
            }
            set {
                profileService.ActiveProfile.ApplicationSettings.LogLevel = value;
                Logger.SetLogLevel(value);
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

        private ProfileMeta _selectedProfile;
        private IFilterWheelMediator filterWheelMediator;

        public ProfileMeta SelectedProfile {
            get {
                return _selectedProfile;
            }
            set {
                _selectedProfile = value;
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<ProfileMeta> Profiles {
            get {
                return profileService.Profiles;
            }
        }
    }
}