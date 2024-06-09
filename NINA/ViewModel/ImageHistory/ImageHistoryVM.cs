#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using NINA.Equipment.Equipment.MyCamera;
using NINA.Utility;
using NINA.Profile.Interfaces;
using System.Windows.Input;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using NINA.ViewModel.Interfaces;
using System;
using System.ComponentModel;
using System.Windows.Data;
using System.IO;
using CsvHelper;
using System.Globalization;
using NINA.Core.Enum;
using NINA.Image.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Utility;
using NINA.Core.Locale;
using NINA.WPF.Base.Model;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.ViewModel;
using NINA.WPF.Base.Utility.AutoFocus;
using Microsoft.Win32;
using NINA.Equipment.Model;
using NINA.Profile;
using static NINA.Equipment.Equipment.MyGPS.PegasusAstro.UnityApi.DriverUranusReport;

namespace NINA.ViewModel.ImageHistory {

    public class ImageHistoryVM : DockableVM, IImageHistoryVM {

        public ImageHistoryVM(IProfileService profileService, IImageSaveMediator imageSaveMediator) : base(profileService) {
            Title = Loc.Instance["LblHFRHistory"];
            HasSettings = true;

            if (System.Windows.Application.Current != null) {
                ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["HFRHistorySVG"];
            }

            this.imageSaveMediator = imageSaveMediator;
            this.imageSaveMediator.ImageSaved += ImageSaveMediator_ImageSaved;

            ObservableImageHistory = new AsyncObservableCollection<ImageHistoryPoint>();
            ObservableImageHistoryView = new AsyncObservableCollection<ImageHistoryPoint>();
            AutoFocusPoints = new AsyncObservableCollection<ImageHistoryPoint>();
            AutoFocusPointsView = new AsyncObservableCollection<ImageHistoryPoint>();

            ImageHistoryLeftSelected = profileService.ActiveProfile.ImageHistorySettings.ImageHistoryLeftSelected;
            ImageHistoryRightSelected = profileService.ActiveProfile.ImageHistorySettings.ImageHistoryRightSelected;
            profileService.ProfileChanged += ProfileChanged;

            FilterList = new AsyncObservableCollection<string>();
            AllFilters = Loc.Instance["LblHFRHistoryAllFilters"];
            FilterList.Add(AllFilters);
            SelectedFilter = AllFilters;

            PlotClearCommand = new RelayCommand((object o) => PlotClear());
            PlotSaveCommand = new RelayCommand((object o) => PlotSave());
        }

        private string AllFilters { get; set; }

        private void ImageSaveMediator_ImageSaved(object sender, ImageSavedEventArgs e) {
            this.AppendImageProperties(e);
        }

        private IImageSaveMediator imageSaveMediator;
        private AsyncObservableCollection<ImageHistoryPoint> _limitedImageHistoryStack;

        public AsyncObservableCollection<ImageHistoryPoint> ObservableImageHistory {
            get => _limitedImageHistoryStack;
            set {
                _limitedImageHistoryStack = value;
                RaisePropertyChanged();
            }
        }

        private AsyncObservableCollection<ImageHistoryPoint> _observableImageHistoryView;

        public AsyncObservableCollection<ImageHistoryPoint> ObservableImageHistoryView {
            get => this._observableImageHistoryView;
            private set {
                if (value == this._observableImageHistoryView) {
                    return;
                }

                this._observableImageHistoryView = value;
                RaisePropertyChanged();
            }
        }

        private int index { get; set; }

        public void FilterImageHistoryList() {
            // Clear the view and recreate it based on selected filters
            ObservableImageHistoryView.Clear();
            index = 1;
            foreach (ImageHistoryPoint imageHistoryPoint in ObservableImageHistory) {
                if ((this.SelectedFilter.Equals(AllFilters) || imageHistoryPoint.Filter.Equals(this.SelectedFilter)) && (ShowSnapshots || imageHistoryPoint.Type == CaptureSequence.ImageTypes.LIGHT)) {
                    imageHistoryPoint.Index = index++;
                    ObservableImageHistoryView.Add(imageHistoryPoint);
                }
            }
            // Now check if AutoFocus points need to be filtered.
            FilterAutoFocusPoints();
        }

        public void FilterAutoFocusPoints() {
            AutoFocusPointsView.Clear();
            // Check if the autofocuspoint is not filtered
            foreach (ImageHistoryPoint imageHistoryPoint in AutoFocusPoints) {
                var imageHistoryItem = ObservableImageHistoryView.FirstOrDefault(item => item.Id == imageHistoryPoint.Id);
                if (imageHistoryItem != null) {
                    imageHistoryPoint.Index = imageHistoryItem.Index;
                    AutoFocusPointsView.Add(imageHistoryPoint);
                }
            }
        }

        private AsyncObservableCollection<ImageHistoryPoint> autoFocusPoints;

        public AsyncObservableCollection<ImageHistoryPoint> AutoFocusPoints {
            get => autoFocusPoints;
            set {
                autoFocusPoints = value;
                RaisePropertyChanged();
            }
        }

        private AsyncObservableCollection<ImageHistoryPoint> autoFocusPointsView;

        public AsyncObservableCollection<ImageHistoryPoint> AutoFocusPointsView {
            get => autoFocusPointsView;
            set {
                autoFocusPointsView = value;
                RaisePropertyChanged();
            }
        }

        private AsyncObservableCollection<string> _filterList;

        public AsyncObservableCollection<string> FilterList {
            get => _filterList;
            set {
                _filterList = value;
                RaisePropertyChanged();
            }
        }

        private string _selectedFilter;

        public string SelectedFilter {
            get => _selectedFilter;
            set {
                _selectedFilter = value;
                FilterImageHistoryList();
                RaisePropertyChanged();
            }
        }

        private void ProfileChanged(object sender, EventArgs e) {
            _imageHistoryLeftSelected = profileService.ActiveProfile.ImageHistorySettings.ImageHistoryLeftSelected;
            RaisePropertyChanged(nameof(ImageHistoryLeftSelected));
            _imageHistoryRightSelected = profileService.ActiveProfile.ImageHistorySettings.ImageHistoryRightSelected;
            RaisePropertyChanged(nameof(ImageHistoryRightSelected));
        }

        private ImageHistoryEnum _imageHistoryLeftSelected;

        public ImageHistoryEnum ImageHistoryLeftSelected {
            get => _imageHistoryLeftSelected;
            set {
                _imageHistoryLeftSelected = value;
                profileService.ActiveProfile.ImageHistorySettings.ImageHistoryLeftSelected = value;
                ImageHistoryLeftSelectedKey = value.ToString();
                RaisePropertyChanged();
            }
        }

        private ImageHistoryEnum _imageHistoryRightSelected;

        public ImageHistoryEnum ImageHistoryRightSelected {
            get => _imageHistoryRightSelected;
            set {
                _imageHistoryRightSelected = value;
                profileService.ActiveProfile.ImageHistorySettings.ImageHistoryRightSelected = value;
                ImageHistoryRightSelectedKey = value.ToString();
                RaisePropertyChanged();
            }
        }

        private string _imageHistoryLeftSelectedKey;

        public string ImageHistoryLeftSelectedKey {
            get => _imageHistoryLeftSelectedKey;
            set {
                _imageHistoryLeftSelectedKey = value;
                RaisePropertyChanged();
            }
        }

        private string _imageHistoryRightSelectedKey;

        public string ImageHistoryRightSelectedKey {
            get => _imageHistoryRightSelectedKey;
            set {
                _imageHistoryRightSelectedKey = value;
                RaisePropertyChanged();
            }
        }

        private bool showSnapshots = false;

        public bool ShowSnapshots {
            get => showSnapshots;
            set {
                showSnapshots = value;
                FilterImageHistoryList();
                RaisePropertyChanged();
            }
        }

        public List<ImageHistoryPoint> ImageHistory { get; private set; } = new List<ImageHistoryPoint>();

        private object lockObj = new object();

        private static int _exposureId = 0;
        private int ExposureId => Interlocked.Increment(ref _exposureId);
        public int GetNextImageId() {
            return ExposureId;
        }

        public void Add(int id, IImageStatistics statistics, string imageType) {
            lock (lockObj) {
                var point = new ImageHistoryPoint(id, statistics, imageType);
                ImageHistory.Add(point);
            }
        }

        public void Add(int id, string imageType) {
            lock (lockObj) {
                var point = new ImageHistoryPoint(id, imageType);
                ImageHistory.Add(point);
            }
        }

        public void PopulateStatistics(int id, IImageStatistics statistics) {
            lock (lockObj) {
                var imageHistoryItem = ImageHistory.FirstOrDefault(item => item.Id == id);
                if(imageHistoryItem != null) {
                    imageHistoryItem.PopulateStatistics(statistics);
                }
            }
        }

        public void AppendImageProperties(ImageSavedEventArgs imageSavedEventArgs) {
            if (imageSavedEventArgs != null) {
                ImageHistoryPoint imageHistoryItem;
                lock (lockObj) {
                    imageHistoryItem = ImageHistory.FirstOrDefault(item => item.Id == imageSavedEventArgs.MetaData.Image.Id);
                }

                if (imageHistoryItem != null) {
                    imageHistoryItem.PopulateProperties(imageSavedEventArgs);
                    ObservableImageHistory.Add(imageHistoryItem);
                    // Check if the filter needs to be added to the list
                    if (!FilterList.Contains(imageSavedEventArgs.Filter))
                        FilterList.Add(imageSavedEventArgs.Filter);
                    // Add to view if it's not filtered
                    if ((this.SelectedFilter.Equals(AllFilters) || imageHistoryItem.Filter.Equals(this.SelectedFilter)) && (ShowSnapshots || imageHistoryItem.Type == CaptureSequence.ImageTypes.LIGHT)) {
                        imageHistoryItem.Index = index++;
                        ObservableImageHistoryView.Add(imageHistoryItem);
                    }
                }
            }
        }

        public void AppendAutoFocusPoint(AutoFocusReport report) {
            if (report != null) {
                ImageHistoryPoint last;
                lock (lockObj) {
                    last = ImageHistory.LastOrDefault();
                }

                if (last == null) {
                    last = new ImageHistoryPoint(GetNextImageId(), "NONE");
                }
                last.PopulateAFPoint(report);
                AutoFocusPoints.Add(last);
                if ((this.SelectedFilter.Equals(AllFilters) || last.Filter.Equals(this.SelectedFilter)) && (ShowSnapshots || last.Type == CaptureSequence.ImageTypes.LIGHT)) {
                    AutoFocusPointsView.Add(last);
                }
            }
        }

        public void PlotClear() {
            ObservableImageHistory.Clear();
            AutoFocusPoints.Clear();
            ObservableImageHistoryView.Clear();
            AutoFocusPointsView.Clear();
            ImageHistory.Clear();
            index = 0;
            _exposureId = 0;
            Logger.Info("Image history has been cleared");
        }

        public void PlotSave() {
            if (this.ObservableImageHistory.Count != 0) {
                FileDialog sfd = new SaveFileDialog();
                sfd.FileName = NINA.Core.Utility.CoreUtil.ApplicationStartDate.ToString("yyyy-MM-dd") + "_history.csv";
                sfd.InitialDirectory = Path.GetDirectoryName(ActiveProfile.SequenceSettings.DefaultSequenceFolder);
                if (sfd.ShowDialog() == true) {
                    if (!sfd.FileName.ToLower().EndsWith(".csv")) sfd.FileName += ".csv";
                    using (var writer = new StreamWriter(sfd.FileName))
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture)) {
                        csv.Context.RegisterClassMap<ImageHistoryPointMap>();
                        csv.WriteRecords(ObservableImageHistory);
                    }
                }
                Logger.Info($"Image history has been saved to {sfd.FileName}");
            }
        }

        public ICommand PlotClearCommand { get; private set; }
        public ICommand PlotSaveCommand { get; private set; }
    }
}