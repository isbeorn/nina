#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Model.MyFilterWheel;
using NINA.Utility;
using NINA.Utility.Astrometry;
using NINA.Utility.Enum;
using NINA.Utility.Mediator.Interfaces;
using NINA.Utility.Profile;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace NINA.ViewModel {

    internal class OptionsVM : DockableVM {

        public OptionsVM(IProfileService profileService, IFilterWheelMediator filterWheelMediator) : base(profileService) {
            Title = "LblOptions";
            CanClose = false;
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["SettingsSVG"];

            this.filterWheelMediator = filterWheelMediator;

            PreviewFileCommand = new RelayCommand(PreviewFile);
            OpenImageFileDiagCommand = new RelayCommand(OpenImageFileDiag);
            OpenSequenceTemplateDiagCommand = new RelayCommand(OpenSequenceTemplateDiag);
            OpenCygwinFileDiagCommand = new RelayCommand(OpenCygwinFileDiag);
            OpenPS2FileDiagCommand = new RelayCommand(OpenPS2FileDiag);
            OpenASPSFileDiagCommand = new RelayCommand(OpenASPSFileDiag);
            ToggleColorsCommand = new RelayCommand(ToggleColors);
            DownloadIndexesCommand = new RelayCommand(DownloadIndexes);
            OpenSkyAtlasImageRepositoryDiagCommand = new RelayCommand(OpenSkyAtlasImageRepositoryDiag);
            OpenSkySurveyCacheDirectoryDiagCommand = new RelayCommand(OpenSkySurveyCacheDirectoryDiag);
            ImportFiltersCommand = new RelayCommand(ImportFilters);
            AddFilterCommand = new RelayCommand(AddFilter);
            RemoveFilterCommand = new RelayCommand(RemoveFilter);
            AddProfileCommand = new RelayCommand(AddProfile);
            CloneProfileCommand = new RelayCommand(CloneProfile, (object o) => { return SelectedProfile != null; });
            RemoveProfileCommand = new RelayCommand(RemoveProfile, (object o) => { return SelectedProfile != null && SelectedProfile.Id != profileService.ActiveProfile.Id; });
            SelectProfileCommand = new RelayCommand(SelectProfile, (o) => {
                return SelectedProfile != null;
            });

            CopyToCustomSchemaCommand = new RelayCommand(CopyToCustomSchema, (object o) => AlternativeColorSchemaName != "Custom");
            CopyToAlternativeCustomSchemaCommand = new RelayCommand(CopyToAlternativeCustomSchema, (object o) => ColorSchemaName != "Alternative Custom");

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

            FilterWheelFilters.CollectionChanged += FilterWheelFilters_CollectionChanged;
        }

        private void CopyToAlternativeCustomSchema(object obj) {
            var schema = ColorSchemas.Items.Where((x) => x.Name == "Alternative Custom").First();

            schema.PrimaryColor = AltPrimaryColor;
            schema.SecondaryColor = AltSecondaryColor;
            schema.BorderColor = AltBorderColor;
            schema.BackgroundColor = AltBackgroundColor;
            schema.SecondaryBackgroundColor = AltSecondaryBackgroundColor;
            schema.TertiaryBackgroundColor = AltTertiaryBackgroundColor;
            schema.ButtonBackgroundColor = AltButtonBackgroundColor;
            schema.ButtonBackgroundSelectedColor = AltButtonBackgroundSelectedColor;
            schema.ButtonForegroundColor = AltButtonForegroundColor;
            schema.ButtonForegroundDisabledColor = AltButtonForegroundDisabledColor;
            schema.NotificationWarningColor = AltNotificationWarningColor;
            schema.NotificationWarningTextColor = AltNotificationWarningTextColor;
            schema.NotificationErrorColor = AltNotificationErrorColor;
            schema.NotificationErrorTextColor = AltNotificationErrorTextColor;
            AlternativeColorSchemaName = schema.Name;
        }

        private void CopyToCustomSchema(object obj) {
            var schema = ColorSchemas.Items.Where((x) => x.Name == "Custom").First();

            schema.PrimaryColor = PrimaryColor;
            schema.SecondaryColor = SecondaryColor;
            schema.BorderColor = BorderColor;
            schema.BackgroundColor = BackgroundColor;
            schema.SecondaryBackgroundColor = SecondaryBackgroundColor;
            schema.TertiaryBackgroundColor = TertiaryBackgroundColor;
            schema.ButtonBackgroundColor = ButtonBackgroundColor;
            schema.ButtonBackgroundSelectedColor = ButtonBackgroundSelectedColor;
            schema.ButtonForegroundColor = ButtonForegroundColor;
            schema.ButtonForegroundDisabledColor = ButtonForegroundDisabledColor;
            schema.NotificationWarningColor = NotificationWarningColor;
            schema.NotificationWarningTextColor = NotificationWarningTextColor;
            schema.NotificationErrorColor = NotificationErrorColor;
            schema.NotificationErrorTextColor = NotificationErrorTextColor;

            ColorSchemaName = schema.Name;
        }

        private void CloneProfile(object obj) {
            profileService.Clone(SelectedProfile.Id);
        }

        private void RemoveProfile(object obj) {
            if (MyMessageBox.MyMessageBox.Show(string.Format(Locale.Loc.Instance["LblRemoveProfileText"], SelectedProfile?.Name, SelectedProfile?.Id), Locale.Loc.Instance["LblRemoveProfileCaption"], System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes) {
                profileService.RemoveProfile(SelectedProfile.Id);
            }
        }

        private void ProfileChanged() {
            RaisePropertyChanged(nameof(ColorSchemaName));
            RaisePropertyChanged(nameof(PrimaryColor));
            RaisePropertyChanged(nameof(SecondaryColor));
            RaisePropertyChanged(nameof(BorderColor));
            RaisePropertyChanged(nameof(BackgroundColor));
            RaisePropertyChanged(nameof(SecondaryBackgroundColor));
            RaisePropertyChanged(nameof(TertiaryBackgroundColor));
            RaisePropertyChanged(nameof(ButtonBackgroundColor));
            RaisePropertyChanged(nameof(ButtonBackgroundSelectedColor));
            RaisePropertyChanged(nameof(ButtonForegroundColor));
            RaisePropertyChanged(nameof(ButtonForegroundDisabledColor));
            RaisePropertyChanged(nameof(NotificationWarningColor));
            RaisePropertyChanged(nameof(NotificationErrorColor));
            RaisePropertyChanged(nameof(NotificationWarningTextColor));
            RaisePropertyChanged(nameof(NotificationErrorTextColor));
            RaisePropertyChanged(nameof(AlternativeColorSchemaName));
            RaisePropertyChanged(nameof(AltPrimaryColor));
            RaisePropertyChanged(nameof(AltSecondaryColor));
            RaisePropertyChanged(nameof(AltBorderColor));
            RaisePropertyChanged(nameof(AltBackgroundColor));
            RaisePropertyChanged(nameof(AltSecondaryBackgroundColor));
            RaisePropertyChanged(nameof(AltTertiaryBackgroundColor));
            RaisePropertyChanged(nameof(AltButtonBackgroundColor));
            RaisePropertyChanged(nameof(AltButtonBackgroundSelectedColor));
            RaisePropertyChanged(nameof(AltButtonForegroundColor));
            RaisePropertyChanged(nameof(AltButtonForegroundDisabledColor));
            RaisePropertyChanged(nameof(AltNotificationWarningColor));
            RaisePropertyChanged(nameof(AltNotificationErrorColor));
            RaisePropertyChanged(nameof(AltNotificationErrorTextColor));
            RaisePropertyChanged(nameof(AltNotificationWarningTextColor));
            //RaisePropertyChanged(nameof(ColorSchemas));
            //RaiseAllPropertiesChanged();
            foreach (System.Reflection.PropertyInfo p in this.GetType().GetProperties()) {
                if (!p.Name.ToLower().Contains("color")) {
                    RaisePropertyChanged(p.Name);
                }
            }
        }

        private void SelectProfile(object obj) {
            profileService.SelectProfile(SelectedProfile.Id);
            ProfileChanged();
        }

        private void AddProfile(object obj) {
            profileService.Add();
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
            var filters = filterWheelMediator.GetAllFilters();
            if (filters?.Count > 0) {
                FilterWheelFilters.Clear();
                FilterWheelFilters.CollectionChanged -= FilterWheelFilters_CollectionChanged;
                var l = filters.OrderBy(x => x.Position);
                foreach (var filter in l) {
                    FilterWheelFilters.Add(filter);
                }
                FilterWheelFilters.CollectionChanged += FilterWheelFilters_CollectionChanged;
            }
        }

        private void FilterWheelFilters_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            FilterWheelFilters = FilterWheelFilters;
        }

        private void OpenSkyAtlasImageRepositoryDiag(object obj) {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                SkyAtlasImageRepository = dialog.SelectedPath;
            }
        }

        private void OpenSkySurveyCacheDirectoryDiag(object obj) {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = profileService.ActiveProfile.ApplicationSettings.SkySurveyCacheDirectory;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                SkySurveyCacheDirectory = dialog.SelectedPath;
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
            dialog.SelectedPath = profileService.ActiveProfile.PlateSolveSettings.CygwinLocation;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                CygwinLocation = dialog.SelectedPath;
            }
        }

        private void OpenPS2FileDiag(object o) {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = profileService.ActiveProfile.PlateSolveSettings.PS2Location;

            if (dialog.ShowDialog() == true) {
                PS2Location = dialog.FileName;
            }
        }

        private void OpenASPSFileDiag(object o) {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = profileService.ActiveProfile.PlateSolveSettings.AspsLocation;

            if (dialog.ShowDialog() == true) {
                AspsLocation = dialog.FileName;
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

        public ICommand OpenASPSFileDiagCommand { get; private set; }

        public ICommand OpenImageFileDiagCommand { get; private set; }

        public ICommand OpenSequenceTemplateDiagCommand { get; private set; }

        public ICommand PreviewFileCommand { get; private set; }

        public ICommand ToggleColorsCommand { get; private set; }

        public ICommand OpenSkyAtlasImageRepositoryDiagCommand { get; private set; }
        public ICommand OpenSkySurveyCacheDirectoryDiagCommand { get; private set; }

        public ICommand ImportFiltersCommand { get; private set; }

        public ICommand AddFilterCommand { get; private set; }

        public ICommand RemoveFilterCommand { get; private set; }

        public ICommand AddProfileCommand { get; private set; }
        public ICommand CloneProfileCommand { get; private set; }
        public ICommand RemoveProfileCommand { get; private set; }

        public ICommand CopyToCustomSchemaCommand { get; private set; }
        public ICommand CopyToAlternativeCustomSchemaCommand { get; private set; }

        public ICommand SelectProfileCommand { get; private set; }

        private void PreviewFile(object o) {
            MyMessageBox.MyMessageBox.Show(ImagePatterns.GetImageFileString(profileService.ActiveProfile.ImageFileSettings.FilePattern), Locale.Loc.Instance["LblFileExample"], System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxResult.OK);
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
                return profileService.ActiveProfile.ApplicationSettings.Language;
            }
            set {
                profileService.ChangeLocale(value);
                RaisePropertyChanged();
            }
        }

        public double ReadNoise {
            get {
                return profileService.ActiveProfile.CameraSettings.ReadNoise;
            }
            set {
                profileService.ActiveProfile.CameraSettings.ReadNoise = value;
                RaisePropertyChanged();
            }
        }

        public double BitDepth {
            get {
                return profileService.ActiveProfile.CameraSettings.BitDepth;
            }
            set {
                profileService.ActiveProfile.CameraSettings.BitDepth = value;
                RaisePropertyChanged();
            }
        }

        public double Offset {
            get {
                return profileService.ActiveProfile.CameraSettings.Offset;
            }
            set {
                profileService.ActiveProfile.CameraSettings.Offset = value;
                RaisePropertyChanged();
            }
        }

        public double FullWellCapacity {
            get {
                return profileService.ActiveProfile.CameraSettings.FullWellCapacity;
            }
            set {
                profileService.ActiveProfile.CameraSettings.FullWellCapacity = value;
                RaisePropertyChanged();
            }
        }

        public double DownloadToDataRatio {
            get {
                return profileService.ActiveProfile.CameraSettings.DownloadToDataRatio;
            }
            set {
                profileService.ActiveProfile.CameraSettings.DownloadToDataRatio = value;
                RaisePropertyChanged();
            }
        }

        public string ImageFilePath {
            get {
                return profileService.ActiveProfile.ImageFileSettings.FilePath;
            }
            set {
                profileService.ActiveProfile.ImageFileSettings.FilePath = value;
                RaisePropertyChanged();
            }
        }

        public string SequenceTemplatePath {
            get {
                return profileService.ActiveProfile.SequenceSettings.TemplatePath;
            }
            set {
                profileService.ActiveProfile.SequenceSettings.TemplatePath = value;
                RaisePropertyChanged();
            }
        }

        public string ImageFilePattern {
            get {
                return profileService.ActiveProfile.ImageFileSettings.FilePattern;
            }
            set {
                profileService.ActiveProfile.ImageFileSettings.FilePattern = value;
                RaisePropertyChanged();
            }
        }

        public string PHD2ServerUrl {
            get {
                return profileService.ActiveProfile.GuiderSettings.PHD2ServerUrl;
            }
            set {
                profileService.ActiveProfile.GuiderSettings.PHD2ServerUrl = value;
                RaisePropertyChanged();
            }
        }

        public double AutoStretchFactor {
            get {
                return profileService.ActiveProfile.ImageSettings.AutoStretchFactor;
            }
            set {
                profileService.ActiveProfile.ImageSettings.AutoStretchFactor = value;
                RaisePropertyChanged();
            }
        }

        public double BlackClipping {
            get {
                return profileService.ActiveProfile.ImageSettings.BlackClipping;
            }
            set {
                profileService.ActiveProfile.ImageSettings.BlackClipping = value;
                RaisePropertyChanged();
            }
        }

        public bool AnnotateImage {
            get {
                return profileService.ActiveProfile.ImageSettings.AnnotateImage;
            }
            set {
                profileService.ActiveProfile.ImageSettings.AnnotateImage = value;
                RaisePropertyChanged();
            }
        }

        public bool DebayerImage {
            get {
                return profileService.ActiveProfile.ImageSettings.DebayerImage;
            }
            set {
                profileService.ActiveProfile.ImageSettings.DebayerImage = value;
                RaisePropertyChanged();
            }
        }

        public PlateSolverEnum PlateSolverType {
            get {
                return profileService.ActiveProfile.PlateSolveSettings.PlateSolverType;
            }
            set {
                profileService.ActiveProfile.PlateSolveSettings.PlateSolverType = value;
                RaisePropertyChanged();
            }
        }

        public BlindSolverEnum BlindSolverType {
            get {
                return profileService.ActiveProfile.PlateSolveSettings.BlindSolverType;
            }
            set {
                profileService.ActiveProfile.PlateSolveSettings.BlindSolverType = value;
                RaisePropertyChanged();
            }
        }

        public Epoch EpochType {
            get {
                return profileService.ActiveProfile.AstrometrySettings.EpochType;
            }
            set {
                profileService.ActiveProfile.AstrometrySettings.EpochType = value;
                RaisePropertyChanged();
            }
        }

        public Hemisphere HemisphereType {
            get {
                return profileService.ActiveProfile.AstrometrySettings.HemisphereType;
            }
            set {
                profileService.ChangeHemisphere(value);

                RaisePropertyChanged();
                Latitude = Latitude;
            }
        }

        public string CygwinLocation {
            get {
                return profileService.ActiveProfile.PlateSolveSettings.CygwinLocation;
            }
            set {
                profileService.ActiveProfile.PlateSolveSettings.CygwinLocation = value;
                ScanForIndexFiles();
                RaisePropertyChanged();
            }
        }

        public string PS2Location {
            get {
                return profileService.ActiveProfile.PlateSolveSettings.PS2Location;
            }
            set {
                profileService.ActiveProfile.PlateSolveSettings.PS2Location = value;
                RaisePropertyChanged();
            }
        }

        public string AspsLocation {
            get {
                return profileService.ActiveProfile.PlateSolveSettings.AspsLocation;
            }
            set {
                profileService.ActiveProfile.PlateSolveSettings.AspsLocation = value;
                RaisePropertyChanged();
            }
        }

        public double AnsvrSearchRadius {
            get {
                return profileService.ActiveProfile.PlateSolveSettings.SearchRadius;
            }
            set {
                profileService.ActiveProfile.PlateSolveSettings.SearchRadius = value;
                RaisePropertyChanged();
            }
        }

        public int PS2Regions {
            get {
                return profileService.ActiveProfile.PlateSolveSettings.Regions;
            }
            set {
                profileService.ActiveProfile.PlateSolveSettings.Regions = value;
                RaisePropertyChanged();
            }
        }

        public double PlateSolveExposureTime {
            get {
                return profileService.ActiveProfile.PlateSolveSettings.ExposureTime;
            }
            set {
                profileService.ActiveProfile.PlateSolveSettings.ExposureTime = value;
                RaisePropertyChanged();
            }
        }

        public FilterInfo PlateSolveFilter {
            get {
                return profileService.ActiveProfile.PlateSolveSettings.Filter;
            }
            set {
                profileService.ActiveProfile.PlateSolveSettings.Filter = value;
                RaisePropertyChanged();
            }
        }

        public double PlateSolveThreshold {
            get {
                return profileService.ActiveProfile.PlateSolveSettings.Threshold;
            }
            set {
                profileService.ActiveProfile.PlateSolveSettings.Threshold = value;
                RaisePropertyChanged();
            }
        }

        public double PlateSolveRotationTolerance {
            get {
                return profileService.ActiveProfile.PlateSolveSettings.RotationTolerance;
            }
            set {
                profileService.ActiveProfile.PlateSolveSettings.RotationTolerance = value;
                RaisePropertyChanged();
            }
        }

        public WeatherDataEnum WeatherDataType {
            get {
                return profileService.ActiveProfile.WeatherDataSettings.WeatherDataType;
            }
            set {
                profileService.ActiveProfile.WeatherDataSettings.WeatherDataType = value;
                RaisePropertyChanged();
            }
        }

        public string OpenWeatherMapAPIKey {
            get {
                return profileService.ActiveProfile.WeatherDataSettings.OpenWeatherMapAPIKey;
            }
            set {
                profileService.ActiveProfile.WeatherDataSettings.OpenWeatherMapAPIKey = value;
                RaisePropertyChanged();
            }
        }

        public string OpenWeatherMapUrl {
            get {
                return profileService.ActiveProfile.WeatherDataSettings.OpenWeatherMapUrl;
            }
            set {
                profileService.ActiveProfile.WeatherDataSettings.OpenWeatherMapUrl = value;
                RaisePropertyChanged();
            }
        }

        public string AstrometryAPIKey {
            get {
                return profileService.ActiveProfile.PlateSolveSettings.AstrometryAPIKey;
            }
            set {
                profileService.ActiveProfile.PlateSolveSettings.AstrometryAPIKey = value;
                RaisePropertyChanged();
            }
        }

        public int PHD2ServerPort {
            get {
                return profileService.ActiveProfile.GuiderSettings.PHD2ServerPort;
            }
            set {
                profileService.ActiveProfile.GuiderSettings.PHD2ServerPort = value;
                RaisePropertyChanged();
            }
        }

        private void ToggleColors(object o) {
            var tmpSchemaName = ColorSchemaName;
            var tmpPrimaryColor = PrimaryColor;
            var tmpSecondaryColor = SecondaryColor;
            var tmpBorderColor = BorderColor;
            var tmpBackgroundColor = BackgroundColor;
            var tmpSecondaryBackgroundColor = SecondaryBackgroundColor;
            var tmpTertiaryBackgroundColor = TertiaryBackgroundColor;
            var tmpButtonBackgroundColor = ButtonBackgroundColor;
            var tmpButtonBackgroundSelectedColor = ButtonBackgroundSelectedColor;
            var tmpButtonForegroundColor = ButtonForegroundColor;
            var tmpButtonForegroundDisabledColor = ButtonForegroundDisabledColor;
            var tmpNotificationWarningColor = NotificationWarningColor;
            var tmpNotificationWarningTextColor = NotificationWarningTextColor;
            var tmpNotificationErrorColor = NotificationErrorColor;
            var tmpNotificationErrorTextColor = NotificationErrorTextColor;

            ColorSchemaName = AlternativeColorSchemaName;
            PrimaryColor = AltPrimaryColor;
            SecondaryColor = AltSecondaryColor;
            BorderColor = AltBorderColor;
            BackgroundColor = AltBackgroundColor;
            SecondaryBackgroundColor = AltSecondaryBackgroundColor;
            TertiaryBackgroundColor = AltTertiaryBackgroundColor;
            ButtonBackgroundColor = AltButtonBackgroundColor;
            ButtonBackgroundSelectedColor = AltButtonBackgroundSelectedColor;
            ButtonForegroundColor = AltButtonForegroundColor;
            ButtonForegroundDisabledColor = AltButtonForegroundDisabledColor;
            NotificationWarningColor = AltNotificationWarningColor;
            NotificationWarningTextColor = AltNotificationWarningTextColor;
            NotificationErrorColor = AltNotificationErrorColor;
            NotificationErrorTextColor = AltNotificationErrorTextColor;

            AlternativeColorSchemaName = tmpSchemaName;
            AltPrimaryColor = tmpPrimaryColor;
            AltSecondaryColor = tmpSecondaryColor;
            AltBorderColor = tmpBorderColor;
            AltBackgroundColor = tmpBackgroundColor;
            AltSecondaryBackgroundColor = tmpSecondaryBackgroundColor;
            AltTertiaryBackgroundColor = tmpTertiaryBackgroundColor;
            AltButtonBackgroundColor = tmpButtonBackgroundColor;
            AltButtonBackgroundSelectedColor = tmpButtonBackgroundSelectedColor;
            AltButtonForegroundColor = tmpButtonForegroundColor;
            AltButtonForegroundDisabledColor = tmpButtonForegroundDisabledColor;
            AltNotificationWarningColor = tmpNotificationWarningColor;
            AltNotificationWarningTextColor = tmpNotificationWarningTextColor;
            AltNotificationErrorColor = tmpNotificationErrorColor;
            AltNotificationErrorTextColor = tmpNotificationErrorTextColor;
        }

        public string ColorSchemaName {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.ColorSchemaName;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.ColorSchemaName = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(PrimaryColor));
                RaisePropertyChanged(nameof(SecondaryColor));
                RaisePropertyChanged(nameof(BorderColor));
                RaisePropertyChanged(nameof(BackgroundColor));
                RaisePropertyChanged(nameof(SecondaryBackgroundColor));
                RaisePropertyChanged(nameof(TertiaryBackgroundColor));
                RaisePropertyChanged(nameof(ButtonBackgroundColor));
                RaisePropertyChanged(nameof(ButtonBackgroundSelectedColor));
                RaisePropertyChanged(nameof(ButtonForegroundColor));
                RaisePropertyChanged(nameof(ButtonForegroundDisabledColor));
                RaisePropertyChanged(nameof(NotificationWarningColor));
                RaisePropertyChanged(nameof(NotificationErrorColor));
                RaisePropertyChanged(nameof(NotificationWarningTextColor));
                RaisePropertyChanged(nameof(NotificationErrorTextColor));
            }
        }

        public ColorSchemas ColorSchemas {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.ColorSchemas;
            }
        }

        public string AlternativeColorSchemaName {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.AltColorSchemaName;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.AltColorSchemaName = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(AltPrimaryColor));
                RaisePropertyChanged(nameof(AltSecondaryColor));
                RaisePropertyChanged(nameof(AltBorderColor));
                RaisePropertyChanged(nameof(AltBackgroundColor));
                RaisePropertyChanged(nameof(AltSecondaryBackgroundColor));
                RaisePropertyChanged(nameof(AltTertiaryBackgroundColor));
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
                return profileService.ActiveProfile.ColorSchemaSettings.PrimaryColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.PrimaryColor = value;
                RaisePropertyChanged();
            }
        }

        public Color SecondaryColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.SecondaryColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.SecondaryColor = value;
                RaisePropertyChanged();
            }
        }

        public Color BorderColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.BorderColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.BorderColor = value;
                RaisePropertyChanged();
            }
        }

        public Color BackgroundColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.BackgroundColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.BackgroundColor = value;
                RaisePropertyChanged();
            }
        }

        public Color SecondaryBackgroundColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.SecondaryBackgroundColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.SecondaryBackgroundColor = value;
                RaisePropertyChanged();
            }
        }

        public Color TertiaryBackgroundColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.TertiaryBackgroundColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.TertiaryBackgroundColor = value;
                RaisePropertyChanged();
            }
        }

        public FileTypeEnum[] FileTypes {
            get {
                return Enum.GetValues(typeof(FileTypeEnum))
                    .Cast<FileTypeEnum>()
                    .Where(p => p != FileTypeEnum.RAW)
                    .ToArray<FileTypeEnum>();
            }
        }

        public FileTypeEnum FileType {
            get {
                return profileService.ActiveProfile.ImageFileSettings.FileType;
            }
            set {
                profileService.ActiveProfile.ImageFileSettings.FileType = value;
                RaisePropertyChanged();
            }
        }

        public Color ButtonBackgroundColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.ButtonBackgroundColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.ButtonBackgroundColor = value;
                RaisePropertyChanged();
            }
        }

        public Color ButtonBackgroundSelectedColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.ButtonBackgroundSelectedColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.ButtonBackgroundSelectedColor = value;
                RaisePropertyChanged();
            }
        }

        public Color ButtonForegroundColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.ButtonForegroundColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.ButtonForegroundColor = value;
                RaisePropertyChanged();
            }
        }

        public Color ButtonForegroundDisabledColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.ButtonForegroundDisabledColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.ButtonForegroundDisabledColor = value;
                RaisePropertyChanged();
            }
        }

        public double DitherPixels {
            get {
                return profileService.ActiveProfile.GuiderSettings.DitherPixels;
            }
            set {
                profileService.ActiveProfile.GuiderSettings.DitherPixels = value;
                RaisePropertyChanged();
            }
        }

        public bool DitherRAOnly {
            get {
                return profileService.ActiveProfile.GuiderSettings.DitherRAOnly;
            }
            set {
                profileService.ActiveProfile.GuiderSettings.DitherRAOnly = value;
                RaisePropertyChanged();
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

        public Color AltPrimaryColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.AltPrimaryColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.AltPrimaryColor = value;
                RaisePropertyChanged();
            }
        }

        public Color AltSecondaryColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.AltSecondaryColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.AltSecondaryColor = value;
                RaisePropertyChanged();
            }
        }

        public Color AltBorderColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.AltBorderColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.AltBorderColor = value;
                RaisePropertyChanged();
            }
        }

        public Color AltBackgroundColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.AltBackgroundColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.AltBackgroundColor = value;
                RaisePropertyChanged();
            }
        }

        public Color AltSecondaryBackgroundColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.AltSecondaryBackgroundColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.AltSecondaryBackgroundColor = value;
                RaisePropertyChanged();
            }
        }

        public Color AltTertiaryBackgroundColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.AltTertiaryBackgroundColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.AltTertiaryBackgroundColor = value;
                RaisePropertyChanged();
            }
        }

        public Color AltButtonBackgroundColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.AltButtonBackgroundColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.AltButtonBackgroundColor = value;
                RaisePropertyChanged();
            }
        }

        public Color AltButtonBackgroundSelectedColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.AltButtonBackgroundSelectedColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.AltButtonBackgroundSelectedColor = value;
                RaisePropertyChanged();
            }
        }

        public Color AltButtonForegroundColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.AltButtonForegroundColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.AltButtonForegroundColor = value;
                RaisePropertyChanged();
            }
        }

        public Color AltButtonForegroundDisabledColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.AltButtonForegroundDisabledColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.AltButtonForegroundDisabledColor = value;
                RaisePropertyChanged();
            }
        }

        public bool AutoMeridianFlip {
            get {
                return profileService.ActiveProfile.MeridianFlipSettings.Enabled;
            }
            set {
                profileService.ActiveProfile.MeridianFlipSettings.Enabled = value;
                RaisePropertyChanged();
            }
        }

        public double MinutesAfterMeridian {
            get {
                return profileService.ActiveProfile.MeridianFlipSettings.MinutesAfterMeridian;
            }
            set {
                profileService.ActiveProfile.MeridianFlipSettings.MinutesAfterMeridian = value;
                RaisePropertyChanged();
            }
        }

        public double PauseTimeBeforeMeridian {
            get {
                return profileService.ActiveProfile.MeridianFlipSettings.PauseTimeBeforeMeridian;
            }
            set {
                profileService.ActiveProfile.MeridianFlipSettings.PauseTimeBeforeMeridian = value;
                RaisePropertyChanged();
            }
        }

        public int MeridianFlipSettleTime {
            get {
                return profileService.ActiveProfile.MeridianFlipSettings.SettleTime;
            }
            set {
                profileService.ActiveProfile.MeridianFlipSettings.SettleTime = value;
                RaisePropertyChanged();
            }
        }

        public bool RecenterAfterFlip {
            get {
                return profileService.ActiveProfile.MeridianFlipSettings.Recenter;
            }
            set {
                profileService.ActiveProfile.MeridianFlipSettings.Recenter = value;
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

        public string SkyAtlasImageRepository {
            get {
                return profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository;
            }
            set {
                profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository = value;
                RaisePropertyChanged();
            }
        }

        public string SkySurveyCacheDirectory {
            get {
                return profileService.ActiveProfile.ApplicationSettings.SkySurveyCacheDirectory;
            }
            set {
                profileService.ActiveProfile.ApplicationSettings.SkySurveyCacheDirectory = value;
                RaisePropertyChanged();
            }
        }

        public Color NotificationWarningColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.NotificationWarningColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.NotificationWarningColor = value;
                RaisePropertyChanged();
            }
        }

        public Color NotificationErrorColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.NotificationErrorColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.NotificationErrorColor = value;
                RaisePropertyChanged();
            }
        }

        public Color NotificationWarningTextColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.NotificationWarningTextColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.NotificationWarningTextColor = value;
                RaisePropertyChanged();
            }
        }

        public Color NotificationErrorTextColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.NotificationErrorTextColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.NotificationErrorTextColor = value;
                RaisePropertyChanged();
            }
        }

        public Color AltNotificationWarningColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.AltNotificationWarningColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.AltNotificationWarningColor = value;
                RaisePropertyChanged();
            }
        }

        public Color AltNotificationErrorColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.AltNotificationErrorColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.AltNotificationErrorColor = value;
                RaisePropertyChanged();
            }
        }

        public Color AltNotificationWarningTextColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.AltNotificationWarningTextColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.AltNotificationWarningTextColor = value;
                RaisePropertyChanged();
            }
        }

        public Color AltNotificationErrorTextColor {
            get {
                return profileService.ActiveProfile.ColorSchemaSettings.AltNotificationErrorTextColor;
            }
            set {
                profileService.ActiveProfile.ColorSchemaSettings.AltNotificationErrorTextColor = value;
                RaisePropertyChanged();
            }
        }

        public bool FocuserUseFilterWheelOffsets {
            get {
                return profileService.ActiveProfile.FocuserSettings.UseFilterWheelOffsets;
            }
            set {
                profileService.ActiveProfile.FocuserSettings.UseFilterWheelOffsets = value;
                RaisePropertyChanged();
            }
        }

        public int FocuserAutoFocusInitialOffsetSteps {
            get {
                return profileService.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps;
            }
            set {
                profileService.ActiveProfile.FocuserSettings.AutoFocusInitialOffsetSteps = value;
                RaisePropertyChanged();
            }
        }

        public int FocuserAutoFocusStepSize {
            get {
                return profileService.ActiveProfile.FocuserSettings.AutoFocusStepSize;
            }
            set {
                profileService.ActiveProfile.FocuserSettings.AutoFocusStepSize = value;
                RaisePropertyChanged();
            }
        }

        public int FocuserAutoFocusExposureTime {
            get {
                return profileService.ActiveProfile.FocuserSettings.AutoFocusExposureTime;
            }
            set {
                profileService.ActiveProfile.FocuserSettings.AutoFocusExposureTime = value;
                RaisePropertyChanged();
            }
        }

        public string TelescopeSnapPortStart {
            get {
                return profileService.ActiveProfile.TelescopeSettings.SnapPortStart;
            }
            set {
                profileService.ActiveProfile.TelescopeSettings.SnapPortStart = value;
                RaisePropertyChanged();
            }
        }

        public string TelescopeSnapPortStop {
            get {
                return profileService.ActiveProfile.TelescopeSettings.SnapPortStop;
            }
            set {
                profileService.ActiveProfile.TelescopeSettings.SnapPortStop = value;
                RaisePropertyChanged();
            }
        }

        public int TelescopeSettleTime {
            get {
                return profileService.ActiveProfile.TelescopeSettings.SettleTime;
            }
            set {
                profileService.ActiveProfile.TelescopeSettings.SettleTime = value;
                RaisePropertyChanged();
            }
        }

        public double DevicePollingInterval {
            get {
                return profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval;
            }
            set {
                if (value > 0) {
                    profileService.ActiveProfile.ApplicationSettings.DevicePollingInterval = value;
                    RaisePropertyChanged();
                }
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

        public CameraBulbModeEnum CameraBulbMode {
            get {
                return profileService.ActiveProfile.CameraSettings.BulbMode;
            }
            set {
                profileService.ActiveProfile.CameraSettings.BulbMode = value;
                RaisePropertyChanged();
            }
        }

        public string CameraSerialPort {
            get {
                return profileService.ActiveProfile.CameraSettings.SerialPort;
            }
            set {
                profileService.ActiveProfile.CameraSettings.SerialPort = value;
                RaisePropertyChanged();
            }
        }

        public double CameraPixelSize {
            get {
                return profileService.ActiveProfile.CameraSettings.PixelSize;
            }
            set {
                profileService.ActiveProfile.CameraSettings.PixelSize = value;
                RaisePropertyChanged();
            }
        }

        public int TelescopeFocalLength {
            get {
                return profileService.ActiveProfile.TelescopeSettings.FocalLength;
            }
            set {
                profileService.ActiveProfile.TelescopeSettings.FocalLength = value;
                RaisePropertyChanged();
            }
        }

        public ObserveAllCollection<FilterInfo> FilterWheelFilters {
            get {
                return profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
            }
            set {
                profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters = value;
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

        public RawConverterEnum RawConverter {
            get {
                return profileService.ActiveProfile.CameraSettings.RawConverter;
            }
            set {
                profileService.ActiveProfile.CameraSettings.RawConverter = value;
                RaisePropertyChanged();
            }
        }

        public int HistogramResolution {
            get {
                return profileService.ActiveProfile.ImageSettings.HistogramResolution;
            }
            set {
                profileService.ActiveProfile.ImageSettings.HistogramResolution = value;
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
                return profileService.ActiveProfile.GuiderSettings.SettleTime;
            }
            set {
                profileService.ActiveProfile.GuiderSettings.SettleTime = value;
                RaisePropertyChanged();
            }
        }

        public double GuiderSettlePixels {
            get {
                return profileService.ActiveProfile.GuiderSettings.SettlePixels;
            }
            set {
                profileService.ActiveProfile.GuiderSettings.SettlePixels = value;
                RaisePropertyChanged();
            }
        }

        public int GuiderSettleTimeout {
            get {
                return profileService.ActiveProfile.GuiderSettings.SettleTimeout;
            }
            set {
                profileService.ActiveProfile.GuiderSettings.SettleTimeout = value;
                RaisePropertyChanged();
            }
        }

        private IProfile _selectedProfile;
        private IFilterWheelMediator filterWheelMediator;

        public IProfile SelectedProfile {
            get {
                return _selectedProfile;
            }
            set {
                _selectedProfile = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<IProfile> Profiles {
            get {
                return profileService.Profiles.ProfileList;
            }
        }
    }
}