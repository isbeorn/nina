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
using System.Collections.ObjectModel;
using System.Diagnostics;
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

            OpenWebRequestCommand = new RelayCommand(OpenWebRequest);
            PreviewFileCommand = new RelayCommand(PreviewFile);
            OpenImageFileDiagCommand = new RelayCommand(OpenImageFileDiag);
            OpenSequenceTemplateDiagCommand = new RelayCommand(OpenSequenceTemplateDiag);
            OpenCygwinFileDiagCommand = new RelayCommand(OpenCygwinFileDiag);
            OpenPS2FileDiagCommand = new RelayCommand(OpenPS2FileDiag);
            OpenPHD2DiagCommand = new RelayCommand(OpenPHD2FileDiag);
            OpenASPSFileDiagCommand = new RelayCommand(OpenASPSFileDiag);
            OpenASTAPFileDiagCommand = new RelayCommand(OpenASTAPFileDiag);
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
        }

        private void OpenWebRequest(object obj) {
            var url = new Uri(obj.ToString());
            Process.Start(new ProcessStartInfo(url.AbsoluteUri));
        }

        public RelayCommand OpenPHD2DiagCommand { get; set; }

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

        public IProfile ActiveProfile {
            get {
                return profileService.ActiveProfile;
            }
        }

        private void ProfileChanged() {
            RaisePropertyChanged(nameof(ActiveProfile));
            RaisePropertyChanged(nameof(IndexFiles));

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
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = ActiveProfile.ApplicationSettings.SkyAtlasImageRepository;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                ActiveProfile.ApplicationSettings.SkyAtlasImageRepository = dialog.SelectedPath;
            }
        }

        private void OpenSkySurveyCacheDirectoryDiag(object obj) {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = ActiveProfile.ApplicationSettings.SkySurveyCacheDirectory;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                ActiveProfile.ApplicationSettings.SkySurveyCacheDirectory = dialog.SelectedPath;
            }
        }

        private void DownloadIndexes(object obj) {
            AstrometryIndexDownloader.AstrometryIndexDownloaderVM.Show(ActiveProfile.PlateSolveSettings.CygwinLocation);
            ScanForIndexFiles();
        }

        private void OpenImageFileDiag(object o) {
            var diag = new System.Windows.Forms.FolderBrowserDialog();
            diag.SelectedPath = ActiveProfile.ImageFileSettings.FilePath;
            System.Windows.Forms.DialogResult result = diag.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) {
                ActiveProfile.ImageFileSettings.FilePath = diag.SelectedPath + "\\";
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

        private void OpenCygwinFileDiag(object o) {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = profileService.ActiveProfile.PlateSolveSettings.CygwinLocation;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                ActiveProfile.PlateSolveSettings.CygwinLocation = dialog.SelectedPath;
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

        public ICommand OpenSequenceTemplateDiagCommand { get; private set; }

        public ICommand OpenWebRequestCommand { get; private set; }

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