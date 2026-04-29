#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Validations;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace NINA.Sequencer.Container {

    [ExportMetadata("Name", "Lbl_SequenceContainer_LinkedTemplateContainer_Name")]
    [ExportMetadata("Description", "Lbl_SequenceContainer_LinkedTemplateContainer_Description")]
    [ExportMetadata("Icon", "ConnectSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Container")]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]
    public class LinkedTemplateContainer : SequenceContainer, ISequenceItemPlacementTarget {
        private const string LinkedTemplateIconResourceKey = "ConnectSVG";
        private readonly ITemplateLinkResolver templateLinkResolver;
        private TemplateReference templateReference = new TemplateReference();
        private TemplateLinkState linkState = TemplateLinkState.Pending;
        private bool isEditing;
        private LinkedTemplateTargetOverride targetOverride;
        private InputTarget targetEditor;
        private InputTarget observedMaterializedTarget;
        private bool suppressTargetEditorUpdate;
        private bool suppressMaterializedTargetUpdate;
        private bool isDeserializing;
        private bool resolvedTemplateSupportsTargetOverride;

        [ImportingConstructor]
        public LinkedTemplateContainer(ITemplateLinkResolver templateLinkResolver) : base(new SequentialStrategy()) {
            this.templateLinkResolver = templateLinkResolver;
            IsExpanded = false;
            Name = Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_Name"];
            Description = Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_Description"];
            Category = Loc.Instance["Lbl_SequenceCategory_Container"];
            Icon = TryGetDefaultIcon();
            BeginEditTemplateCommand = new GalaSoft.MvvmLight.Command.RelayCommand(BeginEditTemplate, () => CanEditTemplate && !IsEditing);
            CancelEditTemplateCommand = new GalaSoft.MvvmLight.Command.RelayCommand(CancelEditTemplate, () => IsEditing);
            SaveTemplateCommand = new AsyncCommand<bool>(SaveTemplate, (object o) => CanSaveTemplate);
            DropTargetCommand = new GalaSoft.MvvmLight.Command.RelayCommand<object>(DropTarget);
            EnsureTargetEditor();
        }

        public LinkedTemplateContainer() : this(null) {
        }

        [OnDeserializing]
        public void OnLinkedTemplateContainerDeserializing(StreamingContext context) {
            isDeserializing = true;
        }

        [OnDeserialized]
        public void OnLinkedTemplateContainerDeserialized(StreamingContext context) {
            isDeserializing = false;
            TemplateReference ??= new TemplateReference();
            ClearMaterializedTemplate();
            resolvedTemplateSupportsTargetOverride = false;
            base.IsExpanded = false;
            LinkState = TemplateReference.IsValid ? TemplateLinkState.Pending : TemplateLinkState.Invalid;
        }

        [JsonProperty]
        public TemplateReference TemplateReference {
            get => templateReference;
            set {
                templateReference = value ?? new TemplateReference();
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(SourceTemplateName));
            }
        }

        [JsonProperty]
        public TemplateLinkState LinkState {
            get => linkState;
            private set {
                linkState = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(LinkStatusText));
                RaisePropertyChanged(nameof(CanEditTemplate));
                RaisePropertyChanged(nameof(CanSaveTemplate));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool IsEditing {
            get => isEditing;
            private set {
                isEditing = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(LinkStatusText));
                RaisePropertyChanged(nameof(HeaderText));
                RaisePropertyChanged(nameof(CanEditTemplate));
                RaisePropertyChanged(nameof(CanSaveTemplate));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string HeaderText => IsEditing
            ? Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_HeaderEditing"]
            : Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_HeaderReadonly"];

        public string SourceTemplateName => string.IsNullOrWhiteSpace(TemplateReference?.DisplayName)
            ? Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_Name"]
            : TemplateReference.DisplayName;

        public string LinkStatusText {
            get {
                if (IsEditing) {
                    return Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_StatusEditing"];
                }

                switch (LinkState) {
                    case TemplateLinkState.Resolved:
                        return string.Format(Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_StatusReadonly"], SourceTemplateName);
                    case TemplateLinkState.Loading:
                    case TemplateLinkState.Pending:
                        return string.Format(Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_StatusLoading"], SourceTemplateName);
                    case TemplateLinkState.Missing:
                        return string.Format(Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_StatusMissing"], SourceTemplateName);
                    default:
                        return Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_StatusInvalid"];
                }
            }
        }

        public bool CanEditTemplate => LinkState == TemplateLinkState.Resolved
            && TemplateReference?.SourceKind == TemplateReferenceSourceKind.User;

        public bool CanSaveTemplate => IsEditing && CanEditTemplate && Items.OfType<ISequenceContainer>().Count() == 1;

        public bool IsMaterialized => Items.Count > 0;

        [JsonProperty]
        public override bool IsExpanded {
            get => base.IsExpanded;
            set {
                bool wasExpanded = base.IsExpanded;
                base.IsExpanded = value;
                if (!isDeserializing && value && !wasExpanded && !IsEditing) {
                    TryResolveTemplate();
                }
            }
        }

        [JsonProperty]
        public LinkedTemplateTargetOverride TargetOverride {
            get => targetOverride;
            set => SetTargetOverride(value, true);
        }

        public InputTarget TargetEditor => EnsureTargetEditor();

        public bool SupportsTargetOverride => resolvedTemplateSupportsTargetOverride || GetMaterializedDeepSkyObjectContainer() != null;

        public bool HasTargetOverride => HasTarget(TargetOverride);

        public string TargetStatusText => SupportsTargetOverride
            ? (HasTargetOverride
                ? string.Format(Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_TargetSelected"], GetTargetDisplayText(TargetOverride))
                : Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_TargetMissing"])
            : string.Empty;

        public ICommand BeginEditTemplateCommand { get; }
        public ICommand CancelEditTemplateCommand { get; }
        public ICommand DropTargetCommand { get; }
        public IAsyncCommand SaveTemplateCommand { get; }

        public bool CanAcceptSequenceItemPlacement => false;

        private static GeometryGroup TryGetDefaultIcon() {
            Application application = Application.Current;
            if (application?.Dispatcher.CheckAccess() == true && application.Resources.Contains(LinkedTemplateIconResourceKey)) {
                return application.Resources[LinkedTemplateIconResourceKey] as GeometryGroup;
            }

            return null;
        }

        private void UpdateReferenceFromTemplate(TemplatedSequenceContainer template) {
            TemplateReference = template.Reference?.Clone() ?? TemplateReference;
            TemplateReference.DisplayName = template.Container.Name;
            Name = string.Format(Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_DisplayName"], template.Container.Name);
            resolvedTemplateSupportsTargetOverride = template.Container is IDeepSkyObjectContainer;
            LinkState = TemplateLinkState.Resolved;
            RaisePropertyChanged(nameof(SourceTemplateName));
            RaiseTargetPropertiesChanged();
        }

        public void MaterializeFromTemplate(TemplatedSequenceContainer template, bool? isExpanded = null) {
            if (template?.Container == null) {
                LinkState = TemplateLinkState.Invalid;
                return;
            }

            if (isExpanded.HasValue) {
                base.IsExpanded = isExpanded.Value;
            }

            UpdateReferenceFromTemplate(template);
            ISequenceContainer materializedTemplate = (ISequenceContainer)template.Clone();
            ApplyTargetOverride(materializedTemplate);
            materializedTemplate.AttachNewParent(this);
            Items = new ObservableCollection<ISequenceItem> { materializedTemplate };
            ObserveMaterializedTarget(GetMaterializedDeepSkyObjectContainer());
            RaisePropertyChanged(nameof(Items));
            RaisePropertyChanged(nameof(IsMaterialized));
            RaiseTargetPropertiesChanged();
        }

        public bool TryResolveTemplate() {
            return TryResolveTemplate(true);
        }

        public bool RefreshLinkState() {
            return TryResolveTemplate(false);
        }

        private bool TryResolveTemplate(bool materialize) {
            if (TemplateReference == null || !TemplateReference.IsValid) {
                resolvedTemplateSupportsTargetOverride = false;
                RaiseTargetPropertiesChanged();
                LinkState = TemplateLinkState.Invalid;
                return false;
            }

            if (templateLinkResolver == null) {
                LinkState = Items.Count > 0 ? TemplateLinkState.Pending : TemplateLinkState.Invalid;
                return false;
            }

            if (templateLinkResolver.TryResolve(TemplateReference, out TemplatedSequenceContainer template)) {
                if (materialize && !IsEditing) {
                    MaterializeFromTemplate(template);
                } else {
                    UpdateReferenceFromTemplate(template);
                }
                return true;
            }

            LinkState = templateLinkResolver.InitialLoadComplete ? TemplateLinkState.Missing : TemplateLinkState.Loading;
            if (templateLinkResolver.InitialLoadComplete) {
                resolvedTemplateSupportsTargetOverride = false;
                RaiseTargetPropertiesChanged();
            }
            return false;
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            await EnsureResolved(token);
            await base.Execute(progress, token);
        }

        public override bool Validate() {
            Issues.Clear();
            if (TemplateReference == null || !TemplateReference.IsValid) {
                Issues.Add(Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_StatusInvalid"]);
                return false;
            }

            if (LinkState != TemplateLinkState.Resolved) {
                RefreshLinkState();
            }

            if (LinkState != TemplateLinkState.Resolved) {
                Issues.Add(LinkStatusText);
                return false;
            }

            if (!IsMaterialized) {
                return true;
            }

            bool valid = base.Validate();

            if (SupportsTargetOverride && !HasTargetOverride) {
                Issues.Add(Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_TargetRequired"]);
                valid = false;
            }

            return valid;
        }

        public override object Clone() {
            LinkedTemplateContainer clone = new LinkedTemplateContainer(templateLinkResolver) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
                IsExpanded = IsExpanded,
                TemplateReference = TemplateReference?.Clone() ?? new TemplateReference(),
                TargetOverride = TargetOverride?.Clone(),
                resolvedTemplateSupportsTargetOverride = resolvedTemplateSupportsTargetOverride,
                LinkState = LinkState,
                Items = new ObservableCollection<ISequenceItem>(Items.Select(i => i.Clone() as ISequenceItem)),
                Triggers = new ObservableCollection<ISequenceTrigger>(Triggers.Select(t => t.Clone() as ISequenceTrigger)),
                Conditions = new ObservableCollection<ISequenceCondition>(Conditions.Select(c => c.Clone() as ISequenceCondition))
            };

            foreach (ISequenceItem item in clone.Items) {
                item.AttachNewParent(clone);
            }

            clone.ObserveMaterializedTarget(clone.GetMaterializedDeepSkyObjectContainer());

            foreach (ISequenceCondition condition in clone.Conditions) {
                condition.AttachNewParent(clone);
            }

            foreach (ISequenceTrigger trigger in clone.Triggers) {
                trigger.AttachNewParent(clone);
            }

            return clone;
        }

        private void DropTarget(object obj) {
            if (!TryGetDropTarget(obj, out InputTarget target)) {
                return;
            }

            TargetOverride = LinkedTemplateTargetOverride.FromInputTarget(target);
        }

        private InputTarget EnsureTargetEditor() {
            if (targetEditor == null) {
                targetEditor = CreateTargetEditor(TargetOverride);
                targetEditor.CoordinatesChanged += TargetEditor_OnCoordinatesChanged;
            }

            return targetEditor;
        }

        private static InputTarget CreateTargetEditor(LinkedTemplateTargetOverride targetOverride) {
            InputTarget editor = new InputTarget(Angle.Zero, Angle.Zero, null);
            if (targetOverride != null) {
                CopyTarget(targetOverride, editor);
            } else {
                ClearTarget(editor);
            }

            return editor;
        }

        private void TargetEditor_OnCoordinatesChanged(object sender, EventArgs e) {
            if (suppressTargetEditorUpdate) {
                return;
            }

            SetTargetOverride(LinkedTemplateTargetOverride.FromInputTarget(TargetEditor), false);
        }

        private void MaterializedTarget_OnCoordinatesChanged(object sender, EventArgs e) {
            if (suppressMaterializedTargetUpdate || observedMaterializedTarget == null) {
                return;
            }

            SetTargetOverride(LinkedTemplateTargetOverride.FromInputTarget(observedMaterializedTarget), true, false);
            SynchronizeMaterializedTargetName();
        }

        private void SetTargetOverride(LinkedTemplateTargetOverride value, bool updateTargetEditor, bool applyToMaterializedTemplate = true) {
            targetOverride = value?.Clone();

            if (updateTargetEditor) {
                UpdateTargetEditorFromOverride();
            }

            if (applyToMaterializedTemplate) {
                ApplyTargetOverrideToMaterializedTemplate();
            }

            RaiseTargetPropertiesChanged();
        }

        private void UpdateTargetEditorFromOverride() {
            InputTarget editor = EnsureTargetEditor();
            suppressTargetEditorUpdate = true;
            try {
                if (TargetOverride != null) {
                    CopyTarget(TargetOverride, editor, true);
                } else {
                    ClearTarget(editor, true);
                }
            } finally {
                suppressTargetEditorUpdate = false;
            }

            RaisePropertyChanged(nameof(TargetEditor));
        }

        private bool TryGetDropTarget(object obj, out InputTarget target) {
            target = null;
            if (!SupportsTargetOverride) {
                return false;
            }

            object source = obj is DropIntoParameters parameters ? parameters.Source : obj;
            if (source is TargetSequenceContainer targetSequenceContainer) {
                target = targetSequenceContainer.Container?.Target;
                return target != null;
            }

            if (source is IDeepSkyObjectContainer deepSkyObjectContainer) {
                target = deepSkyObjectContainer.Target;
                return target != null;
            }

            return false;
        }

        private void ApplyTargetOverrideToMaterializedTemplate() {
            IDeepSkyObjectContainer deepSkyObjectContainer = GetMaterializedDeepSkyObjectContainer();
            if (deepSkyObjectContainer == null) {
                return;
            }

            suppressMaterializedTargetUpdate = true;
            try {
                ApplyTargetOverride(deepSkyObjectContainer);
            } finally {
                suppressMaterializedTargetUpdate = false;
            }
        }

        private void ApplyTargetOverride(ISequenceContainer materializedTemplate) {
            if (materializedTemplate is IDeepSkyObjectContainer deepSkyObjectContainer) {
                ApplyTargetOverride(deepSkyObjectContainer);
            }
        }

        private void ApplyTargetOverride(IDeepSkyObjectContainer deepSkyObjectContainer) {
            if (deepSkyObjectContainer?.Target == null) {
                return;
            }

            if (HasTargetOverride) {
                CopyTarget(TargetOverride, deepSkyObjectContainer.Target, true);
                SynchronizeMaterializedTargetName();
            } else {
                ClearTarget(deepSkyObjectContainer.Target, true);
            }
        }

        private IDeepSkyObjectContainer GetMaterializedDeepSkyObjectContainer() {
            return Items.OfType<IDeepSkyObjectContainer>().FirstOrDefault();
        }

        private void SynchronizeMaterializedTargetName() {
            IDeepSkyObjectContainer deepSkyObjectContainer = GetMaterializedDeepSkyObjectContainer();
            if (deepSkyObjectContainer is ISequenceContainer sequenceContainer
                && !string.IsNullOrWhiteSpace(deepSkyObjectContainer.Target?.TargetName)) {
                sequenceContainer.Name = deepSkyObjectContainer.Target.TargetName;
            }
        }

        private void ObserveMaterializedTarget(IDeepSkyObjectContainer deepSkyObjectContainer) {
            if (observedMaterializedTarget != null) {
                WeakEventManager<InputTarget, EventArgs>.RemoveHandler(observedMaterializedTarget, nameof(InputTarget.CoordinatesChanged), MaterializedTarget_OnCoordinatesChanged);
                observedMaterializedTarget = null;
            }

            observedMaterializedTarget = deepSkyObjectContainer?.Target;
            if (observedMaterializedTarget != null) {
                WeakEventManager<InputTarget, EventArgs>.AddHandler(observedMaterializedTarget, nameof(InputTarget.CoordinatesChanged), MaterializedTarget_OnCoordinatesChanged);
            }
        }

        private static bool HasTarget(LinkedTemplateTargetOverride target) {
            return target != null
                && (!string.IsNullOrWhiteSpace(target.TargetName)
                    || HasCoordinates(target.InputCoordinates));
        }

        private static bool HasCoordinates(InputCoordinates inputCoordinates) {
            Coordinates coordinates = inputCoordinates?.Coordinates;
            return coordinates != null
                && (Math.Abs(coordinates.RA) > double.Epsilon || Math.Abs(coordinates.Dec) > double.Epsilon);
        }

        private static string GetTargetDisplayText(LinkedTemplateTargetOverride target) {
            if (!string.IsNullOrWhiteSpace(target?.TargetName)) {
                return target.TargetName;
            }

            if (HasCoordinates(target?.InputCoordinates)) {
                return target.InputCoordinates.ToString();
            }

            return Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_TargetMissing"];
        }

        private static void CopyTarget(LinkedTemplateTargetOverride source, InputTarget target, bool preserveInputCoordinatesInstance = false) {
            if (source == null || target == null) {
                return;
            }

            target.TargetName = source.TargetName ?? string.Empty;
            CopyCoordinates(source.InputCoordinates, target, preserveInputCoordinatesInstance);
            target.PositionAngle = source.PositionAngle;
        }

        private static void CopyCoordinates(InputCoordinates source, InputTarget target, bool preserveInputCoordinatesInstance) {
            InputCoordinates copy = source?.Clone() ?? new InputCoordinates();
            if (preserveInputCoordinatesInstance && target.InputCoordinates != null) {
                target.InputCoordinates.Coordinates = copy.Coordinates;
                return;
            }

            target.InputCoordinates = copy;
        }

        private static void ClearTarget(InputTarget target, bool preserveInputCoordinatesInstance = false) {
            target.TargetName = string.Empty;
            CopyCoordinates(new InputCoordinates(), target, preserveInputCoordinatesInstance);
            target.PositionAngle = 0;
        }

        private void RaiseTargetPropertiesChanged() {
            RaisePropertyChanged(nameof(TargetOverride));
            RaisePropertyChanged(nameof(SupportsTargetOverride));
            RaisePropertyChanged(nameof(HasTargetOverride));
            RaisePropertyChanged(nameof(TargetStatusText));
        }

        private async Task EnsureResolved(CancellationToken token) {
            if (templateLinkResolver == null) {
                throw new SequenceEntityFailedException(Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_StatusInvalid"]);
            }

            LinkState = TemplateLinkState.Loading;
            await templateLinkResolver.WaitForInitialLoad(token);
            if (!TryResolveTemplate()) {
                throw new SequenceEntityFailedException(LinkStatusText);
            }
        }

        private void BeginEditTemplate() {
            if (!CanEditTemplate) {
                return;
            }

            if (!IsMaterialized && !TryResolveTemplate()) {
                return;
            }

            IsEditing = true;
        }

        private async Task<bool> SaveTemplate(object arg) {
            if (!CanSaveTemplate) {
                return false;
            }

            ISequenceContainer templateContainer = Items.OfType<ISequenceContainer>().Single();
            try {
                await templateLinkResolver.SaveTemplate(TemplateReference, templateContainer, CancellationToken.None);
                IsEditing = false;
                TryResolveTemplate();
                Notification.ShowSuccess(string.Format(Loc.Instance["LblTemplate_Updated"], SourceTemplateName));
                return true;
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
                return false;
            }
        }

        private void CancelEditTemplate() {
            IsEditing = false;
            TryResolveTemplate();
        }

        private void ClearMaterializedTemplate() {
            if (Items.Count == 0) {
                ObserveMaterializedTarget(null);
                return;
            }

            foreach (ISequenceItem item in Items.ToArray()) {
                item.AttachNewParent(null);
            }

            Items = new ObservableCollection<ISequenceItem>();
            ObserveMaterializedTarget(null);
            RaisePropertyChanged(nameof(Items));
            RaisePropertyChanged(nameof(IsMaterialized));
            RaiseTargetPropertiesChanged();
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class LinkedTemplateTargetOverride {

        [JsonProperty]
        public string TargetName { get; set; } = string.Empty;

        [JsonProperty]
        public InputCoordinates InputCoordinates { get; set; } = new InputCoordinates();

        [JsonProperty]
        public double PositionAngle { get; set; }

        public LinkedTemplateTargetOverride Clone() {
            return new LinkedTemplateTargetOverride {
                TargetName = TargetName,
                InputCoordinates = InputCoordinates?.Clone() ?? new InputCoordinates(),
                PositionAngle = PositionAngle
            };
        }

        public static LinkedTemplateTargetOverride FromInputTarget(InputTarget source) {
            if (source == null) {
                return null;
            }

            return new LinkedTemplateTargetOverride {
                TargetName = source.TargetName,
                InputCoordinates = source.InputCoordinates?.Clone() ?? new InputCoordinates(),
                PositionAngle = source.PositionAngle
            };
        }
    }
}
