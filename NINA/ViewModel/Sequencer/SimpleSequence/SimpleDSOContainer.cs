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
using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using NINA.Core.Enum;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.MyMessageBox;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyPlanetarium;
using NINA.Equipment.Exceptions;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Autofocus;
using NINA.Sequencer.SequenceItem.Guider;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.SequenceItem.Platesolving;
using NINA.Sequencer.SequenceItem.Telescope;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Trigger.Autofocus;
using NINA.Sequencer.Validations;
using NINA.Utility;
using NINA.ViewModel.FramingAssistant;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel.Sequencer.SimpleSequence;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NINA.Sequencer.Container {

    [JsonObject(MemberSerialization.OptIn)]
    public class SimpleDSOContainer : SequenceContainer, IDeepSkyObjectContainer, ISimpleDSOContainer {
        private readonly IProfileService profileService;
        private readonly ISequencerFactory factory;
        private readonly IFramingAssistantVM framingAssistantVM;
        private readonly IPlanetariumFactory planetariumFactory;
        private readonly IApplicationMediator applicationMediator;
        private INighttimeCalculator nighttimeCalculator;
        private InputTarget target;
        private CameraInfo cameraInfo;
        private int delay;
        private bool stargGuiding;
        private bool slewToTarget;
        private bool centerTarget;
        private bool rotateTarget;
        private bool autoFocusOnStart;
        private bool autoFocusOnFilterChange;
        private AutofocusAfterFilterChange afOnFilterChangeTrigger;
        private AutofocusAfterTimeTrigger autoFocusAfterSetTimeTrigger;
        private AutofocusAfterExposures autoFocusAfterSetExposuresTrigger;
        private AutofocusAfterTemperatureChangeTrigger autoFocusAfterTemperatureChangeTrigger;
        private AutofocusAfterHFRIncreaseTrigger autoFocusAfterHFRChangeTrigger;
        private LoopCondition rotateLoopCondition;
        private bool autoFocusAfterSetTime;
        private bool autoFocusAfterSetExposures;
        private bool autoFocusAfterTemperatureChange;
        private bool autoFocusAfterHFRChange;
        private ICameraMediator cameraMediator;
        private ISimpleExposure selectedSimpleExposure;
        private DateTime estimatedStartTime;
        private DateTime estimatedEndTime;
        private TimeSpan estimatedDuration;
        private ISimpleExposure activeExposure;
        private SequenceMode mode;

        [ImportingConstructor]
        public SimpleDSOContainer(
                ISequencerFactory factory, /* Having the factory here is an antipattern, however the old sequencer won't be further enhanced anyways */
                IProfileService profileService,
                ICameraMediator cameraMediator,
                INighttimeCalculator nighttimeCalculator,
                IFramingAssistantVM framingAssistantVM,
                IApplicationMediator applicationMediator,
                IPlanetariumFactory planetariumFactory) : base(new SequentialStrategy()) {
            this.profileService = profileService;
            this.factory = factory;
            this.cameraMediator = cameraMediator;
            this.nighttimeCalculator = nighttimeCalculator;
            this.applicationMediator = applicationMediator;
            this.framingAssistantVM = framingAssistantVM;
            this.planetariumFactory = planetariumFactory;

            CameraInfo = this.cameraMediator.GetInfo();
            Task.Run(() => NighttimeData = nighttimeCalculator.Calculate());
            Target = new InputTarget(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude), profileService.ActiveProfile.AstrometrySettings.Horizon);
            CoordsToFramingCommand = new AsyncCommand<bool>(() => Task.Run(CoordsToFraming));
            CoordsFromPlanetariumCommand = new AsyncCommand<bool>(() => Task.Run(CoordsFromPlanetarium));

            AddSimpleExposureCommand = new RelayCommand((object o) => AddSimpleExposure());
            RemoveSimpleExposureCommand = new RelayCommand(RemoveSimpleExposure);
            ResetSimpleExposureCommand = new RelayCommand(ResetSimpleExposure);
            PromoteSimpleExposureCommand = new RelayCommand(PromoteSimpleExposure);
            DemoteSimpleExposureCommand = new RelayCommand(DemoteSimpleExposure);

            this.afOnFilterChangeTrigger = factory.GetTrigger<AutofocusAfterFilterChange>();
            this.autoFocusAfterSetTimeTrigger = factory.GetTrigger<AutofocusAfterTimeTrigger>();
            this.autoFocusAfterSetExposuresTrigger = factory.GetTrigger<AutofocusAfterExposures>();
            this.autoFocusAfterTemperatureChangeTrigger = factory.GetTrigger<AutofocusAfterTemperatureChangeTrigger>();
            this.autoFocusAfterHFRChangeTrigger = factory.GetTrigger<AutofocusAfterHFRIncreaseTrigger>();

            this.rotateLoopCondition = factory.GetCondition<LoopCondition>();
            this.rotateLoopCondition.Iterations = 1;
            WeakEventManager<INighttimeCalculator, EventArgs>.AddHandler(nighttimeCalculator, nameof(nighttimeCalculator.OnReferenceDayChanged), NighttimeCalculator_OnReferenceDayChanged);
        }

        public override ICommand ResetProgressCommand => new RelayCommand(
           (o) => {
               if (MyMessageBox.Show(Loc.Instance["Lbl_OldSequencer_ResetTargetRows"], Loc.Instance["Lbl_OldSequencer_ResetTarget"], System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.Yes) == System.Windows.MessageBoxResult.Yes) {
                   base.ResetProgressCommand.Execute(o);
               }
           }
       );

        public ISimpleExposure ActiveExposure {
            get => activeExposure;
            set {
                activeExposure = value;
                RaisePropertyChanged();
            }
        }

        //Workaround to prevent the container being moved into another dso container
        public override bool IsExpanded { get => false; set { } }

        public override void MoveUp() {
            if (this.Parent.Items.IndexOf(this) > 0) {
                base.MoveUp();
            }
            SetChanged();
        }

        public override void MoveDown() {
            if (this.Parent.Items.IndexOf(this) < this.Parent.Items.Count - 1) {
                base.MoveDown();
            }
            SetChanged();
        }

        public TimeSpan CalculateEstimatedRuntime() {
            var duration = TimeSpan.Zero;
            //todo - add estimations for autofocus if possible
            foreach (var item in Items) {
                var se = item as SimpleExposure;
                if (se.Enabled) {
                    var exposureTime = se.GetTakeExposure().GetEstimatedDuration().TotalSeconds;
                    var downloadTime = profileService.ActiveProfile.SequenceSettings.EstimatedDownloadTime.TotalSeconds;
                    var iterations = (se.GetLoopCondition() as LoopCondition).Iterations;
                    var completedIterations = (se.GetLoopCondition() as LoopCondition).CompletedIterations;
                    var rotatesLeft = this.rotateLoopCondition.Iterations - this.rotateLoopCondition.CompletedIterations;

                    if (Mode == SequenceMode.ROTATE) {                        
                        duration += TimeSpan.FromSeconds((exposureTime + downloadTime) * Math.Max(0, (rotatesLeft - completedIterations)));
                    } else {
                        duration += TimeSpan.FromSeconds((exposureTime + downloadTime) * (iterations - completedIterations));
                    }
                }
            }
            return duration;
        }

        public DateTime EstimatedStartTime {
            get => estimatedStartTime;
            set {
                estimatedStartTime = value;
                RaisePropertyChanged();
            }
        }

        public DateTime EstimatedEndTime {
            get => estimatedEndTime;
            set {
                estimatedEndTime = value;
                RaisePropertyChanged();
            }
        }

        public TimeSpan EstimatedDuration {
            get => estimatedDuration;
            set {
                estimatedDuration = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(EstimatedDurationFormatted));
            }
        }
        public string EstimatedDurationFormatted {
            get {
                if (EstimatedDuration.TotalDays > 1) {
                    return EstimatedDuration.ToString(@"dd\d\ hh\h\ mm\m\ ss\s");

                } else if (EstimatedDuration.TotalHours > 1) {
                    return EstimatedDuration.ToString(@"hh\h\ mm\m\ ss\s");

                } else if (EstimatedDuration.TotalMinutes > 1) {
                    return EstimatedDuration.ToString(@"mm\m\ ss\s");

                } else {
                    return EstimatedDuration.ToString(@"ss\s");
                }
            }
        }

        public ISimpleExposure AddSimpleExposure() {
            SimpleExposure item;
            if (SelectedSimpleExposure != null) {
                item = (SimpleExposure)SelectedSimpleExposure.Clone();
                this.Add(item);
            } else {
                item = new SimpleExposure(factory);
                this.Add(item);
            }

            item.PropertyChanged += Item_PropertyChanged1;

            SelectedSimpleExposure = item;
            ActiveExposure = Items.FirstOrDefault() as SimpleExposure;
            this.ResetProgressCascaded();

            SetChanged();

            return item;
        }

        private void Item_PropertyChanged1(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(SimpleExposure.Status)) {
                var se = sender as SimpleExposure;
                if (se.Status == SequenceEntityStatus.RUNNING) {
                    ActiveExposure = se;
                }
            }
        }

        public override ISequenceRootContainer GetSequenceRootContainer() {
            return Parent as ISequenceRootContainer;
        }

        private void RemoveSimpleExposure(object obj) {
            if (SelectedSimpleExposure != null) {
                var idx = this.Items.IndexOf(SelectedSimpleExposure);
                (SelectedSimpleExposure as SimpleExposure).PropertyChanged -= Item_PropertyChanged;
                this.Remove(SelectedSimpleExposure);

                if (idx >= this.Items.Count) {
                    SelectedSimpleExposure = (SimpleExposure)this.Items.LastOrDefault();
                } else {
                    SelectedSimpleExposure = (SimpleExposure)this.Items[idx];
                }

                SetChanged();

            }
        }

        private void ResetSimpleExposure(object obj) {
            SelectedSimpleExposure?.ResetAll();
            this.ResetProgressCascaded();
        }

        private void PromoteSimpleExposure(object obj) {
            var idx = this.Items.IndexOf(SelectedSimpleExposure);
            if (idx > 0 && idx <= this.Items.Count - 1) {
                SelectedSimpleExposure?.MoveUp();
                SelectedSimpleExposure = (SimpleExposure)this.Items[--idx];
                SetChanged();
            }
        }

        private void DemoteSimpleExposure(object obj) {
            var idx = this.Items.IndexOf(SelectedSimpleExposure);
            if (idx >= 0 && idx < this.Items.Count - 1) {
                SelectedSimpleExposure?.MoveDown();
                SelectedSimpleExposure = (SimpleExposure)this.Items[++idx];
                SetChanged();
            }
        }

        public int RotateIterations {
            get => this.rotateLoopCondition.Iterations;
            set {
                this.rotateLoopCondition.Iterations = value;
                RaisePropertyChanged();
                this.ResetProgressCascaded();
                foreach (var item in Items) {
                    item.ResetProgress();
                }
                SetChanged();
            }
        }

        public SequenceMode Mode {
            get => mode;
            set {
                if (mode != value) {
                    this.mode = value;

                    if (mode == SequenceMode.ROTATE) {
                        var maxIteration = 1;
                        this.Conditions.Add(rotateLoopCondition);
                        foreach (var item in Items) {
                            var se = item as SimpleExposure;
                            var loop = se.GetLoopCondition() as LoopCondition;
                            maxIteration = Math.Max(maxIteration, loop.Iterations);
                            loop.Iterations = 1;
                            loop.ResetProgress();
                        }
                        RotateIterations = maxIteration;
                    } else {
                        this.Conditions.Remove(rotateLoopCondition);
                        foreach (var item in Items) {
                            var se = item as SimpleExposure;
                            var loop = se.GetLoopCondition() as LoopCondition;
                            loop.Iterations = RotateIterations;
                            loop.ResetProgress();
                        }
                    }
                    SetChanged();
                    RaisePropertyChanged();
                }
            }
        }

        public ISimpleExposure SelectedSimpleExposure {
            get => selectedSimpleExposure;
            set {
                selectedSimpleExposure = value;
                RaisePropertyChanged();
                SetChanged();
            }
        }

        public CameraInfo CameraInfo {
            get => cameraInfo;
            private set {
                cameraInfo = value;
                RaisePropertyChanged();
                SetChanged();
            }
        }

        public override bool Validate() {
            CameraInfo = this.cameraMediator.GetInfo();

            var issues = new List<string>();
            if (this.Status == SequenceEntityStatus.CREATED) {
                var startup = CreateStartupContainer();
                startup.Validate();

                foreach (var item in startup.GetItemsSnapshot()) {
                    if (item is IValidatable validatable) {
                        issues.AddRange(validatable.Issues);
                    }
                }

                var closure = CreateClosureContainer();
                closure.Validate();

                foreach (var item in closure.GetItemsSnapshot()) {
                    if (item is IValidatable validatable) {
                        issues.AddRange(validatable.Issues);
                    }
                }
            }


            this.Issues = issues;
            return base.Validate();
        }

        [JsonProperty]
        public int Delay {
            get => delay;
            set {
                delay = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public bool StartGuiding {
            get => stargGuiding;
            set {
                stargGuiding = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public bool SlewToTarget {
            get => slewToTarget;
            set {
                slewToTarget = value;
                if (!slewToTarget) {
                    CenterTarget = slewToTarget;
                    RotateTarget = slewToTarget;
                }
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public bool CenterTarget {
            get => centerTarget;
            set {
                centerTarget = value;
                if (centerTarget) { SlewToTarget = centerTarget; }
                if (!centerTarget) { RotateTarget = centerTarget; }
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public bool RotateTarget {
            get => rotateTarget;
            set {
                rotateTarget = value;
                if (rotateTarget) { CenterTarget = rotateTarget; }
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public bool AutoFocusOnStart {
            get => autoFocusOnStart;
            set {
                autoFocusOnStart = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public bool AutoFocusOnFilterChange {
            get => autoFocusOnFilterChange;
            set {
                autoFocusOnFilterChange = value;
                if (value) {
                    this.Triggers.Add(afOnFilterChangeTrigger);
                } else {
                    this.Triggers.Remove(afOnFilterChangeTrigger);
                }
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public bool AutoFocusAfterSetTime {
            get => autoFocusAfterSetTime;
            set {
                autoFocusAfterSetTime = value;
                if (value) {
                    this.Triggers.Add(autoFocusAfterSetTimeTrigger);
                } else {
                    this.Triggers.Remove(autoFocusAfterSetTimeTrigger);
                }
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public double AutoFocusSetTime {
            get => autoFocusAfterSetTimeTrigger.Amount;
            set {
                autoFocusAfterSetTimeTrigger.Amount = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public bool AutoFocusAfterSetExposures {
            get => autoFocusAfterSetExposures;
            set {
                autoFocusAfterSetExposures = value;
                if (value) {
                    this.Triggers.Add(autoFocusAfterSetExposuresTrigger);
                } else {
                    this.Triggers.Remove(autoFocusAfterSetExposuresTrigger);
                }
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public int AutoFocusSetExposures {
            get => autoFocusAfterSetExposuresTrigger.AfterExposures;
            set {
                autoFocusAfterSetExposuresTrigger.AfterExposures = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public bool AutoFocusAfterTemperatureChange {
            get => autoFocusAfterTemperatureChange;
            set {
                autoFocusAfterTemperatureChange = value;
                if (value) {
                    this.Triggers.Add(autoFocusAfterTemperatureChangeTrigger);
                } else {
                    this.Triggers.Remove(autoFocusAfterTemperatureChangeTrigger);
                }
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public double AutoFocusAfterTemperatureChangeAmount {
            get => autoFocusAfterTemperatureChangeTrigger.Amount;
            set {
                autoFocusAfterTemperatureChangeTrigger.Amount = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public bool AutoFocusAfterHFRChange {
            get => autoFocusAfterHFRChange;
            set {
                autoFocusAfterHFRChange = value;
                if (value) {
                    this.Triggers.Add(autoFocusAfterHFRChangeTrigger);
                } else {
                    this.Triggers.Remove(autoFocusAfterHFRChangeTrigger);
                }
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public double AutoFocusAfterHFRChangeAmount {
            get => autoFocusAfterHFRChangeTrigger.Amount;
            set {
                autoFocusAfterHFRChangeTrigger.Amount = value;
                RaisePropertyChanged();
            }
        }

        [JsonProperty]
        public InputTarget Target {
            get => target;
            set {
                if (Target != null) {
                    Target.PropertyChanged -= Target_PropertyChanged;
                    Target.CoordinatesChanged -= Target_OnCoordinatesChanged;
                }
                target = value;
                if (Target != null) {
                    Target.PropertyChanged += Target_PropertyChanged;
                    Target.CoordinatesChanged += Target_OnCoordinatesChanged;
                }
                RaisePropertyChanged();
            }
        }

        private void NighttimeCalculator_OnReferenceDayChanged(object sender, EventArgs e) {
            NighttimeData = nighttimeCalculator.Calculate();
            RaisePropertyChanged(nameof(NighttimeData));
        }

        public NighttimeData NighttimeData { get; private set; }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var startup = CreateStartupContainer();
            await startup.Execute(progress, token);

            await base.Execute(progress, token);

            var closure = CreateClosureContainer();
            await closure.Execute(progress, token);
        }

        private SequentialContainer CreateStartupContainer() {
            var startup = factory.GetContainer<SequentialContainer>();

            if (Delay > 0) {
                var wait = factory.GetItem<WaitForTimeSpan>();
                wait.Time = Delay;
                startup.Add(wait);
            }

            // Find the first valid instruction to determine which filter is currently relevant
            var firstInstruction = Items.FirstOrDefault(x => {
                if (x is SimpleExposure exp) {
                    var loop = exp.GetLoopCondition() as LoopCondition;
                    if (exp.Enabled && exp.Status == SequenceEntityStatus.CREATED && loop != null && loop.CompletedIterations < loop.Iterations) {
                        return true;
                    }
                }
                return false;
            });
            if (firstInstruction != null) {
                startup.Add((ISequenceItem)(firstInstruction as SimpleExposure).GetSwitchFilter().Clone());
            }

            if (SlewToTarget && !CenterTarget && !RotateTarget) {
                var slew = factory.GetItem<SlewScopeToRaDec>();
                slew.Coordinates = Target.InputCoordinates;
                startup.Add(slew);
            }

            if (CenterTarget && !RotateTarget) {
                var center = factory.GetItem<Center>();
                center.Coordinates = Target.InputCoordinates;
                startup.Add(center);
            }

            if (RotateTarget) {
                var rotate = factory.GetItem<CenterAndRotate>();
                rotate.Coordinates = Target.InputCoordinates;
                rotate.PositionAngle = Target.PositionAngle;
                startup.Add(rotate);
            }

            if (StartGuiding) {
                startup.Add(factory.GetItem<StartGuiding>());
            }

            if (AutoFocusOnStart) {
                startup.Add(factory.GetItem<RunAutofocus>());
            }

            return startup;
        }

        private SequentialContainer CreateClosureContainer() {
            var closure = factory.GetContainer<SequentialContainer>();

            if(StartGuiding) {
                closure.Add(factory.GetItem<StopGuiding>());
            }

            return closure;
        }

        public override object Clone() {
            var clone = new SimpleDSOContainer(factory, profileService, cameraMediator, nighttimeCalculator, framingAssistantVM, applicationMediator, planetariumFactory) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
                Items = new ObservableCollection<ISequenceItem>(Items.Select(i => i.Clone() as ISequenceItem)),
                Triggers = new ObservableCollection<ISequenceTrigger>(),
                Conditions = new ObservableCollection<ISequenceCondition>(),
                Target = new InputTarget(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude), profileService.ActiveProfile.AstrometrySettings.Horizon)
            };

            clone.Target.TargetName = this.Target.TargetName;
            clone.Target.InputCoordinates.Coordinates = this.Target.InputCoordinates.Coordinates.Transform(Epoch.J2000);
            clone.Target.PositionAngle = this.Target.PositionAngle;

            clone.Delay = Delay;
            clone.StartGuiding = StartGuiding;
            clone.SlewToTarget = SlewToTarget;
            clone.CenterTarget = CenterTarget;
            clone.RotateTarget = RotateTarget;
            clone.AutoFocusOnStart = AutoFocusOnStart;
            clone.AutoFocusOnFilterChange = AutoFocusOnFilterChange;
            clone.AutoFocusAfterSetTime = AutoFocusAfterSetTime;
            clone.AutoFocusSetTime = AutoFocusSetTime;
            clone.AutoFocusAfterSetExposures = AutoFocusAfterSetExposures;
            clone.AutoFocusSetExposures = AutoFocusSetExposures;
            clone.AutoFocusAfterTemperatureChange = AutoFocusAfterTemperatureChange;
            clone.AutoFocusAfterTemperatureChangeAmount = AutoFocusAfterTemperatureChangeAmount;
            clone.AutoFocusAfterHFRChange = AutoFocusAfterHFRChange;
            clone.AutoFocusAfterHFRChangeAmount = AutoFocusAfterHFRChangeAmount;

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

        public Dictionary<string, bool> HasChanges { get => hasChanges;  }

        private Dictionary<string, bool> hasChanges = new Dictionary<string, bool>() { { SequenceEntityINPC.defaultChangeSet, false } };

        public void ClearHasChanged() {
            foreach (string key in HasChanges.Keys) {
                HasChanges[key] = false;
            }
        }

        public bool DoesHaveChanges(string hasChangeSet) {
            return HasChanges.ContainsKey(hasChangeSet) && HasChanges[hasChangeSet];
        }
        public void SetChanged(string changedSet = SequenceEntityINPC.defaultChangeSet) {
            if (HasChanges.ContainsKey(changedSet))
                HasChanges[changedSet] = true;
            else
                HasChanges.Add(changedSet, true);
        }

        public bool ShouldStopForChanges(string name, string hasChangedSet) {
            if ((HasChanges.ContainsKey(hasChangedSet)) && (HasChanges[hasChangedSet]) &&
                (MyMessageBox.Show(
                    string.Format(Loc.Instance["LblChangedSequenceWarning"], name ?? ""),
                    Loc.Instance["LblChangedSequenceWarningTitle"],
                    MessageBoxButton.YesNo, System.Windows.MessageBoxResult.Yes) == System.Windows.MessageBoxResult.No)) {
                return true;
            }
            return false;
        }

        public IDeepSkyObjectContainer TransformToDSOContainer() {
            var c = factory.GetContainer<DeepSkyObjectContainer>();
            var t = new InputTarget(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude), profileService.ActiveProfile.AstrometrySettings.Horizon);
            t.TargetName = this.Target.TargetName;
            t.InputCoordinates.Coordinates = this.Target.InputCoordinates.Coordinates.Transform(Epoch.J2000);
            t.PositionAngle = this.Target.PositionAngle;
            c.Target = t;
            c.Name = t.TargetName;

            var startup = CreateStartupContainer();
            startup.Name = Loc.Instance["Lbl_OldSequencer_TargetPreparation"];
            if (startup.Items.Count > 0) {
                c.Add(startup);
            }

            var imaging = factory.GetContainer<SequentialContainer>();
            foreach (var condition in Conditions) {
                imaging.Add((ISequenceCondition)condition.Clone());
            }
            foreach (var trigger in Triggers) {
                imaging.Add((ISequenceTrigger)trigger.Clone());
            }
            foreach (var item in Items) {
                var simple = item as SimpleExposure;
                if (simple.Enabled) {
                    imaging.Add(simple.TransformToSmartExposure() as SmartExposure);
                }
            }
            imaging.Name = Loc.Instance["Lbl_OldSequencer_TargetImaging"];
            c.Add(imaging);

            var closure = CreateClosureContainer();
            closure.Name = Loc.Instance["Lbl_OldSequencer_TargetClosure"];
            if (closure.Items.Count > 0) {
                c.Add(closure);
            }

            return c;
        }

        private void Target_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(InputTarget.TargetName)) {
                Name = Target.TargetName;
                AfterParentChanged();
            }
        }

        private void Target_OnCoordinatesChanged(object sender, EventArgs e) {
            AfterParentChanged();
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

        public override string ToString() {
            var baseString = base.ToString();
            return $"{baseString}, Target: {Target?.TargetName} {Target?.InputCoordinates?.Coordinates} {Target?.PositionAngle}";
        }

        public ICommand AddSimpleExposureCommand { get; private set; }
        public ICommand RemoveSimpleExposureCommand { get; private set; }
        public ICommand ResetSimpleExposureCommand { get; private set; }
        public ICommand PromoteSimpleExposureCommand { get; private set; }
        public ICommand DemoteSimpleExposureCommand { get; private set; }
        public ICommand CoordsToFramingCommand { get; private set; }
        public ICommand CoordsFromPlanetariumCommand { get; private set; }
        public string FileName { get; set; }
    }
}