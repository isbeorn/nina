#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Interfaces;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.MyMessageBox;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Exceptions;
using NINA.Equipment.Interfaces;
using NINA.Image.ImageAnalysis;
using NINA.Plugin;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.InputBox;
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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace NINA.ViewModel {

    internal partial class OptionsVM : DockableVM, IOptionsVM {

        public OptionsVM(IProfileService profileService,
                         IAllDeviceConsumer deviceConsumer,
                         IVersionCheckVM versionCheckVM,
                         ProjectVersion projectVersion,
                         IPlanetariumFactory planetariumFactory,
                         IGnssFactory gnssFactory,
                         IPluggableBehaviorSelector<IStarDetection> starDetectionSelector,
                         IPluggableBehaviorSelector<IStarAnnotator> starAnnotatorSelector,
                         IPluggableBehaviorSelector<IAutoFocusVMFactory> autoFocusVMFactorySelector) : base(profileService) {
            Title = Loc.Instance["LblOptions"];
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["SettingsSVG"];

            DeviceConsumer = deviceConsumer;
            customPatterns = new List<ImagePattern>();
            this.versionCheckVM = versionCheckVM;
            this.projectVersion = projectVersion;
            this.planetariumFactory = planetariumFactory;
            this.gnssFactory = gnssFactory;
            this.PluggableStarDetection = starDetectionSelector;
            this.PluggableStarAnnotator = starAnnotatorSelector;
            this.PluggableAutoFocusVMFactory = autoFocusVMFactorySelector;

            PluginRepositories = new AsyncObservableCollection<string>(CoreUtil.DeserializeList<string>(Properties.Settings.Default.PluginRepositories));
            if (!PluginRepositories.Any(x => x == Constants.MainPluginRepository)) {
                // We enforce that the main plugin repository is always present
                PluginRepositories.Insert(0, Constants.MainPluginRepository);
            }
            CopyToCustomSchemaCommand = new Core.Utility.RelayCommand(CopyToCustomSchema, (object o) => ActiveProfile.ColorSchemaSettings.ColorSchema?.Name != "Custom");
            CopyToAlternativeCustomSchemaCommand = new Core.Utility.RelayCommand(CopyToAlternativeCustomSchema, (object o) => ActiveProfile.ColorSchemaSettings.AltColorSchema?.Name != "Alternative Custom");

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
                    || !string.IsNullOrWhiteSpace(profileService.ActiveProfile.ImageFileSettings.FilePatternFLAT);
            };

            var typeFace = ApplicationFontFamily.FamilyTypefaces.FirstOrDefault(x => x.Weight == FontWeight && x.Style == FontStyle && x.Stretch == FontStretch) ?? ApplicationFontFamily.FamilyTypefaces.FirstOrDefault(); ;
            if (typeFace != null) {
                FamilyTypeface = ApplicationFontFamily.FamilyTypefaces.FirstOrDefault(x => x.Weight == FontWeight && x.Style == FontStyle && x.Stretch == FontStretch);
            }

            Profiles = CollectionViewSource.GetDefaultView(profileService.Profiles);
            Profiles.SortDescriptions.Add(new SortDescription("IsActive", ListSortDirection.Descending));
            Profiles.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            FilePatternsExpanded = !string.IsNullOrWhiteSpace(profileService.ActiveProfile.ImageFileSettings.FilePatternBIAS)
                || !string.IsNullOrWhiteSpace(profileService.ActiveProfile.ImageFileSettings.FilePatternDARK)
                || !string.IsNullOrWhiteSpace(profileService.ActiveProfile.ImageFileSettings.FilePatternFLAT);
        }

        private bool CanCloneProfile => SelectedProfile != null;
        private bool CanRemoveProfile => SelectedProfile != null && SelectedProfile.Id != profileService.ActiveProfile.Id;
        private bool CanSelectProfile => SelectedProfile != null && SelectedProfile.Id != profileService.ActiveProfile.Id;

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

        public AsyncObservableCollection<string> PluginRepositories { get; set; }

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

        [ObservableProperty]
        private bool filePatternsExpanded;

        public string FilePatternPreview => ImagePatterns.GetImageFileString(FilePattern).Replace("\\", " › ");

        public string FilePatternPreviewDARK => ImagePatterns.GetImageFileString(FilePatternDARK, "DARK").Replace("\\", " › ");

        public string FilePatternPreviewBIAS => ImagePatterns.GetImageFileString(FilePatternBIAS, "BIAS").Replace("\\", " › ");

        public string FilePatternPreviewFLAT => ImagePatterns.GetImageFileString(FilePatternFLAT, "FLAT").Replace("\\", " › ");

        private List<ImagePattern> customPatterns;

        [RelayCommand]
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

        [RelayCommand]
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

        public IAllDeviceConsumer DeviceConsumer { get; }

        [RelayCommand]
        private void OpenLogFolder(object obj) {
            var path = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\NINA\Logs");
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        [RelayCommand]
        private void OpenWebRequest(object obj) {
            var url = new Uri(obj.ToString());
            Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
        }

        [RelayCommand(IncludeCancelCommand = true)]
        private Task<bool> SiteFromGnss(CancellationToken token) {
            return Task.Run(async () => {
                IGnss gnss = gnssFactory.GetGnssSource();
                Location loc = null;

                try {
                    loc = await gnss.GetLocation(token);

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
                } catch(OperationCanceledException) {
                } catch (GnssNotFoundException ex) {
                    Logger.Error(ex.Message);
                    Notification.ShowError(string.Format(Loc.Instance["LblGnssNotFound"], gnss.Name));
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(string.Format(Loc.Instance["LblGnssConnectFail"], gnss.Name));
                }

                return (loc != null);
            });
        }
        [RelayCommand(IncludeCancelCommand = true)]
        private Task<bool> SiteFromPlanetarium(CancellationToken token) {
            return Task.Run(async () => {
                IPlanetarium s = planetariumFactory.GetPlanetarium();
                Location loc = null;

                try {
                    loc = await s.GetSite(token);

                    if (loc != null) {
                        Latitude = loc.Latitude;
                        Longitude = loc.Longitude;
                        Elevation = loc.Elevation;
                        Notification.ShowSuccess(string.Format(Loc.Instance["LblPlanetariumCoordsOk"], s.Name));
                    }
                } catch(OperationCanceledException) { 
                } catch (PlanetariumFailedToConnect ex) {
                    Logger.Error($"Unable to connect to {s.Name}", ex);
                    Notification.ShowError(string.Format(Loc.Instance["LblPlanetariumFailedToConnect"], s.Name));
                } catch (Exception ex) {
                    Logger.Error($"Failed to get coordinates from {s.Name}", ex);
                    Notification.ShowError(string.Format(Loc.Instance["LblPlanetariumCoordsError"], s.Name));
                }

                return (loc != null);
            });
        }

        public string Version => projectVersion.ToString();
        private void CopyToCustomSchema(object obj) {
            ActiveProfile.ColorSchemaSettings.CopyToCustom();
        }
        private void CopyToAlternativeCustomSchema(object obj) {
            ActiveProfile.ColorSchemaSettings.CopyToAltCustom();
        }
        [RelayCommand(CanExecute = nameof(CanCloneProfile))]
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
        [RelayCommand(CanExecute = nameof(CanRemoveProfile))]
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
        [RelayCommand(CanExecute = nameof(CanSelectProfile))]
        private void SelectProfile(object obj) {
            if (!profileService.SelectProfile(SelectedProfile)) {
                Notification.ShowWarning(Loc.Instance["LblLoadProfileInUseWarning"]);
                ProfileService.ActivateInstanceOfNinaReferencingProfile(SelectedProfile.Id.ToString());
            }
        }
        [RelayCommand]
        private void AddProfile(object obj) {
            profileService.Add();
        }
        [RelayCommand]
        private void RemoveFilter(object obj) {
            if (obj is FilterInfo filter) {
                var filters = ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                filters.Remove(filter);
                for (short i = 0; i < filters.Count; i++) {
                    filters[i].Position = i;
                }
            }
        }
        [RelayCommand]
        private void AddFilter(object obj) {
            var pos = ActiveProfile.FilterWheelSettings.FilterWheelFilters.Count;
            var filter = new FilterInfo(Loc.Instance["LblFilter"] + (pos + 1), 0, (short)pos, -1, new BinningMode(1, 1), -1, -1);
            ActiveProfile.FilterWheelSettings.FilterWheelFilters.Add(filter);
            SelectedFilter = filter;
        }
        [RelayCommand]
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
        [RelayCommand]
        private void OpenSkySurveyCacheDirectoryDiag(object obj) {
            var dialog = new OpenFolderDialog();
            dialog.InitialDirectory = ActiveProfile.ApplicationSettings.SkySurveyCacheDirectory;

            if (dialog.ShowDialog() == true) {
                ActiveProfile.ApplicationSettings.SkySurveyCacheDirectory = dialog.FolderName;
            }

        }
        [RelayCommand]
        private void DownloadIndexes(object obj) {
            AstrometryIndexDownloader.AstrometryIndexDownloaderVM.Show(ActiveProfile.PlateSolveSettings.CygwinLocation);
            ScanForIndexFiles();
        }

        [RelayCommand]
        private void OpenImageFileDiag(object o) {
            var diag = new OpenFolderDialog();
            diag.FolderName = ActiveProfile.ImageFileSettings.FilePath;
            if (diag.ShowDialog() == true) {
                ActiveProfile.ImageFileSettings.FilePath = diag.FolderName + "\\";
            }
        }
        [RelayCommand]
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

        [RelayCommand]
        private void OpenSequenceFolderDiag(object o) {
            var diag = new OpenFolderDialog();
            diag.InitialDirectory = ActiveProfile.SequenceSettings.DefaultSequenceFolder;
            if (diag.ShowDialog() == true) {
                ActiveProfile.SequenceSettings.DefaultSequenceFolder = diag.FolderName + "\\";
            }

        }
        [RelayCommand]
        private void OpenTargetsFolderDiag(object o) {
            var diag = new OpenFolderDialog();
            diag.InitialDirectory = ActiveProfile.SequenceSettings.SequencerTargetsFolder;
            if (diag.ShowDialog() == true) {
                ActiveProfile.SequenceSettings.SequencerTargetsFolder = diag.FolderName + "\\";
            }

        }
        [RelayCommand]
        private void OpenSequenceTemplateFolderDiag(object o) {
            var diag = new OpenFolderDialog();
            diag.InitialDirectory = ActiveProfile.SequenceSettings.SequencerTemplatesFolder;
            if (diag.ShowDialog() == true) {
                ActiveProfile.SequenceSettings.SequencerTemplatesFolder = diag.FolderName + "\\";

            }
        }
        [RelayCommand]
        private void OpenCygwinFileDiag(object o) {
            var dialog = new OpenFolderDialog();
            dialog.InitialDirectory = profileService.ActiveProfile.PlateSolveSettings.CygwinLocation;

            if(dialog.ShowDialog() == true) {
                ActiveProfile.PlateSolveSettings.CygwinLocation = dialog.FolderName;
            }

        }
        [RelayCommand]
        private void OpenPS2FileDiag(object o) {
            var dialog = GetFilteredFileDialog(profileService.ActiveProfile.PlateSolveSettings.PS2Location, "PlateSolve2.exe", "PlateSolve2|PlateSolve2.exe");
            if (dialog.ShowDialog() == true) {
                ActiveProfile.PlateSolveSettings.PS2Location = dialog.FileName;
            }
        }
        [RelayCommand]
        private void OpenPS3FileDiag(object o) {
            var dialog = GetFilteredFileDialog(profileService.ActiveProfile.PlateSolveSettings.PS3Location, "PlateSolve3.exe", "PlateSolve3|PlateSolve3*.exe");
            if (dialog.ShowDialog() == true) {
                ActiveProfile.PlateSolveSettings.PS3Location = dialog.FileName;
            }
        }
        [RelayCommand]
        private void OpenASPSFileDiag(object o) {
            var dialog = GetFilteredFileDialog(profileService.ActiveProfile.PlateSolveSettings.AspsLocation, "PlateSolver.exe", "ASPS|PlateSolver.exe");
            if (dialog.ShowDialog() == true) {
                ActiveProfile.PlateSolveSettings.AspsLocation = dialog.FileName;
            }
        }
        [RelayCommand]
        private void OpenASTAPFileDiag(object o) {
            var dialog = GetFilteredFileDialog(profileService.ActiveProfile.PlateSolveSettings.ASTAPLocation, "astap.exe", "ASTAP|astap.exe");
            if (dialog.ShowDialog() == true) {
                ActiveProfile.PlateSolveSettings.ASTAPLocation = dialog.FileName;
            }
        }
        [RelayCommand]
        private void OpenPinPointCatalogDiag(object o) {
            var dialog = new OpenFolderDialog();

            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName)) {
                ActiveProfile.PlateSolveSettings.PinPointCatalogRoot = dialog.FolderName;
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

        [ObservableProperty]
        private ObservableCollection<string> _indexFiles = new ObservableCollection<string>();

        [RelayCommand]
        private void AddPluginRepository(object obj) {
            var box = new InputBox(Loc.Instance["LblPluginRepositoryEnterUrl"], "https://<repository url>");
            box.Owner = System.Windows.Application.Current.MainWindow;
            box.Width = 350;
            box.Height = 150;
            box.Show();
            box.Closing += (sender, e) => {
                var d = sender as InputBox;
                if (d.Canceled) {
                    return;
                }
                var url = box.InputText;
                bool isValidUrl = Uri.TryCreate(url, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                if (!isValidUrl) {
                    Notification.ShowError(string.Format(Loc.Instance["LblPluginRepositoryUrlInvalid"], url));
                    Logger.Error($"Plugin Repository Url is invalid: {url}");
                    return;
                }
                try {
                    this.PluginRepositories.Add(url);
                    Properties.Settings.Default.PluginRepositories = CoreUtil.SerializeList<string>(this.PluginRepositories.ToList());
                    CoreUtil.SaveSettings(Properties.Settings.Default);
                } catch (Exception ex) {
                    Notification.ShowError(ex.Message);
                    Logger.Error(ex);
                }
            };
        }

        [RelayCommand]
        private void RemovePluginRepository(object obj) {
            if (obj is string url) {
                try {
                    if (url == Constants.MainPluginRepository) {
                        // Removing the main repository is ignored
                        return;
                    }
                    this.PluginRepositories.Remove(url);
                    Properties.Settings.Default.PluginRepositories = CoreUtil.SerializeList<string>(this.PluginRepositories.ToList());
                    CoreUtil.SaveSettings(Properties.Settings.Default);
                } catch (Exception ex) {
                    Notification.ShowError(ex.Message);
                    Logger.Error(ex);
                }
            }
        }

        public ICommand CopyToCustomSchemaCommand { get; private set; }
        public ICommand CopyToAlternativeCustomSchemaCommand { get; private set; }

        [ObservableProperty]
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
                if (FamilyTypeface != null) {
                    FontStretch = FamilyTypeface.Stretch;
                    FontStyle = FamilyTypeface.Style;
                    FontWeight = FamilyTypeface.Weight;
                }
                RaisePropertyChanged();
            }
        }

        private FamilyTypeface familyTypeface;

        public FamilyTypeface FamilyTypeface {
            get => familyTypeface;
            set {
                familyTypeface = value;
                if(familyTypeface != null) {
                    FontStretch = familyTypeface.Stretch;
                    FontStyle = familyTypeface.Style;
                    FontWeight = familyTypeface.Weight;
                    RaisePropertyChanged();
                }
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
        [RelayCommand]
        private void ToggleColors(object o) {
            ActiveProfile.ColorSchemaSettings.ToggleSchema();
        }

        public static Dc3PoinPointCatalogEnum[] Dc3PoinPointCatalogs => Enum.GetValues(typeof(Dc3PoinPointCatalogEnum))
                    .Cast<Dc3PoinPointCatalogEnum>()
                    .ToArray();

#pragma warning disable CS0612 // Type or member is obsolete

        public static FileTypeEnum[] FileTypes => Enum.GetValues(typeof(FileTypeEnum))
                    .Cast<FileTypeEnum>()
                    .Where(p => p != FileTypeEnum.RAW)
                    .Where(p => p != FileTypeEnum.TIFF_LZW)
                    .Where(p => p != FileTypeEnum.TIFF_ZIP)
                    .ToArray();

#pragma warning restore CS0612 // Type or member is obsolete

        public static TIFFCompressionTypeEnum[] TIFFCompressionTypes => Enum.GetValues(typeof(TIFFCompressionTypeEnum))
                    .Cast<TIFFCompressionTypeEnum>()
                    .ToArray();

        public static XISFCompressionTypeEnum[] XISFCompressionTypes => Enum.GetValues(typeof(XISFCompressionTypeEnum))
                    .Cast<XISFCompressionTypeEnum>()
                    .ToArray();

        public static XISFChecksumTypeEnum[] XISFChecksumTypes =>
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

        public static FITSCompressionTypeEnum[] FITSCompressionTypes => Enum.GetValues(typeof(FITSCompressionTypeEnum))
                   .Cast<FITSCompressionTypeEnum>()
                   .ToArray();

        [ObservableProperty]
        private ImagePatterns _imagePatterns;

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

        public LogLevelEnum LogLevel {
            get => profileService.ActiveProfile.ApplicationSettings.LogLevel;
            set {
                profileService.ActiveProfile.ApplicationSettings.LogLevel = value;
                Logger.SetLogLevel(value);
                RaisePropertyChanged();
            }
        }

        [ObservableProperty]
        private FilterInfo _selectedFilter;

        [ObservableProperty]
        private bool requiresRestart = false;
        private readonly IVersionCheckVM versionCheckVM;
        private readonly ProjectVersion projectVersion;
        private readonly IPlanetariumFactory planetariumFactory;
        private readonly IGnssFactory gnssFactory;

        [NotifyCanExecuteChangedFor(nameof(CloneProfileCommand))]
        [NotifyCanExecuteChangedFor(nameof(RemoveProfileCommand))]
        [NotifyCanExecuteChangedFor(nameof(SelectProfileCommand))]
        [ObservableProperty]
        private ProfileMeta selectedProfile;


        public ICollectionView Profiles { get; }

        public IPluggableBehaviorSelector<IStarDetection> PluggableStarDetection { get; private set; }
        public IPluggableBehaviorSelector<IStarAnnotator> PluggableStarAnnotator { get; private set; }
        public IPluggableBehaviorSelector<IAutoFocusVMFactory> PluggableAutoFocusVMFactory { get; private set; }
    }
}