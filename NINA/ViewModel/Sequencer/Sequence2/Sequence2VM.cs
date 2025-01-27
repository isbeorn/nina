#region "copyright"
/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
#endregion "copyright"
using Accord.IO;
using Newtonsoft.Json;
using NINA.Core.Enum;
using NINA.Equipment.Equipment.MyPlanetarium;
using NINA.Profile.Interfaces;
using NINA.Sequencer;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Utility;
using NINA.Sequencer.Serialization;
using NINA.Sequencer.Trigger;
using NINA.Utility;
using NINA.Astrometry;
using NINA.ViewModel.FramingAssistant;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;
using System.Windows.Threading;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.Core.Utility;
using NINA.Core.MyMessageBox;
using NINA.Core.Locale;
using NINA.Core.Utility.Notification;
using NINA.Core.Model;
using NINA.Astrometry.Interfaces;
using NINA.Equipment.Interfaces;
using NINA.WPF.Base.ViewModel;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.Trigger.Autofocus;
using NINA.Equipment.Equipment.MyCamera;
using System.ComponentModel;
using NINA.Core.Utility.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NINA.ViewModel.Sequencer {

    internal partial class Sequence2VM : BaseVM, ISequence2VM {
        private ICommandLineOptions commandLineOptions;
        private IApplicationStatusMediator applicationStatusMediator;
        private ISequenceMediator sequenceMediator;
        private IApplicationMediator applicationMediator;
        private ICameraMediator cameraMediator;
        private DispatcherTimer validationTimer;

        public Sequence2VM(
            IProfileService profileService,
            ICommandLineOptions commandLineOptions,
            ISequenceMediator sequenceMediator,
            IApplicationMediator applicationMediator,
            IApplicationStatusMediator applicationStatusMediator,
            ICameraMediator cameraMediator,
            ISequencerFactory factory
            ) : base(profileService) {

            this.commandLineOptions = commandLineOptions;
            this.applicationStatusMediator = applicationStatusMediator;

            this.sequenceMediator = sequenceMediator;
            this.applicationMediator = applicationMediator;
            this.cameraMediator = cameraMediator;
            cameraMediator.RegisterConsumer(this);

            SequencerFactory = factory;

            StartSequenceCommand = new AsyncCommand<bool>(StartSequence, (object o) => cameraMediator.IsFreeToCapture(this));
            CancelSequenceCommand = new GalaSoft.MvvmLight.Command.RelayCommand<object>(CancelSequence);
            SaveAsSequenceCommand = new GalaSoft.MvvmLight.Command.RelayCommand<object>(SaveAsSequence);
            SaveSequenceCommand = new GalaSoft.MvvmLight.Command.RelayCommand<object>(SaveSequence);
            AddTemplateCommand = new GalaSoft.MvvmLight.Command.RelayCommand<object>(AddTemplate);
            AddTargetToControllerCommand = new GalaSoft.MvvmLight.Command.RelayCommand<object>(AddTargetToController);
            LoadSequenceCommand = new GalaSoft.MvvmLight.Command.RelayCommand<object>(LoadSequence);
            SwitchToOverviewCommand = new GalaSoft.MvvmLight.Command.RelayCommand(sequenceMediator.SwitchToOverview, IsSimpleSequencerEnabled);

            DetachCommand = new GalaSoft.MvvmLight.Command.RelayCommand<object>(Detach);
            SkipCurrentItemCommand = new AsyncCommand<bool>(SkipCurrentItem);
            SkipToEndOfSequenceCommand = new AsyncCommand<bool>(SkipToEndOfSequence);
        }

        public event Func<object, EventArgs, Task> SequenceStarting;
        public event Func<object, EventArgs, Task> SequenceFinished;

        private string savePath = string.Empty;
        public string SavePath {
            get => savePath;
            set {
                savePath = value;
                if (!string.IsNullOrWhiteSpace(value)) {
                    DetachSequencerINPC();
                    Sequencer.MainContainer.SequenceTitle = Path.GetFileNameWithoutExtension(value);
                    AttachSequencerINPC();
                }
                RaisePropertyChanged();
            }
        }

        private ISequencer sequencer;
        public ISequencer Sequencer {
            get => sequencer;
            private set {
                DetachSequencerINPC();
                sequencer = value;
                AttachSequencerINPC();
            }
        }

        [ObservableProperty]
        private bool isLocked;

        [RelayCommand]
        public void LockSequence() {
            IsLocked = true;
            Logger.Info("Sequencer unlocked");
        }
        [RelayCommand]
        public void UnlockSequence() {
            IsLocked = false;
            Logger.Info("Sequencer locked");
        }

        [ObservableProperty]
        private bool canDragAndDrop = true;

        [RelayCommand]
        public void DisableDragAndDrop() {
            CanDragAndDrop = false;
            Logger.Info("Sequencer drag and drop disabled");
        }
        [RelayCommand]
        public void EnableDragAndDrop() {
            CanDragAndDrop = true;
            Logger.Info("Sequencer drag and drop enabled");
        }

        private void DetachSequencerINPC() {
            if (sequencer != null && sequencer.MainContainer is INotifyPropertyChanged oldinpc) {
                oldinpc.PropertyChanged -= Sequencer_PropertyChanged;
            }
        }

        private void AttachSequencerINPC() {
            if (sequencer != null && sequencer.MainContainer is INotifyPropertyChanged newinpc) {
                newinpc.PropertyChanged += Sequencer_PropertyChanged;
            }
        }

        private void Sequencer_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(Sequencer.MainContainer.SequenceTitle)) {
                SavePath = string.Empty;
            }
        }

        private bool IsSimpleSequencerEnabled() {
            return !profileService.ActiveProfile.SequenceSettings.DisableSimpleSequencer;
        }

        public void Dispose() {
            cameraMediator.RemoveConsumer(this);
        }

        private void Detach(object o) {
            var source = (o as DropIntoParameters)?.Source;
            source?.Detach();
            if (source != null) {
                if (source is TemplatedSequenceContainer) {
                    var result = MyMessageBox.Show(string.Format(Loc.Instance["LblTemplate_DeleteTemplateMessageBox_Text"], (source as TemplatedSequenceContainer).Container.Name),
                        Loc.Instance["LblTemplate_DeleteTemplateMessageBox_Caption"], System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
                    if (result == System.Windows.MessageBoxResult.OK) {
                        TemplateController.DeleteUserTemplate(source as TemplatedSequenceContainer);
                    }
                }
                if (source is TargetSequenceContainer) {
                    var result = MyMessageBox.Show(string.Format(Loc.Instance["Lbl_Sequencer_TargetSidebar_DeleteTargetMessageBox_Text"], (source as TargetSequenceContainer).Name),
                        Loc.Instance["Lbl_Sequencer_TargetSidebar_DeleteTargetMessageBox_Caption"], System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxResult.No);
                    if (result == System.Windows.MessageBoxResult.Yes) {
                        TargetController.DeleteTarget(source as TargetSequenceContainer);
                    }
                }
            }
        }

        public Task Initialize() {
            return Task.Run(async () => {
                await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => {
                    SequenceJsonConverter = new SequenceJsonConverter(SequencerFactory);
                    TemplateController = new TemplateController(SequenceJsonConverter, profileService);
                    TargetController = new TargetController(SequenceJsonConverter, profileService);

                    var rootContainer = SequencerFactory.GetContainer<SequenceRootContainer>();
                    rootContainer.Name = Loc.Instance["LblAdvancedSequenceTitle"];
                    rootContainer.Add(SequencerFactory.GetContainer<StartAreaContainer>());
                    rootContainer.Add(SequencerFactory.GetContainer<TargetAreaContainer>());
                    rootContainer.Add(SequencerFactory.GetContainer<EndAreaContainer>());
                    rootContainer.ClearHasChanged();

                    Sequencer = new NINA.Sequencer.Sequencer(
                        rootContainer
                    );

                    validationTimer = new DispatcherTimer(DispatcherPriority.Background);
                    validationTimer.Interval = TimeSpan.FromSeconds(5);
                    validationTimer.IsEnabled = true;
                    validationTimer.Tick += (sender, args) => Sequencer.MainContainer.Validate();
                    validationTimer.Start();

                    if (commandLineOptions.SequenceFile == null && File.Exists(profileService.ActiveProfile.SequenceSettings.StartupSequenceTemplate)) {
                        try {
                            LoadSequenceFromFile(profileService.ActiveProfile.SequenceSettings.StartupSequenceTemplate);
                            SavePath = string.Empty;
                            Sequencer.MainContainer.ClearHasChanged();
                        } catch (Exception ex) {
                            Logger.Error("Startup Sequence failed to load", ex);
                        }
                    }

                    if (commandLineOptions.SequenceFile != null) {
                        TryLoadSequenceFile();
                    }
                }));
            });
        }

        private Task<bool> SkipCurrentItem(object arg) {
            Sequencer.MainContainer.SkipCurrentRunningItems();
            return Task.FromResult(true);
        }

        private async Task<bool> SkipToEndOfSequence(object arg) {
            var startContainer = Sequencer.MainContainer.Items[0] as ISequenceContainer;
            var targetContainer = Sequencer.MainContainer.Items[1] as ISequenceContainer;
            if (startContainer.Status == SequenceEntityStatus.RUNNING) {
                await startContainer.Interrupt();
                await Task.Delay(100);
            }
            if (targetContainer.Status == SequenceEntityStatus.RUNNING) {
                await targetContainer.Interrupt();
            }
            return true;
        }

        private void AddTargetToController(object obj) {
            var original = ((obj as DropIntoParameters).Source as IDeepSkyObjectContainer);
            IDeepSkyObjectContainer clonedContainer = original.Clone() as IDeepSkyObjectContainer;

            if (clonedContainer == null) { return; }

            if (TargetController.Targets.Any(t => t.Name == clonedContainer.Name)) {
                if (MyMessageBox.Show(
                    string.Format(Loc.Instance["Lbl_Sequencer_TargetSidebar_OverwriteMessageBox_Text"], clonedContainer.Name),
                    Loc.Instance["Lbl_Sequencer_TargetSidebar_OverwriteMessageBox_Caption"], System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxResult.No
                  ) == System.Windows.MessageBoxResult.Yes) {
                    TargetController.AddTarget(clonedContainer);
                    Notification.ShowSuccess(string.Format(Loc.Instance["Lbl_Sequencer_TargetSidebar_Updated"], clonedContainer.Name));
                }
            } else {
                TargetController.AddTarget(clonedContainer);
                Notification.ShowSuccess(string.Format(Loc.Instance["Lbl_Sequencer_TargetSidebar_Created"], clonedContainer.Name));
            }
        }

        private void AddTemplate(object o) {
            ISequenceContainer clonedContainer = ((o as DropIntoParameters).Source as ISequenceContainer).Clone() as ISequenceContainer;
            if (clonedContainer == null || clonedContainer is ISequenceRootContainer || clonedContainer is IImmutableContainer) return;
            clonedContainer.AttachNewParent(null);
            clonedContainer.ResetAll();
            if (clonedContainer is DeepSkyObjectContainer dsoContainer) {
                dsoContainer.ExposureInfoList.Clear();
            }

            bool addTemplate = true;
            var templateExists = TemplateController.UserTemplates.Any(t => t.Container.Name == clonedContainer.Name);
            if (templateExists) {
                var result = MyMessageBox.Show(string.Format(Loc.Instance["LblTemplate_OverwriteTemplateMessageBox_Text"], clonedContainer.Name),
                    Loc.Instance["LblTemplate_OverwriteTemplateMessageBox_Caption"], System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxResult.Cancel);
                addTemplate = result == System.Windows.MessageBoxResult.OK;
            }

            if (addTemplate) {
                TemplateController.AddNewUserTemplate(clonedContainer);
                if (templateExists) {
                    Notification.ShowSuccess(string.Format(Loc.Instance["LblTemplate_Updated"], clonedContainer.Name));
                } else {
                    Notification.ShowSuccess(string.Format(Loc.Instance["LblTemplate_Created"], clonedContainer.Name));
                }
            }
        }

        private void LoadSequence(object obj) {
            bool isEmpty = true;
            foreach (ISequenceContainer cont in Sequencer.MainContainer.Items) {
                if (cont.Items.Count > 0) {
                    isEmpty = false;
                    break;
                }
            }

            // Don't ask if this is an empty sequence
            if (!isEmpty && Sequencer.MainContainer.AskHasChanged(SavePath ?? "")) {
                return;
            }

            var initialDirectory = string.Empty;
            if (Directory.Exists(profileService.ActiveProfile.SequenceSettings.DefaultSequenceFolder)) {
                initialDirectory = Path.GetFullPath(profileService.ActiveProfile.SequenceSettings.DefaultSequenceFolder);
            }
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Multiselect = false;
            dialog.Title = Loc.Instance["LblLoadSequence"];
            if (string.IsNullOrEmpty(SavePath) || Path.GetExtension(SavePath) != ".json") {
                dialog.InitialDirectory = initialDirectory;
                dialog.FileName = "";
            } else {
                dialog.InitialDirectory = Path.GetDirectoryName(SavePath);
                dialog.FileName = Path.GetFileName(SavePath);
            }
            dialog.DefaultExt = "json";
            dialog.Filter = "N.I.N.A. sequence JSON|*." + dialog.DefaultExt;

            if (dialog.ShowDialog() == true) {
                LoadSequenceFromFile(dialog.FileName);
            }
        }

        private void LoadSequenceFromFile(string file) {
            try {
                var json = File.ReadAllText(file);
                var container = SequenceJsonConverter.Deserialize(json);
                if (container is ISequenceRootContainer root) {
                    SavePath = file;
                    Sequencer.MainContainer = root;
                    Sequencer.MainContainer.Validate();
                    SavePath = file;
                    Sequencer.MainContainer.ClearHasChanged();
                } else if (container != null) {
                    // In case a template or target was selected to load, put it into a new sequence root container
                    var rootContainer = SequencerFactory.GetContainer<SequenceRootContainer>();
                    rootContainer.Name = Loc.Instance["LblAdvancedSequenceTitle"];
                    rootContainer.Add(SequencerFactory.GetContainer<StartAreaContainer>());
                    var targetAreaContainer = SequencerFactory.GetContainer<TargetAreaContainer>();
                    targetAreaContainer.Add(container);
                    rootContainer.Add(targetAreaContainer);
                    rootContainer.Add(SequencerFactory.GetContainer<EndAreaContainer>());
                    rootContainer.ClearHasChanged();

                    // Save path will be empty, as the origin file is not a complete sequencer file
                    SavePath = string.Empty;
                    Sequencer.MainContainer = rootContainer;
                    Sequencer.MainContainer.Validate();
                    Sequencer.MainContainer.ClearHasChanged();

                } else {
                    Logger.Error("Unable to load sequence");
                    Notification.ShowError(Loc.Instance["Lbl_Sequencer_UnableToDeserializeJSON"]);
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(Loc.Instance["Lbl_Sequencer_UnableToDeserializeJSON"]);
            }
        }

        private void TryLoadSequenceFile() {
            string sequenceFile = commandLineOptions.SequenceFile;

            try {
                if (!File.Exists(sequenceFile)) {
                    Logger.Error($"Auto load sequence file not found: {sequenceFile}");
                    return;
                }

                Logger.Info($"Loading sequence file: {sequenceFile}");
                LoadSequenceFromFile(sequenceFile);
                SavePath = string.Empty;
                Sequencer.MainContainer.ClearHasChanged();

                if (commandLineOptions.RunSequence) {
                    Logger.Info("Starting sequence from command line options");
                    _ = StartSequenceCommand.ExecuteAsync(commandLineOptions.RunSequence);
                    applicationMediator.ChangeTab(ApplicationTab.IMAGING);
                }
            } catch (Exception ex) {
                Logger.Error(ex);
            }
        }

        private void SaveAsSequence(object arg) {
            try {
                var initialDirectory = string.Empty;
                if (Directory.Exists(profileService.ActiveProfile.SequenceSettings.DefaultSequenceFolder)) {
                    initialDirectory = Path.GetFullPath(profileService.ActiveProfile.SequenceSettings.DefaultSequenceFolder);
                }
                Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
                dialog.Title = Loc.Instance["LblSave"];
                if (string.IsNullOrEmpty(SavePath) || Path.GetExtension(SavePath) != ".json") {
                    dialog.InitialDirectory = initialDirectory;
                    dialog.FileName = Sequencer.MainContainer.Name;
                } else {
                    dialog.InitialDirectory = Path.GetDirectoryName(SavePath);
                    dialog.FileName = Path.GetFileName(SavePath);
                }
                dialog.DefaultExt = "json";
                dialog.Filter = "N.I.N.A. sequence JSON|*." + dialog.DefaultExt;
                dialog.OverwritePrompt = true;

                if (dialog.ShowDialog().Value) {
                    var json = SequenceJsonConverter.Serialize(Sequencer.MainContainer);
                    File.WriteAllText(dialog.FileName, json);
                    SavePath = dialog.FileName;
                    Sequencer.MainContainer.ClearHasChanged();
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(string.Format(Loc.Instance["Lbl_Sequencer_SaveSequence_FailureNotification"], Sequencer.MainContainer.Name, ex.Message));
            }
        }

        private void SaveSequence(object arg) {
            if (string.IsNullOrEmpty(SavePath)) {
                SaveAsSequence(arg);
            } else {
                try {
                    var json = SequenceJsonConverter.Serialize(Sequencer.MainContainer);
                    File.WriteAllText(SavePath, json);
                    Sequencer.MainContainer.ClearHasChanged();
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(string.Format(Loc.Instance["Lbl_Sequencer_SaveSequence_FailureNotification"], Sequencer.MainContainer.Name, ex.Message));
                    return;
                }
            }
            if (!string.IsNullOrEmpty(SavePath)) {
                Notification.ShowSuccess(string.Format(Loc.Instance["Lbl_Sequencer_SaveSequence_Notification"], Sequencer.MainContainer.Name, SavePath));
            }
        }

        public ISequencerFactory SequencerFactory { get; }

        public TemplateController TemplateController { get; private set; }
        public TargetController TargetController { get; private set; }

        public SequenceJsonConverter SequenceJsonConverter { get; private set; }

        private bool isRunning;

        public bool IsRunning {
            get => isRunning;
            set {
                isRunning = value;
                RaisePropertyChanged();
            }
        }

        private TaskbarItemProgressState taskBarProgressState = TaskbarItemProgressState.None;

        public TaskbarItemProgressState TaskBarProgressState {
            get => taskBarProgressState;
            set {
                taskBarProgressState = value;
                RaisePropertyChanged();
            }
        }

        private ApplicationStatus _status;

        public ApplicationStatus Status {
            get => _status;
            set {
                _status = value;
                if (string.IsNullOrWhiteSpace(_status.Source)) {
                    _status.Source = Loc.Instance["LblSequence"];
                }

                RaisePropertyChanged();

                applicationStatusMediator.StatusUpdate(_status);
            }
        }

        private CancellationTokenSource cts;

        public async Task<bool> StartSequence(object arg) {
            bool skipValidationPrompt = false;
            if (arg != null && arg is bool b) {
                skipValidationPrompt = b;
            }
            cts?.Dispose();
            cts = new CancellationTokenSource();
            var token = cts.Token;
            IsRunning = true;
            TaskBarProgressState = TaskbarItemProgressState.Normal;
            try {
                cameraMediator.RegisterCaptureBlock(this);

                //Set base containers to created to rerun
                Sequencer.MainContainer.Status = SequenceEntityStatus.CREATED;
                Sequencer.MainContainer.Items[0].Status = SequenceEntityStatus.CREATED;
                Sequencer.MainContainer.Items[1].Status = SequenceEntityStatus.CREATED;
                Sequencer.MainContainer.Items[2].Status = SequenceEntityStatus.CREATED;

                Logger.Info("Advanced Sequence starting");
                await (SequenceStarting?.InvokeAsync(this, new EventArgs()) ?? Task.CompletedTask);
                await Sequencer.Start(new Progress<ApplicationStatus>(p => Status = p), token, skipValidationPrompt);
                return true;
            } finally {
                Logger.Info("Advanced Sequence finished");
                cameraMediator.ReleaseCaptureBlock(this);
                TaskBarProgressState = TaskbarItemProgressState.None;
                IsRunning = false;
                await (SequenceFinished?.InvokeAsync(this, new EventArgs()) ?? Task.CompletedTask);
                if (commandLineOptions.ExitAfterSequence) {
                    Logger.Info("Quitting application after sequence is finished");
                    Application.Current.Shutdown();
                }
            }
        }

        private void CancelSequence(object obj) {
            try { cts?.Cancel(); } catch { }
        }

        public IList<IDeepSkyObjectContainer> GetDeepSkyObjectContainerTemplates() {
            var emptyContainer = SequencerFactory.GetContainer<DeepSkyObjectContainer>();
            var templates = TemplateController.Templates.Where(x => x.Container is IDeepSkyObjectContainer).Select(y => y.Container as IDeepSkyObjectContainer).ToList();

            var items = new List<IDeepSkyObjectContainer>();
            items.Add(emptyContainer);
            items.AddRange(templates);
            return items;
        }

        public void AddTarget(IDeepSkyObjectContainer container) {
            (this.Sequencer.MainContainer.Items[1] as ISequenceContainer).Add(container);
        }

        public void AddTargetToTargetList(IDeepSkyObjectContainer container) {
            var d = new DropIntoParameters(container);
            d.Position = DropTargetEnum.Center;
            AddTargetToController(d);
        }

        [JsonProperty]
        public string StartSequenceTooltip {
            get {
                if (cameraMediator.IsFreeToCapture(this)) return Loc.Instance["LblTooltipStartSequence"];
                return Loc.Instance["LblCameraBusy"];
            }
        }

        public void UpdateDeviceInfo(CameraInfo deviceInfo) {
            RaisePropertyChanged("StartSequenceTooltip");
        }

        public async Task SaveContainer(ISequenceContainer content, string filePath, CancellationToken token) {            
            var json = SequenceJsonConverter.Serialize(content);
            await File.WriteAllTextAsync(filePath, json, token);
        }

        public string GetAdvancedSequencerSavePath() {
            return SavePath;
        }

        public ICommand AddTemplateCommand { get; private set; }
        public ICommand AddTargetToControllerCommand { get; private set; }

        public ICommand DetachCommand { get; set; }
        public IAsyncCommand StartSequenceCommand { get; private set; }
        public IAsyncCommand SkipCurrentItemCommand { get; private set; }
        public IAsyncCommand SkipToEndOfSequenceCommand { get; private set; }
        public ICommand CancelSequenceCommand { get; private set; }

        public ICommand LoadSequenceCommand { get; }
        public ICommand SwitchToOverviewCommand { get; }
        public ICommand SaveSequenceCommand { get; }
        public ICommand SaveAsSequenceCommand { get; }
    }
}