#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Sequencer.Editing;
using NINA.Sequencer.SequenceItem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace NINA.Sequencer.Container {
    public partial class LinkedTemplateContainer {
        private volatile bool executionReserved;
        private readonly TimeProvider editTimeProvider = TimeProvider.System;
        private static readonly TimeSpan EditInactivityTimeout = TimeSpan.FromMinutes(5);
        private long? lastEditActivity;
        private bool isSavingTemplate;
        private Dispatcher editDispatcher;
        // One outcome per session: null after cleanup, or a restoration failure that aborts waiting execution.
        private TaskCompletionSource<Exception> editCompletion;

        public bool IsWaitingForEdits => lastEditActivity.HasValue;
        private TimeSpan EditWaitRemaining => EditInactivityTimeout
            - editTimeProvider.GetElapsedTime(lastEditActivity ?? editTimeProvider.GetTimestamp());
        private string EditWaitStatusText => LinkedTemplatesInSubtree().Any(link => link.isSavingTemplate)
            ? Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_WaitingForSave"]
            : string.Format(Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_WaitingForEdits"],
                TimeSpan.FromSeconds(Math.Max(0, Math.Ceiling(EditWaitRemaining.TotalSeconds))).ToString(@"mm\:ss"));
        public bool HasOpenEdits => LinkedTemplatesInSubtree().Any(link => link.IsEditing || link.isSavingTemplate);
        public bool CanEditContents => IsEditing && !isSavingTemplate;
        private bool CanEditSource => LinkState == TemplateLinkState.Resolved
            && TemplateReference?.SourceKind == TemplateReferenceSourceKind.User;
        public bool CanEditTemplate => CanEditSource && !LinkedAncestors().Any(link => link.executionReserved || link.isSavingTemplate);
        public bool CanSaveTemplate => IsEditing && CanEditSource && !isSavingTemplate
            && !HasOpenDescendantEdits && Items.OfType<ISequenceContainer>().Count() == 1;
        public bool CanCancelTemplate => IsEditing && !isSavingTemplate && !HasOpenDescendantEdits;
        private bool HasOpenDescendantEdits => LinkedTemplatesInSubtree().Skip(1).Any(link => link.IsEditing || link.isSavingTemplate);

        private IEnumerable<LinkedTemplateContainer> LinkedAncestors() {
            for (ISequenceContainer current = this; current != null; current = current.Parent) {
                if (current is LinkedTemplateContainer linked) yield return linked;
            }
        }

        private IEnumerable<LinkedTemplateContainer> LinkedTemplatesInSubtree() {
            yield return this;
            foreach (var (_, children) in SequenceEditorGraph.Read(this, includeReadOnlyContents: true)) {
                foreach (var child in children.OfType<LinkedTemplateContainer>()) yield return child;
            }
        }

        private Task OnEditorThread(Action action) {
            // Share the structural-edit lock with commands, including admission to execution.
            // Dispatch first and never await while holding the lock.
            void Apply() => ApplyEditorChange(action);
            Dispatcher dispatcher = editDispatcher ??= LinkedTemplatesInSubtree().Select(link => link.editDispatcher).FirstOrDefault(d => d != null);
            if (dispatcher != null && !dispatcher.CheckAccess()) return dispatcher.InvokeAsync(Apply).Task;
            Apply();
            return Task.CompletedTask;
        }

        private void NotifyEditProperties() {
            foreach (var link in LinkedAncestors()) {
                link.RaisePropertyChanged(nameof(CanEditTemplate));
                link.RaisePropertyChanged(nameof(CanEditContents));
                link.RaisePropertyChanged(nameof(CanSaveTemplate));
                link.RaisePropertyChanged(nameof(CanCancelTemplate));
                link.RaisePropertyChanged(nameof(HasOpenEdits));
                link.RaisePropertyChanged(nameof(IsWaitingForEdits));
                link.RaisePropertyChanged(nameof(LinkStatusText));
            }
            CommandManager.InvalidateRequerySuggested();
        }

        internal void NotifyEditingActivity() {
            ApplyEditorChange(() => {
                if (!IsEditing || isSavingTemplate) return;
                RestartEditCountdowns();
            });
        }

        private void RestartEditCountdowns() {
            foreach (var link in LinkedAncestors().Where(link => link.IsWaitingForEdits)) {
                link.lastEditActivity = link.editTimeProvider.GetTimestamp();
                link.RaisePropertyChanged(nameof(LinkStatusText));
            }
        }

        private async Task WaitForEditing(IProgress<ApplicationStatus> progress, CancellationToken token) {
            while (true) {
                Task[] pending = null;
                await OnEditorThread(() => {
                    pending = CheckEditWait(token);
                    RaisePropertyChanged(nameof(IsWaitingForEdits));
                    RaisePropertyChanged(nameof(LinkStatusText));
                    if (IsWaitingForEdits) progress?.Report(new ApplicationStatus { Status = LinkStatusText });
                });
                if (pending.Length == 0) return;
                using var tick = CancellationTokenSource.CreateLinkedTokenSource(token);
                try {
                    var refresh = Task.Delay(TimeSpan.FromSeconds(1), editTimeProvider, tick.Token);
                    await Task.WhenAny(pending.Append(refresh)).WaitAsync(token);
                } finally { tick.Cancel(); }
            }
        }

        // Called on the editor thread under the same lock used by Save, Cancel and user activity.
        private Task[] CheckEditWait(CancellationToken token) {
            token.ThrowIfCancellationRequested();
            var editors = LinkedTemplatesInSubtree().Where(link => link.IsEditing || link.isSavingTemplate).ToArray();
            foreach (var editor in editors) {
                if (editor.editCompletion.Task.IsCompleted && editor.editCompletion.Task.Result is Exception failure) {
                    throw new SequenceEntityFailedException(failure.Message);
                }
            }
            if (editors.Length > 0) {
                lastEditActivity ??= editTimeProvider.GetTimestamp();
                if (editors.Any(link => link.isSavingTemplate) || EditWaitRemaining > TimeSpan.Zero) {
                    return editors.Select(link => (Task)link.editCompletion.Task).ToArray();
                }
                token.ThrowIfCancellationRequested();
                foreach (var editor in editors.Reverse()) {
                    editor.CancelEditing();
                    Logger.Warning($"Discarded inactive linked-template edits before execution: {editor.TemplateReference?.RelativePath}");
                }
                Notification.ShowWarning(string.Format(
                    Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_EditTimeout"], SourceTemplateName));
            }
            lastEditActivity = null;
            return Array.Empty<Task>();
        }

        private void BeginEditTemplate() {
            ApplyEditorChange(() => {
                if (!CanEditTemplate || IsEditing) return;
                if (!IsMaterialized && !TryResolveTemplate()) return;
                if (SynchronizationContext.Current is DispatcherSynchronizationContext) editDispatcher = Dispatcher.CurrentDispatcher;
                editCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
                IsEditing = true;
                SequenceEditContext.Find(this)?.ForContents(this);
                NotifyEditProperties();
            });
        }

        private async Task<bool> SaveTemplate(object arg) {
            SequenceEditHistory history = null;
            bool resumeHistory = false;
            bool accepted = false;
            try {
                ISequenceContainer content = null;
                ApplyEditorChange(() => {
                    if (!CanSaveTemplate) return;
                    accepted = isSavingTemplate = true;
                    SequenceEditContext.Find(this)?.Flush();
                    history = SequenceEditContext.Find(this)?.ForContents(this);
                    resumeHistory = history?.IsEnabled == true;
                    if (resumeHistory) history.IsEnabled = false;
                    NotifyEditProperties();
                    content = Items.OfType<ISequenceContainer>().Single();
                });
                if (content == null) return false;
                await templateLinkResolver.SaveTemplate(TemplateReference, content, CancellationToken.None);
                await OnEditorThread(() => {
                    RestoreSavedTemplate();
                    Notification.ShowSuccess(string.Format(Loc.Instance["LblTemplate_Updated"], SourceTemplateName));
                });
                return true;
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
                return false;
            } finally {
                if (accepted) {
                    await OnEditorThread(() => {
                        isSavingTemplate = false;
                        // Nested editors can share the still-open outer editor's history.
                        if (resumeHistory) history.IsEnabled = true;
                        RestartEditCountdowns();
                        if (!IsEditing) editCompletion.TrySetResult(null);
                        NotifyEditProperties();
                    });
                }
            }
        }

        private void RestoreSavedTemplate() {
            // Resolve before ending the session, so a missing source cannot silently run unsaved contents.
            if (templateLinkResolver?.TryResolve(TemplateReference, out var template) != true) {
                throw new SequenceEntityFailedException(string.Format(
                    Loc.Instance["Lbl_SequenceContainer_LinkedTemplateContainer_StatusMissing"], SourceTemplateName));
            }
            MaterializeFromTemplate(template);
            IsEditing = false;
        }

        private void CancelEditing() {
            try {
                RestoreSavedTemplate();
                editCompletion.TrySetResult(null);
            } catch (Exception ex) {
                editCompletion.TrySetResult(ex);
                throw;
            } finally { NotifyEditProperties(); }
        }

        private void CancelEditTemplate() {
            ApplyEditorChange(() => {
                if (!CanCancelTemplate) return;
                try { CancelEditing(); }
                catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
            });
        }
    }
}
