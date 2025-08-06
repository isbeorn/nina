#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.Validations;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Model.Equipment;
using NINA.Core.Locale;
using NINA.Equipment.Model;
using NINA.Equipment.Equipment.MyCamera;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.Sequencer.Interfaces;
using NINA.Image.Interfaces;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Generators;
using NINA.Sequencer.Logic;

namespace NINA.Sequencer.SequenceItem.Imaging {

    [ExportMetadata("Name", "Lbl_SequenceItem_Imaging_TakeExposure_Name")]
    [ExportMetadata("Description", "Lbl_SequenceItem_Imaging_TakeExposure_Description")]
    [ExportMetadata("Icon", "CameraSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Camera")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    [UsesExpressions]

    public partial class TakeExposure : SequenceItem, IExposureItem, IValidatable {
        private ICameraMediator cameraMediator;
        private IImagingMediator imagingMediator;
        private IImageSaveMediator imageSaveMediator;
        private IImageHistoryVM imageHistoryVM;
        private IProfileService profileService;
        Task imageProcessingTask;

        [ImportingConstructor]
        public TakeExposure(IProfileService profileService, ICameraMediator cameraMediator, IImagingMediator imagingMediator, IImageSaveMediator imageSaveMediator, IImageHistoryVM imageHistoryVM) {
            ImageType = CaptureSequence.ImageTypes.LIGHT;
            this.cameraMediator = cameraMediator;
            this.imagingMediator = imagingMediator;
            this.imageSaveMediator = imageSaveMediator;
            this.imageHistoryVM = imageHistoryVM;
            this.profileService = profileService;
            CameraInfo = this.cameraMediator.GetInfo();
        }

        private TakeExposure(TakeExposure cloneMe) : this(cloneMe.profileService, cloneMe.cameraMediator, cloneMe.imagingMediator, cloneMe.imageSaveMediator, cloneMe.imageHistoryVM) {
            CopyMetaData(cloneMe);
        }

        partial void AfterClone(TakeExposure clone) {
            clone.ExposureCount = 0;
            clone.Binning = Binning;
            clone.ImageType = ImageType;

            if (clone.Binning == null) {
                clone.Binning = new BinningMode(1, 1);
            }
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        [IsExpression(Default = 60, Range = [0, 3600, ExpressionRange.MIN_EXCLUSIVE])]
        private double exposureTime;


        [IsExpression(Default = -1, DefaultString = "{Camera}", HasValidator = true)]
        private int gain;

        partial void GainExpressionValidator(Expression expr) {
            if (CameraInfo.CanSetGain && Gain > -1 && (Gain < CameraInfo.GainMin || Gain > CameraInfo.GainMax)) {
                expr.Error = string.Format(Loc.Instance["Lbl_SequenceItem_Imaging_TakeExposure_Validation_Gain"], CameraInfo.GainMin, CameraInfo.GainMax, Gain);
            }
        }

        [IsExpression(Default = -1, DefaultString = "{Camera}", HasValidator = true)]
        private int offset;

        partial void OffsetExpressionValidator(Expression expr) {
            if (CameraInfo.CanSetOffset && Offset > -1 && (Offset < CameraInfo.OffsetMin || Offset > CameraInfo.OffsetMax)) {
                expr.Error = string.Format(Loc.Instance["Lbl_SequenceItem_Imaging_TakeExposure_Validation_Offset"], CameraInfo.OffsetMin, CameraInfo.OffsetMax, Offset);
            }
        }

        private BinningMode binning;

        [JsonProperty]
        public BinningMode Binning { get => binning; set { binning = value; RaisePropertyChanged(); } }

        private string imageType;

        [JsonProperty]
        public string ImageType { get => imageType; set { imageType = value; RaisePropertyChanged(); } }

        private int exposureCount;

        [JsonProperty]
        public int ExposureCount { get => exposureCount; set { exposureCount = value; RaisePropertyChanged(); } }

        private CameraInfo cameraInfo;

        public CameraInfo CameraInfo {
            get => cameraInfo;
            private set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<string> _imageTypes;

        public ObservableCollection<string> ImageTypes {
            get {
                if (_imageTypes == null) {
                    _imageTypes = new ObservableCollection<string>();

                    Type type = typeof(CaptureSequence.ImageTypes);
                    foreach (var p in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)) {
                        var v = p.GetValue(null);
                        _imageTypes.Add(v.ToString());
                    }
                }
                return _imageTypes;
            }
            set {
                _imageTypes = value;
                RaisePropertyChanged();
            }
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var count = ExposureCount;
            var dsoContainer = RetrieveTarget(this.Parent);
            var specificDSOContainer = dsoContainer as DeepSkyObjectContainer;
            if (specificDSOContainer != null) {                
                count = specificDSOContainer.GetOrCreateExposureCountForItemAndCurrentFilter(this, 1)?.Count ?? ExposureCount;
            }

            var capture = new CaptureSequence() {
                ExposureTime = ExposureTime,
                Binning = Binning,
                Gain = Gain,
                Offset = Offset,
                ImageType = ImageType,
                ProgressExposureCount = count,
                TotalExposureCount = count + 1,
            };

            
            var exposureData = await imagingMediator.CaptureImage(capture, token, progress);

            if (IsLightSequence()) {
                imageHistoryVM.Add(exposureData.MetaData.Image.Id, ImageType);
            }

            if (imageProcessingTask != null) {
                await imageProcessingTask;
            }
            imageProcessingTask = ProcessImageData(dsoContainer, exposureData, progress, token);

            if (specificDSOContainer != null) {
                specificDSOContainer.IncrementExposureCountForItemAndCurrentFilter(this, 1);
            }
            ExposureCount++;
        }

        private async Task ProcessImageData(IDeepSkyObjectContainer dsoContainer, IExposureData exposureData, IProgress<ApplicationStatus> progress, CancellationToken token) {
            try {
                var imageParams = new PrepareImageParameters(null, false);
                if (IsLightSequence()) {
                    imageParams = new PrepareImageParameters(true, true);
                }

                var imageData = await exposureData.ToImageData(progress, token);

                var prepareTask = imagingMediator.PrepareImage(imageData, imageParams, token);

                if (IsLightSequence()) {
                    imageHistoryVM.PopulateStatistics(imageData.MetaData.Image.Id, await imageData.Statistics);
                }

                if (dsoContainer != null) {
                    var target = dsoContainer.Target;
                    if (target != null) {
                        imageData.MetaData.Target.Name = target.DeepSkyObject.NameAsAscii;
                        imageData.MetaData.Target.Coordinates = target.InputCoordinates.Coordinates;
                        imageData.MetaData.Target.PositionAngle = target.PositionAngle;
                    }
                }

                var root = ItemUtility.GetRootContainer(this.Parent);
                if (root != null) {
                    imageData.MetaData.Sequence.Title = root.SequenceTitle;
                }

                await imageSaveMediator.Enqueue(imageData, prepareTask, progress, token);

            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        private bool IsLightSequence() {
            return ImageType == CaptureSequence.ImageTypes.SNAPSHOT || ImageType == CaptureSequence.ImageTypes.LIGHT;
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();
            Validate();
        }

        private IDeepSkyObjectContainer RetrieveTarget(ISequenceContainer parent) {
            if (parent != null) {
                var container = parent as IDeepSkyObjectContainer;
                if (container != null) {
                    return container;
                } else {
                    return RetrieveTarget(parent.Parent);
                }
            } else {
                return null;
            }
        }

        public bool Validate() {
            var i = new List<string>();
            CameraInfo = this.cameraMediator.GetInfo();
            if (!CameraInfo.Connected) {
                i.Add(Loc.Instance["LblCameraNotConnected"]);
            } else {
                if (CameraInfo.CanSetGain && Gain > -1 && (Gain < CameraInfo.GainMin || Gain > CameraInfo.GainMax)) {
                    i.Add(string.Format(Loc.Instance["Lbl_SequenceItem_Imaging_TakeExposure_Validation_Gain"], CameraInfo.GainMin, CameraInfo.GainMax, Gain));
                }
                if (CameraInfo.CanSetOffset && Offset > -1 && (Offset < CameraInfo.OffsetMin || Offset > CameraInfo.OffsetMax)) {
                    i.Add(string.Format(Loc.Instance["Lbl_SequenceItem_Imaging_TakeExposure_Validation_Offset"], CameraInfo.OffsetMin, CameraInfo.OffsetMax, Offset));
                }
            }

            var fileSettings = profileService.ActiveProfile.ImageFileSettings;

            if (string.IsNullOrWhiteSpace(fileSettings.FilePath)) {
                i.Add(Loc.Instance["Lbl_SequenceItem_Imaging_TakeExposure_Validation_FilePathEmpty"]);
            } else if (!Directory.Exists(fileSettings.FilePath)) {
                i.Add(Loc.Instance["Lbl_SequenceItem_Imaging_TakeExposure_Validation_FilePathInvalid"]);
            }

            if (GainExpression.Default != CameraInfo.DefaultGain) {
                GainExpression.Default = CameraInfo.DefaultGain;
            }

            if (OffsetExpression.Default != CameraInfo.DefaultOffset) {
                OffsetExpression.Default = CameraInfo.DefaultOffset;
            }

            Expression.ValidateExpressions(i, ExposureTimeExpression, GainExpression, OffsetExpression);

            GainExpression.Range = CameraInfo.CanSetGain ? new double[] { CameraInfo.GainMin, CameraInfo.GainMax, 0 } : null;
            OffsetExpression.Range = CameraInfo.CanSetOffset ? new double[] { CameraInfo.OffsetMin, CameraInfo.OffsetMax, 0 } : null;

            Issues = i;
            return i.Count == 0;
        }

        public override TimeSpan GetEstimatedDuration() {
            return TimeSpan.FromSeconds(ExposureTime);
        }

        public override string ToString() {
            var currentGain = Gain == -1 ? CameraInfo.DefaultGain : Gain;
            var currentOffset = Offset == -1 ? CameraInfo.DefaultOffset : Offset;
            return $"Category: {Category}, Item: {nameof(TakeExposure)}, ExposureTime {ExposureTime}, Gain {currentGain}, Offset {currentOffset}, ImageType {ImageType}, Binning {Binning?.Name ?? "1x1"}";
        }
    }
}