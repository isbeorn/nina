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
using NINA.Core.Enum;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Core.Utility;
using NINA.Astrometry;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Equipment.Exceptions;
using NINA.Core.Utility.Notification;
using NINA.Core.Locale;
using NINA.Astrometry.Interfaces;
using NINA.Equipment.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;
using System.Windows;
using System.Collections.Generic;
using NINA.Sequencer.Interfaces;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.Utility;

namespace NINA.Sequencer.Container {

    [ExportMetadata("Name", "Lbl_SequenceContainer_DeepSkyObjectContainer_Name")]
    [ExportMetadata("Description", "Lbl_SequenceContainer_DeepSkyObjectContainer_Description")]
    [ExportMetadata("Icon", "TelescopeSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Container")]
    [Export(typeof(ISequenceItem))]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]
    public class DeepSkyObjectContainer : SequenceContainer, IDeepSkyObjectContainer {
        private readonly IProfileService profileService;
        private readonly IFramingAssistantVM framingAssistantVM;
        private readonly IPlanetariumFactory planetariumFactory;
        private readonly ICameraMediator cameraMediator;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly IApplicationMediator applicationMediator;
        private INighttimeCalculator nighttimeCalculator;
        private bool exposureInfoListExpanded;
        private AsyncObservableCollection<ExposureInfo> exposureInfoList;

        private InputTarget target;

        [ImportingConstructor]
        public DeepSkyObjectContainer(
                IProfileService profileService,
                INighttimeCalculator nighttimeCalculator,
                IFramingAssistantVM framingAssistantVM,
                IApplicationMediator applicationMediator,
                IPlanetariumFactory planetariumFactory,
                ICameraMediator cameraMediator,
                IFilterWheelMediator filterWheelMediator) : base(new SequentialStrategy()) {
            this.profileService = profileService;
            this.nighttimeCalculator = nighttimeCalculator;
            this.applicationMediator = applicationMediator;
            this.framingAssistantVM = framingAssistantVM;
            this.planetariumFactory = planetariumFactory;
            this.cameraMediator = cameraMediator;
            this.filterWheelMediator = filterWheelMediator;
            Task.Run(() => NighttimeData = nighttimeCalculator.Calculate());
            ExposureInfoList = new AsyncObservableCollection<ExposureInfo>();
            Target = new InputTarget(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude), profileService.ActiveProfile.AstrometrySettings.Horizon);
            CoordsToFramingCommand = new GalaSoft.MvvmLight.Command.RelayCommand(SendCoordinatesToFraming);
            CoordsFromPlanetariumCommand = new GalaSoft.MvvmLight.Command.RelayCommand(GetCoordsFromPlanetarium);
            DropTargetCommand = new GalaSoft.MvvmLight.Command.RelayCommand<object>(DropTarget);
            DeleteExposureInfoCommand = new GalaSoft.MvvmLight.Command.RelayCommand<ExposureInfo>(DeleteExposureInfo);

            WeakEventManager<IProfileService, EventArgs>.AddHandler(profileService, nameof(profileService.LocationChanged), ProfileService_LocationChanged);
            WeakEventManager<IProfileService, EventArgs>.AddHandler(profileService, nameof(profileService.HorizonChanged), ProfileService_HorizonChanged);
            WeakEventManager<INighttimeCalculator, EventArgs>.AddHandler(nighttimeCalculator, nameof(nighttimeCalculator.OnReferenceDayChanged), NighttimeCalculator_OnReferenceDayChanged);
        }

        private void SendCoordinatesToFraming() {
            _ = CoordsToFraming();
        }

        private void GetCoordsFromPlanetarium() {
            _ = CoordsFromPlanetarium();
        }

        private void NighttimeCalculator_OnReferenceDayChanged(object sender, EventArgs e) {
            NighttimeData = nighttimeCalculator.Calculate();
            RaisePropertyChanged(nameof(NighttimeData));
        }

        private void ProfileService_HorizonChanged(object sender, EventArgs e) {
            Target?.DeepSkyObject?.SetCustomHorizon(profileService.ActiveProfile.AstrometrySettings.Horizon);
        }

        private void ProfileService_LocationChanged(object sender, EventArgs e) {
            Target?.SetPosition(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude));
        }

        private void DropTarget(object obj) {
            var p = obj as DragDrop.DropIntoParameters;
            if (p != null) {
                var con = p.Source as TargetSequenceContainer;
                if (con != null) {
                    var dropTarget = con.Container.Target;
                    if (dropTarget != null) {
                        if (con.Container is DeepSkyObjectContainer dsoContainer) {
                            this.ExposureInfoList = new AsyncObservableCollection<ExposureInfo>(dsoContainer.ExposureInfoList);
                        }
                        this.Name = dropTarget.TargetName;
                        this.Target.TargetName = dropTarget.TargetName;
                        this.Target.InputCoordinates = dropTarget.InputCoordinates?.Clone();
                        this.Target.PositionAngle = dropTarget.PositionAngle;
                    }
                }
            }
        }

        public NighttimeData NighttimeData { get; private set; }

        public ICommand CoordsToFramingCommand { get; set; }
        public ICommand CoordsFromPlanetariumCommand { get; set; }
        public ICommand DropTargetCommand { get; set; }
        public ICommand DeleteExposureInfoCommand { get; set; }

        [JsonProperty]
        public InputTarget Target {
            get => target;
            set {
                if (Target != null) {
                    WeakEventManager<InputTarget, EventArgs>.RemoveHandler(Target, nameof(Target.CoordinatesChanged), Target_OnCoordinatesChanged);
                }
                target = value;
                if (Target != null) {
                    WeakEventManager<InputTarget, EventArgs>.AddHandler(Target, nameof(Target.CoordinatesChanged), Target_OnCoordinatesChanged);
                }
                RaisePropertyChanged();
            }
        }

        private void Target_OnCoordinatesChanged(object sender, EventArgs e) {
            AfterParentChanged();
        }

        public override object Clone() {
            var clone = new DeepSkyObjectContainer(profileService, nighttimeCalculator, framingAssistantVM, applicationMediator, planetariumFactory, cameraMediator, filterWheelMediator) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
                Items = new ObservableCollection<ISequenceItem>(Items.Select(i => i.Clone() as ISequenceItem)),
                Triggers = new ObservableCollection<ISequenceTrigger>(Triggers.Select(t => t.Clone() as ISequenceTrigger)),
                Conditions = new ObservableCollection<ISequenceCondition>(Conditions.Select(t => t.Clone() as ISequenceCondition)),
                Target = new InputTarget(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude), profileService.ActiveProfile.AstrometrySettings.Horizon),
                ExposureInfoListExpanded = ExposureInfoListExpanded,
                ExposureInfoList = new AsyncObservableCollection<ExposureInfo>(ExposureInfoList)
            };

            clone.Target.TargetName = this.Target.TargetName;
            clone.Target.InputCoordinates.Coordinates = this.Target.InputCoordinates.Coordinates.Transform(Epoch.J2000);
            clone.Target.PositionAngle = this.Target.PositionAngle;

            foreach (var item in clone.Items) {
                item.AttachNewParent(clone);
            }

            foreach (var condition in clone.Conditions) {
                condition.AttachNewParent(clone);
            }

            foreach (var trigger in clone.Triggers) {
                trigger.AttachNewParent(clone);
            }

            return clone;
        }

        public override string ToString() {
            var baseString = base.ToString();
            return $"{baseString}, Target: {Target?.TargetName} {Target?.InputCoordinates?.Coordinates} {Target?.PositionAngle}";
        }

        private async Task<bool> CoordsToFraming() {
            if (Target.DeepSkyObject?.Coordinates != null) {
                var dso = new DeepSkyObject(Target.DeepSkyObject.Name, Target.DeepSkyObject.Coordinates, profileService.ActiveProfile.AstrometrySettings.Horizon);
                dso.RotationPositionAngle = Target.PositionAngle;
                applicationMediator.ChangeTab(ApplicationTab.FRAMINGASSISTANT);
                return await framingAssistantVM.SetCoordinates(dso);
            }
            return false;
        }

        private async Task<bool> CoordsFromPlanetarium() {
            IPlanetarium s = planetariumFactory.GetPlanetarium();
            DeepSkyObject resp = null;

            try {
                resp = await s.GetTarget();

                if (resp != null) {
                    Target.InputCoordinates.Coordinates = resp.Coordinates;
                    Target.TargetName = resp.Name;
                    this.Name = resp.Name;

                    Target.PositionAngle = 0;

                    if (s.CanGetRotationAngle) {
                        double rotationAngle = await s.GetRotationAngle();

                        if (!double.IsNaN(rotationAngle)) {
                            Target.PositionAngle = rotationAngle;
                        }
                    }

                    Notification.ShowSuccess(string.Format(Loc.Instance["LblPlanetariumCoordsOk"], s.Name));
                }
            } catch (PlanetariumObjectNotSelectedException) {
                Logger.Error($"Attempted to get coordinates from {s.Name} when no object was selected");
                Notification.ShowError(string.Format(Loc.Instance["LblPlanetariumObjectNotSelected"], s.Name));
            } catch (PlanetariumFailedToConnect ex) {
                Logger.Error($"Unable to connect to {s.Name}: {ex}");
                Notification.ShowError(string.Format(Loc.Instance["LblPlanetariumFailedToConnect"], s.Name));
            } catch (Exception ex) {
                Logger.Error($"Failed to get coordinates from {s.Name}: {ex}");
                Notification.ShowError(string.Format(Loc.Instance["LblPlanetariumCoordsError"], s.Name));
            }

            return (resp != null);
        }

        [JsonProperty]
        public bool ExposureInfoListExpanded {
            get => exposureInfoListExpanded;
            set {
                exposureInfoListExpanded = value;
                RaisePropertyChanged();
            }
        }

        public string ExposureInfoSummary {
            get {
                lock (ExposureInfoList) {
                    var exposureByFilter = ExposureInfoList
                        .GroupBy(x => x.Filter)
                        .Select(y => {
                            var filterName = y.Key;
                            var total = TimeSpan.FromSeconds(y.Sum(s => s.ExposureTime * s.Count));
                            return $"{filterName} - {total.Hours:D2}:{total.Minutes:D2}:{total.Seconds:D2}";
                        });

                    return string.Join(" | ", exposureByFilter);
                }
            }
        }

        [JsonProperty]
        public AsyncObservableCollection<ExposureInfo> ExposureInfoList {
            get => exposureInfoList;
            set {
                exposureInfoList = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ExposureInfoSummary));
            }
        }

        public ExposureInfo GetOrCreateExposureCountForItemAndCurrentFilter(IExposureItem exposureItem, double roi) {
            lock (ExposureInfoList) {
                if(exposureItem.ImageType != Equipment.Model.CaptureSequence.ImageTypes.LIGHT) {
                    return null;
                }

                // Check for Gain and Offset when they are below zero if there is a default value available
                var gain = exposureItem.Gain;
                if (gain < 0) {
                    var cameraInfo = cameraMediator.GetInfo();
                    if (cameraInfo.Connected && cameraInfo.CanSetGain) {
                        gain = cameraInfo.DefaultGain;
                    }
                }

                var offset = exposureItem.Offset;
                if (offset < 0) {
                    var cameraInfo = cameraMediator.GetInfo();
                    if (cameraInfo.Connected && cameraInfo.CanSetOffset) {
                        offset = cameraInfo.DefaultOffset;
                    }
                }

                var binning = exposureItem.Binning;
                if (binning == null) {
                    binning = new BinningMode(1, 1);
                }

                var filterInfo = filterWheelMediator.GetInfo();
                var filterName = filterInfo.Connected ? filterInfo.SelectedFilter?.Name ?? Loc.Instance["LblNoFilter"] : Loc.Instance["LblNoFilter"];

                var count = ExposureInfoList.FirstOrDefault(
                    x => x.Filter == filterName
                         && x.ExposureTime == exposureItem.ExposureTime
                         && x.Gain == gain
                         && x.Offset == offset
                         && x.ImageType == exposureItem.ImageType
                         && x.BinningX == binning.X
                         && x.BinningY == binning.Y
                         && x.ROI == roi
                );

                if (count == null) {
                    count = new ExposureInfo(filterName, exposureItem.ExposureTime, gain, offset, exposureItem.ImageType, binning.X, binning.Y, roi);
                    ExposureInfoList.Add(count);
                }

                return count;
            }
        }

        private void DeleteExposureInfo(ExposureInfo obj) {
            lock (ExposureInfoList) {
                ExposureInfoList.Remove(obj);
            }
            RaisePropertyChanged(nameof(ExposureInfoSummary));
        }

        public void IncrementExposureCountForItemAndCurrentFilter(IExposureItem exposureItem, int roi) {
            var count = GetOrCreateExposureCountForItemAndCurrentFilter(exposureItem, roi);
            count?.Increment();
            RaisePropertyChanged(nameof(ExposureInfoSummary));
        }

    }
}