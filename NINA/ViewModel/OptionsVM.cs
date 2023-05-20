#region "copyright"

/*
    Copyright © 2016 - 2023 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Interfaces;
using NINA.Core.Interfaces.API.SGP;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.MyMessageBox;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Exceptions;
using NINA.Equipment.Interfaces;
using NINA.Image.ImageAnalysis;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Utility;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.Utility;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace NINA.ViewModel {

    internal class OptionsVM : DockableVM, IOptionsVM {

        public OptionsVM(IProfileService profileService,
                         IAllDeviceConsumer deviceConsumer,
                         IVersionCheckVM versionCheckVM,
                         ProjectVersion projectVersion,
                         IPlanetariumFactory planetariumFactory,
                         IGnssFactory gnssFactory,
                         ISGPServiceHost sgpServiceHost,
                         IPluggableBehaviorSelector<IStarDetection> starDetectionSelector,
                         IPluggableBehaviorSelector<IStarAnnotator> starAnnotatorSelector,
                         IPluggableBehaviorSelector<IAutoFocusVMFactory> autoFocusVMFactorySelector) : base(profileService) {
            Title = Loc.Instance["LblOptions"];
            CanClose = false;
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["SettingsSVG"];

            DeviceConsumer = deviceConsumer;
            customPatterns = new List<ImagePattern>();
            this.versionCheckVM = versionCheckVM;
            this.projectVersion = projectVersion;
            this.planetariumFactory = planetariumFactory;
            this.gnssFactory = gnssFactory;
            this.sgpServiceHost = sgpServiceHost;
            this.PluggableStarDetection = starDetectionSelector;
            this.PluggableStarAnnotator = starAnnotatorSelector;
            this.PluggableAutoFocusVMFactory = autoFocusVMFactorySelector;
            OpenWebRequestCommand = new RelayCommand(OpenWebRequest);
            OpenImageFileDiagCommand = new RelayCommand(OpenImageFileDiag);
            OpenSequenceTemplateDiagCommand = new RelayCommand(OpenSequenceTemplateDiag);
            OpenStartupSequenceTemplateDiagCommand = new RelayCommand(OpenStartupSequenceTemplateDiag);
            OpenTargetsFolderDiagCommand = new RelayCommand(OpenTargetsFolderDiag);
            OpenSequenceFolderDiagCommand = new RelayCommand(OpenSequenceFolderDiag);
            OpenSequenceTemplateFolderDiagCommand = new RelayCommand(OpenSequenceTemplateFolderDiag);
            OpenCygwinFileDiagCommand = new RelayCommand(OpenCygwinFileDiag);
            OpenPS2FileDiagCommand = new RelayCommand(OpenPS2FileDiag);
            OpenPS3FileDiagCommand = new RelayCommand(OpenPS3FileDiag);
            OpenASPSFileDiagCommand = new RelayCommand(OpenASPSFileDiag);
            OpenASTAPFileDiagCommand = new RelayCommand(OpenASTAPFileDiag);
            OpenHorizonFilePathDiagCommand = new RelayCommand(OpenHorizonFilePathDiag);
            OpenLogFolderCommand = new RelayCommand(OpenLogFolder);
            ToggleColorsCommand = new RelayCommand(ToggleColors);
            DownloadIndexesCommand = new RelayCommand(DownloadIndexes);
            OpenSkyAtlasImageRepositoryDiagCommand = new RelayCommand(OpenSkyAtlasImageRepositoryDiag);
            OpenSkySurveyCacheDirectoryDiagCommand = new RelayCommand(OpenSkySurveyCacheDirectoryDiag);
            AddFilterCommand = new RelayCommand(AddFilter);
            SetAutoFocusFilterCommand = new RelayCommand(SetAutoFocusFilter);
            RemoveFilterCommand = new RelayCommand(RemoveFilter);
            AddProfileCommand = new RelayCommand(AddProfile);
            CloneProfileCommand = new RelayCommand(CloneProfile, (object o) => { return SelectedProfile != null; });
            RemoveProfileCommand = new RelayCommand(RemoveProfile, (object o) => { return SelectedProfile != null && SelectedProfile.Id != profileService.ActiveProfile.Id; });
            SelectProfileCommand = new RelayCommand(SelectProfile, (o) => {
                return SelectedProfile != null && SelectedProfile.Id != profileService.ActiveProfile.Id;
            });

            CopyToCustomSchemaCommand = new RelayCommand(CopyToCustomSchema, (object o) => ActiveProfile.ColorSchemaSettings.ColorSchema?.Name != "Custom");
            CopyToAlternativeCustomSchemaCommand = new RelayCommand(CopyToAlternativeCustomSchema, (object o) => ActiveProfile.ColorSchemaSettings.ColorSchema?.Name != "Alternative Custom");
            SiteFromGnssCommand = new AsyncCommand<bool>(() => Task.Run(SiteFromGnss));
            SiteFromPlanetariumCommand = new AsyncCommand<bool>(() => Task.Run(SiteFromPlanetarium));
            RecreatePatterns();

            ScanForIndexFiles();

            profileService.LocaleChanged += (object sender, EventArgs e) => {
                RecreatePatterns();
                RaisePropertyChanged(nameof(FileTypes));
            };

            profileService.LocationChanged += (object sender, EventArgs e) => {
                RaisePropertyChanged(nameof(Latitude));
                RaisePropertyChanged(nameof(Longitude));
                RaisePropertyChanged(nameof(Elevation));
            };

            profileService.HorizonChanged += (object sender, EventArgs e) => {
                RaisePropertyChanged(nameof(HorizonFilePath));
            };

            profileService.ProfileChanged += (object sender, EventArgs e) => {
                ProfileChanged();
                Profiles.Refresh();
                FilePatternsExpanded = !string.IsNullOrWhiteSpace(profileService.ActiveProfile.ImageFileSettings.FilePatternBIAS)
                    || !string.IsNullOrWhiteSpace(profileService.ActiveProfile.ImageFileSettings.FilePatternDARK)
                    || !string.IsNullOrWhiteSpace(profileService.ActiveProfile.ImageFileSettings.FilePatternFLAT)
                    || !string.IsNullOrWhiteSpace(profileService.ActiveProfile.ImageFileSettings.FilePatternDARKFLAT);
            };
            ToggleSGPService();

            FamilyTypeface = ApplicationFontFamily.FamilyTypefaces.FirstOrDefault(x => x.Weight == FontWeight && x.Style == FontStyle && x.Stretch == FontStretch);

            Profiles = CollectionViewSource.GetDefaultView(profileService.Profiles);
            Profiles.SortDescriptions.Add(new SortDescription("IsActive", ListSortDirection.Descending));
            Profiles.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            FilePatternsExpanded = !string.IsNullOrWhiteSpace(profileService.ActiveProfile.ImageFileSettings.FilePatternBIAS)
                || !string.IsNullOrWhiteSpace(profileService.ActiveProfile.ImageFileSettings.FilePatternDARK)
                || !string.IsNullOrWhiteSpace(profileService.ActiveProfile.ImageFileSettings.FilePatternFLAT)
                || !string.IsNullOrWhiteSpace(profileService.ActiveProfile.ImageFileSettings.FilePatternDARKFLAT);
        }

        public bool IsX64 => !DllLoader.IsX86();

        public void AddImagePattern(ImagePattern pattern) {
            customPatterns.Add(pattern);
            RecreatePatterns();
        }

        private void RecreatePatterns() {
            var patterns = ImagePatterns.CreateExample();
            foreach (var cp in customPatterns) {
                patterns.Add(cp);
            }
            ImagePatterns = patterns;
            RaisePropertyChanged(nameof(FilePatternPreview));
        }

        public string FilePattern {
            get => profileService.ActiveProfile.ImageFileSettings.FilePattern;
            set {
                profileService.ActiveProfile.ImageFileSettings.FilePattern = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(FilePatternPreview));
            }
        }

        public string FilePatternDARK {
            get => profileService.ActiveProfile.ImageFileSettings.FilePatternDARK;
            set {
                profileService.ActiveProfile.ImageFileSettings.FilePatternDARK = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(FilePatternPreviewDARK));
            }
        }

        public string FilePatternFLAT {
            get => profileService.ActiveProfile.ImageFileSettings.FilePatternFLAT;
            set {
                profileService.ActiveProfile.ImageFileSettings.FilePatternFLAT = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(FilePatternPreviewFLAT));
            }
        }

        public string FilePatternBIAS {
            get => profileService.ActiveProfile.ImageFileSettings.FilePatternBIAS;
            set {
                profileService.ActiveProfile.ImageFileSettings.FilePatternBIAS = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(FilePatternPreviewBIAS));
            }
        }

        public string FilePatternDARKFLAT {
            get => profileService.ActiveProfile.ImageFileSettings.FilePatternDARKFLAT;
            set {
                profileService.ActiveProfile.ImageFileSettings.FilePatternDARKFLAT = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(FilePatternPreviewDARKFLAT));
            }
        }

        private bool filePatternsExpanded;

        public bool FilePatternsExpanded {
            get => filePatternsExpanded;
            set {
                filePatternsExpanded = value;
                RaisePropertyChanged();
            }
        }

        public string FilePatternPreview => ImagePatterns.GetImageFileString(FilePattern).Replace("\\", " › ");

        public string FilePatternPreviewDARK => ImagePatterns.GetImageFileString(FilePatternDARK, "DARK").Replace("\\", " › ");

        public string FilePatternPreviewBIAS => ImagePatterns.GetImageFileString(FilePatternBIAS, "BIAS").Replace("\\", " › ");

        public string FilePatternPreviewFLAT => ImagePatterns.GetImageFileString(FilePatternFLAT, "FLAT").Replace("\\", " › ");

        public string FilePatternPreviewDARKFLAT => ImagePatterns.GetImageFileString(FilePatternDARKFLAT, "DARKFLAT").Replace("\\", " › ");

        private List<ImagePattern> customPatterns;

        private void OpenHorizonFilePathDiag(object obj) {
            var dialog = GetFilteredFileDialog(string.Empty, string.Empty, "Horizon File|*.hrz;*.hzn;*.txt|MountWizzard4 Horizon File|*.hpts");
            if (dialog.ShowDialog() == true) {
                HorizonFilePath = dialog.FileName;
            }
        }

        public string HorizonFilePath {
            get => profileService.ActiveProfile.AstrometrySettings.HorizonFilePath;

            set {
                profileService.ChangeHorizon(value);
                RaisePropertyChanged();
            }
        }

        private void OpenStartupSequenceTemplateDiag(object obj) {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = Loc.Instance["LblSequenceTemplate"];
            dialog.FileName = "Sequence";
            dialog.DefaultExt = ".json";
            dialog.Filter = "N.I.N.A. sequence JSON|*." + dialog.DefaultExt;

            if (dialog.ShowDialog() == true) {
                ActiveProfile.SequenceSettings.StartupSequenceTemplate = dialog.FileName;
            }
        }

        private readonly ISGPServiceHost sgpServiceHost;
        public IAllDeviceConsumer DeviceConsumer { get; }

        private void OpenLogFolder(object obj) {
            var path = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\NINA\Logs");
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        private void OpenWebRequest(object obj) {
            var url = new Uri(obj.ToString());
            Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
        }

        private async Task<bool> SiteFromGnss() {
            IGnss gnss = gnssFactory.GetGnssSource();
            Location loc = null;

            try {
                loc = await gnss.GetLocation();

                if (loc != null) {
                    Latitude = loc.Latitude;
                    Longitude = loc.Longitude;
                    Elevation = loc.Elevation;

                    Logger.Info($"Location information from {gnss.Name}: Latitude: {loc.Latitude}, Longitude: {loc.Longitude}, Elevation: {loc.Elevation}");
                    Notification.ShowSuccess(string.Format(Loc.Instance["LblGnssLocationSet"], gnss.Name));
                }
            } catch (GnssNoFixException ex) {
                string message;

                if (!string.IsNullOrEmpty(ex.Message)) {
                    message = ex.Message;
                } else {
                    message = string.Format(Loc.Instance["LblGnssNoFix"], gnss.Name);
                }

                Logger.Error(message);
                Notification.ShowExternalError(message, Loc.Instance["LblGnss"]);
            } catch (GnssNotFoundException ex) {
                Logger.Error(ex.Message);
                Notification.ShowError(string.Format(Loc.Instance["LblGnssNotFound"], gnss.Name));
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(string.Format(Loc.Instance["LblGnssConnectFail"], gnss.Name));
            }

            return (loc != null);
        }

        private async Task<bool> SiteFromPlanetarium() {
            IPlanetarium s = planetariumFactory.GetPlanetarium();
            Location loc = null;

            try {
                loc = await s.GetSite();

                if (loc != null) {
                    Latitude = loc.Latitude;
                    Longitude = loc.Longitude;
                    Elevation = loc.Elevation;
                    Notification.ShowSuccess(string.Format(Loc.Instance["LblPlanetariumCoordsOk"], s.Name));
                }
            } catch (PlanetariumFailedToConnect ex) {
                Logger.Error($"Unable to connect to {s.Name}: {ex}");
                Notification.ShowError(string.Format(Loc.Instance["LblPlanetariumFailedToConnect"], s.Name));
            } catch (Exception ex) {
                Logger.Error($"Failed to get coordinates from {s.Name}: {ex}");
                Notification.ShowError(string.Format(Loc.Instance["LblPlanetariumCoordsError"], s.Name));
            }

            return (loc != null);
        }

        public string Version => projectVersion.ToString();

        private void CopyToCustomSchema(object obj) {
            ActiveProfile.ColorSchemaSettings.CopyToCustom();
        }

        private void CopyToAlternativeCustomSchema(object obj) {
            ActiveProfile.ColorSchemaSettings.CopyToAltCustom();
        }

        private void CloneProfile(object obj) {
            if (!profileService.Clone(SelectedProfile)) {
                Notification.ShowWarning(Loc.Instance["LblLoadProfileInUseWarning"]);
            } else {
                if (!Properties.Settings.Default.SingleDockLayout) {
                    try {
                        var currentProfileId = profileService.ActiveProfile.Id;
                        var dockPath = DockManagerVM.GetDockConfigPath(currentProfileId);

                        var newProfile = profileService.Profiles.Last();

                        if (File.Exists(dockPath)) {
                            File.Copy(dockPath, Path.Combine(Path.GetDirectoryName(dockPath), $"{newProfile.Id}.dock.config"));
                        }
                    } catch (Exception e) {
                        Logger.Error("Failed to clone dock config", e);
                    }
                }
            }
        }

        private void RemoveProfile(object obj) {
            if (MyMessageBox.Show(string.Format(Loc.Instance["LblRemoveProfileText"], SelectedProfile?.Name, SelectedProfile?.Id), Loc.Instance["LblRemoveProfileCaption"], System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes) {
                if (!profileService.RemoveProfile(SelectedProfile)) {
                    Notification.ShowWarning(Loc.Instance["LblDeleteProfileInUseWarning"]);
                }
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

        private void ToggleSGPService() {
            if (SGPServerEnabled) {
                sgpServiceHost.RunService();
            } else {
                sgpServiceHost.Stop();
            }
        }

        private void SelectProfile(object obj) {
            if (!profileService.SelectProfile(SelectedProfile)) {
                Notification.ShowWarning(Loc.Instance["LblLoadProfileInUseWarning"]);
                ProfileService.ActivateInstanceOfNinaReferencingProfile(SelectedProfile.Id.ToString());
            }
        }

        private void AddProfile(object obj) {
            profileService.Add();
        }

        private void RemoveFilter(object obj) {
            var filters = ActiveProfile.FilterWheelSettings.FilterWheelFilters;
            if (SelectedFilter == null && filters.Count > 0) {
                SelectedFilter = filters.Last();
            }
            filters.Remove(SelectedFilter);
            if (filters.Count > 0) {
                SelectedFilter = filters.Last();
            }
            for (short i = 0; i < filters.Count; i++) {
                filters[i].Position = i;
            }
        }

        private void AddFilter(object obj) {
            var pos = ActiveProfile.FilterWheelSettings.FilterWheelFilters.Count;
            var filter = new FilterInfo(Loc.Instance["LblFilter"] + (pos + 1), 0, (short)pos, -1, new BinningMode(1, 1), -1, -1);
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

        private void OpenSequenceTemplateDiag(object o) {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = Loc.Instance["LblSequenceTemplate"];
            dialog.FileName = "Sequence";
            dialog.DefaultExt = ".xml";
            dialog.Filter = "XML documents|*.xml";

            if (dialog.ShowDialog() == true) {
                ActiveProfile.SequenceSettings.TemplatePath = dialog.FileName;
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

        private void OpenTargetsFolderDiag(object o) {
            using (var diag = new System.Windows.Forms.FolderBrowserDialog()) {
                diag.SelectedPath = ActiveProfile.SequenceSettings.SequencerTargetsFolder;
                System.Windows.Forms.DialogResult result = diag.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK) {
                    ActiveProfile.SequenceSettings.SequencerTargetsFolder = diag.SelectedPath + "\\";
                }
            }
        }

        private void OpenSequenceTemplateFolderDiag(object o) {
            using (var diag = new System.Windows.Forms.FolderBrowserDialog()) {
                diag.SelectedPath = ActiveProfile.SequenceSettings.SequencerTemplatesFolder;
                System.Windows.Forms.DialogResult result = diag.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK) {
                    ActiveProfile.SequenceSettings.SequencerTemplatesFolder = diag.SelectedPath + "\\";
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

        private void OpenPS3FileDiag(object o) {
            var dialog = GetFilteredFileDialog(profileService.ActiveProfile.PlateSolveSettings.PS3Location, "PlateSolve3.exe", "PlateSolve3|PlateSolve3*.exe");
            if (dialog.ShowDialog() == true) {
                ActiveProfile.PlateSolveSettings.PS3Location = dialog.FileName;
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

        public static Microsoft.Win32.OpenFileDialog GetFilteredFileDialog(string path, string filename, string filter) {
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

        public ICommand OpenPS3FileDiagCommand { get; private set; }

        public ICommand OpenASPSFileDiagCommand { get; private set; }

        public ICommand OpenHorizonFilePathDiagCommand { get; private set; }

        public ICommand OpenASTAPFileDiagCommand { get; private set; }

        public ICommand OpenImageFileDiagCommand { get; private set; }
        public ICommand SensorAnalysisFolderChangedCommand { get; private set; }

        public ICommand OpenSequenceTemplateDiagCommand { get; private set; }
        public ICommand OpenStartupSequenceTemplateDiagCommand { get; private set; }
        public ICommand OpenTargetsFolderDiagCommand { get; private set; }

        public ICommand OpenSequenceFolderDiagCommand { get; private set; }
        public ICommand OpenSequenceTemplateFolderDiagCommand { get; private set; }

        public ICommand OpenWebRequestCommand { get; private set; }

        public ICommand ToggleColorsCommand { get; private set; }

        public ICommand OpenSkyAtlasImageRepositoryDiagCommand { get; private set; }
        public ICommand OpenSkySurveyCacheDirectoryDiagCommand { get; private set; }

        public ICommand AddFilterCommand { get; private set; }
        public ICommand SetAutoFocusFilterCommand { get; private set; }

        public ICommand RemoveFilterCommand { get; private set; }

        public ICommand AddProfileCommand { get; private set; }
        public ICommand CloneProfileCommand { get; private set; }
        public ICommand RemoveProfileCommand { get; private set; }
        public ICommand OpenLogFolderCommand { get; private set; }
        public ICommand CopyToCustomSchemaCommand { get; private set; }
        public ICommand CopyToAlternativeCustomSchemaCommand { get; private set; }
        public ICommand SiteFromGnssCommand { get; private set; }
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
            new CultureInfo("el-GR"),
            new CultureInfo("cs-CZ"),
            new CultureInfo("ca-ES"),
            new CultureInfo("nb-NO"),
            new CultureInfo("ko-KR")
        };

        public ObservableCollection<CultureInfo> AvailableLanguages {
            get => _availableLanguages;
            set {
                _availableLanguages = value;
                RaisePropertyChanged();
            }
        }

        public CultureInfo Language {
            get => profileService.ActiveProfile.ApplicationSettings.Language;
            set {
                profileService.ChangeLocale(value);
                RaisePropertyChanged();
            }
        }

        public FontFamily ApplicationFontFamily {
            get => NINA.Properties.Settings.Default.ApplicationFontFamily;
            set {
                NINA.Properties.Settings.Default.ApplicationFontFamily = value;
                CoreUtil.SaveSettings(NINA.Properties.Settings.Default);

                FamilyTypeface = value.FamilyTypefaces.FirstOrDefault(x => (x.AdjustedFaceNames.First().Value == "Regular") || (x.AdjustedFaceNames.First().Value == "Normal")) ?? value.FamilyTypefaces.FirstOrDefault();
                FontStretch = FamilyTypeface.Stretch;
                FontStyle = FamilyTypeface.Style;
                FontWeight = FamilyTypeface.Weight;

                RaisePropertyChanged();
            }
        }

        private FamilyTypeface familyTypeface;

        public FamilyTypeface FamilyTypeface {
            get => familyTypeface;
            set {
                familyTypeface = value;
                FontStretch = familyTypeface.Stretch;
                FontStyle = familyTypeface.Style;
                FontWeight = familyTypeface.Weight;

                RaisePropertyChanged();
            }
        }

        public bool SingleDockLayout {
            get => NINA.Properties.Settings.Default.SingleDockLayout;
            set {
                NINA.Properties.Settings.Default.SingleDockLayout = value;
                CoreUtil.SaveSettings(NINA.Properties.Settings.Default);
                RaisePropertyChanged();
                RequiresRestart = true;
            }
        }

        public FontStretch FontStretch {
            get => NINA.Properties.Settings.Default.FontStretch;
            set {
                NINA.Properties.Settings.Default.FontStretch = value;
                CoreUtil.SaveSettings(NINA.Properties.Settings.Default);
                RaisePropertyChanged();
            }
        }

        public FontStyle FontStyle {
            get => NINA.Properties.Settings.Default.FontStyle;
            set {
                NINA.Properties.Settings.Default.FontStyle = value;
                CoreUtil.SaveSettings(NINA.Properties.Settings.Default);
                RaisePropertyChanged();
            }
        }

        public FontWeight FontWeight {
            get => NINA.Properties.Settings.Default.FontWeight;
            set {
                NINA.Properties.Settings.Default.FontWeight = value;
                CoreUtil.SaveSettings(NINA.Properties.Settings.Default);
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

        public TIFFCompressionTypeEnum[] TIFFCompressionTypes => Enum.GetValues(typeof(TIFFCompressionTypeEnum))
                    .Cast<TIFFCompressionTypeEnum>()
                    .ToArray();

        public XISFCompressionTypeEnum[] XISFCompressionTypes => Enum.GetValues(typeof(XISFCompressionTypeEnum))
                    .Cast<XISFCompressionTypeEnum>()
                    .ToArray();

        public XISFChecksumTypeEnum[] XISFChecksumTypes =>
                /*
* NOTE: PixInsight does not yet support opening files with SHA3 checksums, despite then
* being defined as part of the XISF 1.0 specification. We will not permit the user to choose
* these as a checksum type until PixInsight also supports them.
*/
                Enum.GetValues(typeof(XISFChecksumTypeEnum))
                    .Cast<XISFChecksumTypeEnum>()
                    .Where(p => p != XISFChecksumTypeEnum.SHA3_256)
                    .Where(p => p != XISFChecksumTypeEnum.SHA3_512)
                    .ToArray();

        private ImagePatterns _imagePatterns;

        public ImagePatterns ImagePatterns {
            get => _imagePatterns;
            set {
                _imagePatterns = value;
                RaisePropertyChanged();
            }
        }

        public double Latitude {
            get => profileService.ActiveProfile.AstrometrySettings.Latitude;
            set {
                profileService.ChangeLatitude(value);
                RaisePropertyChanged();
            }
        }

        public double Longitude {
            get => profileService.ActiveProfile.AstrometrySettings.Longitude;
            set {
                profileService.ChangeLongitude(value);
                RaisePropertyChanged();
            }
        }

        public double Elevation {
            get => profileService.ActiveProfile.AstrometrySettings.Elevation;
            set {
                profileService.ChangeElevation(value);
                RaisePropertyChanged();
            }
        }

        public AutoUpdateSourceEnum AutoUpdateSource {
            get => (AutoUpdateSourceEnum)NINA.Properties.Settings.Default.AutoUpdateSource;
            set {
                NINA.Properties.Settings.Default.AutoUpdateSource = (int)value;
                CoreUtil.SaveSettings(NINA.Properties.Settings.Default);
                versionCheckVM.CheckUpdate();
                RaisePropertyChanged();
            }
        }

        public bool UseSavedProfileSelection {
            get => Properties.Settings.Default.UseSavedProfileSelection;
            set {
                NINA.Properties.Settings.Default.UseSavedProfileSelection = value;
                CoreUtil.SaveSettings(NINA.Properties.Settings.Default);
                RaisePropertyChanged();
            }
        }

        public bool SGPServerEnabled {
            get => NINA.Properties.Settings.Default.SGPServerEnabled;
            set {
                NINA.Properties.Settings.Default.SGPServerEnabled = value;
                CoreUtil.SaveSettings(NINA.Properties.Settings.Default);
                ToggleSGPService();
                RaisePropertyChanged();
            }
        }

        public int SaveQueueSize {
            get => Properties.Settings.Default.SaveQueueSize;
            set {
                if (value < 1) { value = 1; }
                if (value != SaveQueueSize) {
                    NINA.Properties.Settings.Default.SaveQueueSize = value;
                    CoreUtil.SaveSettings(NINA.Properties.Settings.Default);
                    RaisePropertyChanged();
                    RequiresRestart = true;
                }
            }
        }

        public bool HardwareAcceleration {
            get => NINA.Properties.Settings.Default.HardwareAcceleration;
            set {
                NINA.Properties.Settings.Default.HardwareAcceleration = value;
                CoreUtil.SaveSettings(NINA.Properties.Settings.Default);
                RaisePropertyChanged();
                RequiresRestart = true;
            }
        }

        public bool RequiresRestart {
            get => requiresRestart;
            set {
                requiresRestart = value;
                RaisePropertyChanged();
            }
        }

        public LogLevelEnum LogLevel {
            get => profileService.ActiveProfile.ApplicationSettings.LogLevel;
            set {
                profileService.ActiveProfile.ApplicationSettings.LogLevel = value;
                Logger.SetLogLevel(value);
                RaisePropertyChanged();
            }
        }

        private FilterInfo _selectedFilter;

        public FilterInfo SelectedFilter {
            get => _selectedFilter;
            set {
                _selectedFilter = value;
                RaisePropertyChanged();
            }
        }

        private ProfileMeta _selectedProfile;
        private bool requiresRestart = false;
        private readonly IVersionCheckVM versionCheckVM;
        private readonly ProjectVersion projectVersion;
        private readonly IPlanetariumFactory planetariumFactory;
        private readonly IGnssFactory gnssFactory;

        public ProfileMeta SelectedProfile {
            get => _selectedProfile;
            set {
                _selectedProfile = value;
                RaisePropertyChanged();
            }
        }

        public ICollectionView Profiles { get; }

        public IPluggableBehaviorSelector<IStarDetection> PluggableStarDetection { get; private set; }
        public IPluggableBehaviorSelector<IStarAnnotator> PluggableStarAnnotator { get; private set; }
        public IPluggableBehaviorSelector<IAutoFocusVMFactory> PluggableAutoFocusVMFactory { get; private set; }
    }
}