#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Serialization;
using NINA.Core.Utility;
using NINA.Astrometry;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using NINA.Core.Utility.Notification;
using NINA.Core.Locale;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NINA.Sequencer {

    public partial class TemplateController : BaseINPC {
        private readonly SequenceJsonConverter sequenceJsonConverter;
        private readonly IProfileService profileService;
        private readonly ITemplateLinkResolver templateLinkResolver;
        private readonly string defaultTemplatePath;
        private FileSystemWatcher sequenceTemplateFolderWatcher;
        private string userTemplatePath;
        private readonly SemaphoreSlim loadUserTemplatesSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim saveLinkedTemplateSemaphore = new SemaphoreSlim(1, 1);
        public const string DefaultTemplatesGroup = "LblTemplate_DefaultTemplates";
        private const string UserTemplatesGroup = "LblTemplate_UserTemplates";
        public const string TemplateFileExtension = ".template.json";
        private ISequenceSettings activeSequenceSettings;

        public IList<TemplatedSequenceContainer> UserTemplates => Templates.Where(t => t.Group == UserTemplatesGroup).ToList();

        public IList<TemplatedSequenceContainer> Templates { get; }

        private CollectionViewSource templatesView;
        private CollectionViewSource templatesMenuView;
        public ICollectionView TemplatesView => templatesView.View;
        public ICollectionView TemplatesMenuView => templatesMenuView.View;

        private string viewFilter = string.Empty;

        public string ViewFilter {
            get => viewFilter;
            set {
                viewFilter = value;
                TemplatesView.Refresh();
            }
        }

        [ObservableProperty]
        private bool templatesLoading = true;
        [ObservableProperty]
        private int templatesLoadingProgress;
        [ObservableProperty]
        private int templatesLoadingTotalCount;

        public TemplateController(SequenceJsonConverter sequenceJsonConverter, IProfileService profileService, ITemplateLinkResolver templateLinkResolver = null) {
            this.sequenceJsonConverter = sequenceJsonConverter;
            this.profileService = profileService;
            this.templateLinkResolver = templateLinkResolver;
            defaultTemplatePath = Path.Combine(NINA.Core.Utility.CoreUtil.APPLICATIONDIRECTORY, "Sequencer", "Examples");

            Templates = new List<TemplatedSequenceContainer>();
            try {
                if (!Directory.Exists(defaultTemplatePath)) {
                    Directory.CreateDirectory(defaultTemplatePath);
                }
                foreach (var file in Directory.GetFiles(defaultTemplatePath, "*" + TemplateFileExtension)) {
                    try {
                        var container = sequenceJsonConverter.Deserialize(File.ReadAllText(file), file);
                        if (container is ISequenceRootContainer) continue;
                        TemplateReference reference = CreateTemplateReference(TemplateReferenceSourceKind.Default, defaultTemplatePath, file, container.Name);
                        Templates.Add(new TemplatedSequenceContainer(profileService, DefaultTemplatesGroup, container, reference, templateLinkResolver));
                    } catch (Exception ex) {
                        Logger.Error("Invalid template JSON", ex);
                    }
                }
            } catch (Exception ex) {
                Logger.Error("Error occurred while loading default templates", ex);
            }

            templateLinkResolver?.UpdateTemplates(Templates.ToList(), false, SaveLinkedTemplate);

            templatesView = new CollectionViewSource { Source = Templates };
            TemplatesView.GroupDescriptions.Add(new PropertyGroupDescription("GroupTranslated"));
            TemplatesView.SortDescriptions.Add(new SortDescription("GroupTranslated", ListSortDirection.Ascending));
            TemplatesView.SortDescriptions.Add(new SortDescription("Container.Name", ListSortDirection.Ascending));
            TemplatesView.Filter += new Predicate<object>(ApplyViewFilter);

            templatesMenuView = new CollectionViewSource { Source = Templates };
            TemplatesMenuView.SortDescriptions.Add(new SortDescription("Container.Name", ListSortDirection.Ascending));

            LoadUserTemplates().ContinueWith(t => {
                sequenceTemplateFolderWatcher = new FileSystemWatcher(profileService.ActiveProfile.SequenceSettings.SequencerTemplatesFolder, "*" + TemplateFileExtension);
                sequenceTemplateFolderWatcher.Changed += SequenceTemplateFolderWatcher_Changed;
                sequenceTemplateFolderWatcher.Deleted += SequenceTemplateFolderWatcher_Changed;
                sequenceTemplateFolderWatcher.IncludeSubdirectories = true;
                sequenceTemplateFolderWatcher.EnableRaisingEvents = true;

                profileService.ProfileChanged += ProfileService_ProfileChanged;
                activeSequenceSettings = profileService.ActiveProfile.SequenceSettings;
                activeSequenceSettings.PropertyChanged += SequenceSettings_SequencerTemplatesFolderChanged;
            });
        }

        private bool ApplyViewFilter(object obj) {
            return (obj as TemplatedSequenceContainer).Container.Name.IndexOf(ViewFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private async void SequenceTemplateFolderWatcher_Changed(object sender, FileSystemEventArgs e) {
            try {
                sequenceTemplateFolderWatcher.EnableRaisingEvents = false;
                await LoadUserTemplates();
            } finally {
                sequenceTemplateFolderWatcher.EnableRaisingEvents = true;
            }
        }

        private async void SequenceSettings_SequencerTemplatesFolderChanged(object sender, System.EventArgs e) {
            if ((e as PropertyChangedEventArgs)?.PropertyName == nameof(profileService.ActiveProfile.SequenceSettings.SequencerTemplatesFolder)) {
                if (!Directory.Exists(profileService.ActiveProfile.SequenceSettings.SequencerTemplatesFolder)) { return; }

                sequenceTemplateFolderWatcher.Path = profileService.ActiveProfile.SequenceSettings.SequencerTemplatesFolder;
                try {
                    sequenceTemplateFolderWatcher.EnableRaisingEvents = false;
                    await LoadUserTemplates();
                } finally {
                    sequenceTemplateFolderWatcher.EnableRaisingEvents = true;
                }
            }
        }

        private async void ProfileService_ProfileChanged(object sender, System.EventArgs e) {
            activeSequenceSettings.PropertyChanged -= SequenceSettings_SequencerTemplatesFolderChanged;
            activeSequenceSettings = profileService.ActiveProfile.SequenceSettings;
            activeSequenceSettings.PropertyChanged += SequenceSettings_SequencerTemplatesFolderChanged;
            try {
                sequenceTemplateFolderWatcher.EnableRaisingEvents = false;
                await LoadUserTemplates();
            } finally {
                sequenceTemplateFolderWatcher.EnableRaisingEvents = true;
            }
        }

        private Task LoadUserTemplates() {
            return LoadUserTemplatesSerialized();
        }

        private async Task LoadUserTemplatesSerialized() {
            await loadUserTemplatesSemaphore.WaitAsync();
            try {
                await Task.Run(async () => {
                    try {
                        TemplatesLoading = true;
                        userTemplatePath = profileService.ActiveProfile.SequenceSettings.SequencerTemplatesFolder;
                        var rootParts = userTemplatePath.Split(new char[] { Path.DirectorySeparatorChar }, System.StringSplitOptions.RemoveEmptyEntries);

                        if (!Directory.Exists(userTemplatePath)) {
                            Directory.CreateDirectory(userTemplatePath);
                        }

                        foreach (var template in Templates.Where(t => t.Group != DefaultTemplatesGroup).ToList()) {
                            await Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => Templates.Remove(template)));
                        }
                        TemplatesLoadingProgress = 0;
                        TemplatesLoadingTotalCount = 1;

                        var files = Directory.GetFiles(userTemplatePath, "*" + TemplateFileExtension, SearchOption.AllDirectories);
                        TemplatesLoadingTotalCount = files.Length;

                        foreach (var file in files) {
                            try {
                                var container = sequenceJsonConverter.Deserialize(File.ReadAllText(file), file);
                                if (container is ISequenceRootContainer) continue;
                                var fileInfo = new FileInfo(file);
                                container.Name = fileInfo.Name.Replace(TemplateFileExtension, "");
                                TemplateReference reference = CreateTemplateReference(TemplateReferenceSourceKind.User, userTemplatePath, file, container.Name);
                                var template = new TemplatedSequenceContainer(profileService, UserTemplatesGroup, container, reference, templateLinkResolver);
                                var parts = fileInfo.Directory.FullName.Split(new char[] { Path.DirectorySeparatorChar }, System.StringSplitOptions.RemoveEmptyEntries);
                                template.SubGroups = parts.Except(rootParts).ToArray();
                                Templates.Add(template);
                            } catch (Exception ex) {
                                Logger.Error("Invalid template JSON", ex);
                            }
                            TemplatesLoadingProgress++;
                        }

                        await Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => {
                            try {
                                TemplatesView.Refresh();
                                TemplatesMenuView.Refresh();
                            } catch (Exception ex) {
                                Logger.Error(ex);
                            }
                        }));
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        Notification.ShowError(Loc.Instance["Lbl_SequenceTemplateController_LoadUserTemplatesFailed"]);
                    } finally {
                        TemplatesLoading = false;
                        templateLinkResolver?.UpdateTemplates(Templates.ToList(), true, SaveLinkedTemplate);
                    }
                });
            } finally {
                loadUserTemplatesSemaphore.Release();
            }
        }

        public void AddNewUserTemplate(ISequenceContainer sequenceContainer) {
            try {
                var existingTemplate = UserTemplates.FirstOrDefault(t => t.Container.Name == sequenceContainer.Name);

                PrepareDeepSkyObjectTemplate(sequenceContainer);

                var path = existingTemplate == null ? userTemplatePath : Path.Combine(userTemplatePath, Path.Combine(existingTemplate.SubGroups));

                var jsonContainer = sequenceJsonConverter.Serialize(sequenceContainer);
                File.WriteAllText(Path.Combine(path, GetTemplateFileName(sequenceContainer)), jsonContainer);
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(Loc.Instance["Lbl_SequenceTemplateController_AddNewTemplateFailed"]);
            }
        }

        public void DeleteUserTemplate(TemplatedSequenceContainer sequenceContainer) {
            try {
                if (sequenceContainer == null) return;
                File.Delete(Path.Combine(userTemplatePath, Path.Combine(sequenceContainer.SubGroups), GetTemplateFileName(sequenceContainer.Container)));
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(Loc.Instance["Lbl_SequenceTemplateController_DeleteTemplateFailed"]);
            }
        }

        private async Task SaveLinkedTemplate(TemplateReference reference, ISequenceContainer sequenceContainer, CancellationToken token) {
            if (reference == null || !reference.IsValid) {
                throw new InvalidOperationException(Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_StatusInvalid"]);
            }

            await saveLinkedTemplateSemaphore.WaitAsync(token);
            bool restoreWatcher = false;
            try {
                if (reference.SourceKind != TemplateReferenceSourceKind.User) {
                    throw new InvalidOperationException(Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_DefaultNotEditable"]);
                }

                string templateRoot = Path.GetFullPath(userTemplatePath ?? profileService.ActiveProfile.SequenceSettings.SequencerTemplatesFolder);
                string relativePath = TemplateReference.NormalizeRelativePath(reference.RelativePath).Replace('/', Path.DirectorySeparatorChar);
                if (!relativePath.EndsWith(TemplateFileExtension, StringComparison.OrdinalIgnoreCase)) {
                    throw new InvalidOperationException(Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_StatusInvalid"]);
                }

                string filePath = Path.GetFullPath(Path.Combine(templateRoot, relativePath));
                string rootWithSeparator = templateRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!filePath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)) {
                    throw new InvalidOperationException(Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_StatusInvalid"]);
                }

                if (sequenceTemplateFolderWatcher != null) {
                    restoreWatcher = sequenceTemplateFolderWatcher.EnableRaisingEvents;
                    sequenceTemplateFolderWatcher.EnableRaisingEvents = false;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                ISequenceContainer templateContainer = (ISequenceContainer)sequenceContainer.Clone();
                templateContainer.AttachNewParent(null);
                templateContainer.ResetAll();
                PrepareDeepSkyObjectTemplate(templateContainer);

                string jsonContainer = sequenceJsonConverter.Serialize(templateContainer);
                await File.WriteAllTextAsync(filePath, jsonContainer, token);
                await LoadUserTemplates();
            } finally {
                if (restoreWatcher && sequenceTemplateFolderWatcher != null) {
                    sequenceTemplateFolderWatcher.EnableRaisingEvents = true;
                }
                saveLinkedTemplateSemaphore.Release();
            }
        }

        private string GetTemplateFileName(ISequenceContainer container) {
            return NINA.Core.Utility.CoreUtil.ReplaceAllInvalidFilenameChars(container.Name) + TemplateFileExtension;
        }

        private TemplateReference CreateTemplateReference(TemplateReferenceSourceKind sourceKind, string root, string file, string displayName) {
            return new TemplateReference {
                SourceKind = sourceKind,
                RelativePath = Path.GetRelativePath(root, file),
                DisplayName = displayName
            };
        }

        private void PrepareDeepSkyObjectTemplate(ISequenceContainer sequenceContainer) {
            if (sequenceContainer is IDeepSkyObjectContainer) {
                var dso = (sequenceContainer as IDeepSkyObjectContainer);
                dso.Target.TargetName = string.Empty;
                dso.Target.InputCoordinates.Coordinates = new Coordinates(Angle.Zero, Angle.Zero, Epoch.J2000);
                dso.Target.PositionAngle = 0;
                dso.Target = dso.Target;
            }
        }
    }

    public class TemplatedSequenceContainer : IDroppable, IDroppableSourceProvider {

        public TemplatedSequenceContainer(IProfileService profileService, string group, ISequenceContainer container)
            : this(profileService, group, container, null, null) {
        }

        public TemplatedSequenceContainer(
            IProfileService profileService,
            string group,
            ISequenceContainer container,
            TemplateReference reference,
            ITemplateLinkResolver templateLinkResolver) {
            Group = group;
            Container = container;
            SubGroups = new string[0];
            this.profileService = profileService;
            this.templateLinkResolver = templateLinkResolver;
            Reference = reference ?? new TemplateReference {
                SourceKind = group == TemplateController.DefaultTemplatesGroup ? TemplateReferenceSourceKind.Default : TemplateReferenceSourceKind.User,
                DisplayName = container?.Name ?? string.Empty
            };
        }

        public string GroupTranslated => Loc.Instance[Group] + " › " + (SubGroups.Length > 0 ? $"{string.Join(" › ", SubGroups)}" : "Base");

        public string Group { get; }
        public string[] SubGroups { get; set; }

        private IProfileService profileService;
        private readonly ITemplateLinkResolver templateLinkResolver;

        public ISequenceContainer Container { get; }
        public TemplateReference Reference { get; }

        public ISequenceContainer Parent => null;

        public ICommand DetachCommand => null;

        public ICommand MoveUpCommand => null;

        public ICommand MoveDownCommand => null;

        public void AfterParentChanged() {
        }

        public void AttachNewParent(ISequenceContainer newParent) {
        }

        public void Detach() {
        }

        public void MoveDown() {
        }

        public void MoveUp() {
        }

        public ISequenceItem Clone() {
            var clone = (ISequenceContainer)Container.Clone();
            if (profileService.ActiveProfile.SequenceSettings.CollapseSequencerTemplatesByDefault) {
                clone.IsExpanded = false;
            }
            return clone;
        }

        public LinkedTemplateContainer CreateLinkedContainer() {
            LinkedTemplateContainer linkedTemplateContainer = new LinkedTemplateContainer(templateLinkResolver);
            linkedTemplateContainer.MaterializeFromTemplate(this, !profileService.ActiveProfile.SequenceSettings.CollapseSequencerTemplatesByDefault);
            return linkedTemplateContainer;
        }

        public IDroppable GetDropSource(ModifierKeys modifiers) {
            if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control) {
                return CreateLinkedContainer();
            }

            return this;
        }

        public override string ToString() {
            return this.Container.Name;
        }
    }
}
