#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Core.Locale;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace NINA.Sequencer.Editing {
    /// <summary>Inherited editor context. Custom plugin views can register their own reversible edits.</summary>
    public static class SequenceEditContext {
        /// <summary>Wraps host-provided editing commands, including plugin overrides, without changing their contracts.</summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached(
            "Command", typeof(ICommand), typeof(SequenceEditContext), new PropertyMetadata(null, CommandChanged));
        public static ICommand GetCommand(DependencyObject element) => (ICommand)element.GetValue(CommandProperty);
        public static void SetCommand(DependencyObject element, ICommand value) => element.SetValue(CommandProperty, value);
        private static void CommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is ButtonBase button) button.Command = e.NewValue is ICommand command ? new HistoryCommand(button, command) : null;
        }

        public static readonly DependencyProperty OperationProperty = DependencyProperty.RegisterAttached(
            "Operation", typeof(SequenceEditOperation), typeof(SequenceEditContext), new PropertyMetadata(SequenceEditOperation.Move));
        public static SequenceEditOperation GetOperation(DependencyObject element) => (SequenceEditOperation)element.GetValue(OperationProperty);
        public static void SetOperation(DependencyObject element, SequenceEditOperation value) => element.SetValue(OperationProperty, value);

        internal static ICommand CreateCommand(ISequenceEntity owner, SequenceEditOperation operation, ICommand command) =>
            new SequenceEditCommand(command, parameter => ExecuteOperation(owner, operation, command, parameter));

        // A dialog or async operation starts recording itself around the accepted model update.
        internal static ICommand SelfRecordingCommand(ICommand command) => new SequenceEditCommand(command, command.Execute);

        internal static void ExecuteCommand(ISequenceEntity owner, SequenceEditOperation operation, ICommand command, object parameter) {
            if (command is SequenceEditCommand) command.Execute(parameter);
            else ExecuteOperation(owner, operation, command, parameter);
        }
        private static void ExecuteOperation(ISequenceEntity owner, SequenceEditOperation operation, ICommand command, object parameter) {
            void Apply() => command.Execute(parameter);
            if (operation == SequenceEditOperation.Toggle) { Toggle(owner, Apply); return; }
            string label = operation switch {
                SequenceEditOperation.Delete => "Lbl_SequenceHistory_DeleteAction",
                SequenceEditOperation.Duplicate => "Lbl_SequenceHistory_DuplicateAction",
                SequenceEditOperation.Place => "Lbl_SequenceHistory_PlaceAction",
                _ => "Lbl_SequenceHistory_MoveAction"
            };
            if (operation == SequenceEditOperation.Place) {
                var drop = parameter as DropIntoParameters;
                Structure(owner, label, Apply, drop?.Source as ISequenceEntity, drop?.Duplicate == true ? SequenceEditOperation.Duplicate : operation);
            } else Placement(owner, label, Apply, operation);
        }

        public static readonly DependencyProperty HistoryProperty = DependencyProperty.RegisterAttached(
            "History", typeof(ISequenceEditHistory), typeof(SequenceEditContext),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));
        public static ISequenceEditHistory GetHistory(DependencyObject element) {
            var history = (ISequenceEditHistory)element.GetValue(HistoryProperty);
            if (history is not SequenceEditHistory session) return history;
            ISequenceEntity owner = SequenceEditBindingResolver.Ancestors(element).OfType<FrameworkElement>()
                .Select(view => view.DataContext).OfType<ISequenceEntity>().FirstOrDefault();
            return owner == null ? session : session.ForOwner(owner);
        }
        public static void SetHistory(DependencyObject element, ISequenceEditHistory value) => element.SetValue(HistoryProperty, value);

        public static readonly DependencyProperty IsRecordingEnabledProperty = DependencyProperty.RegisterAttached(
            "IsRecordingEnabled", typeof(bool), typeof(SequenceEditContext),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));
        /// <summary>Set false on runtime controls or custom editors which register their own edits.</summary>
        public static bool GetIsRecordingEnabled(DependencyObject element) => (bool)element.GetValue(IsRecordingEnabledProperty);
        public static void SetIsRecordingEnabled(DependencyObject element, bool value) => element.SetValue(IsRecordingEnabledProperty, value);

        private static readonly List<WeakReference<SequenceEditHistory>> sessions = new();
        internal static void Register(SequenceEditHistory history) {
            lock (sessions) {
                sessions.RemoveAll(entry => !entry.TryGetTarget(out _));
                sessions.Add(new(history));
            }
        }
        internal static void Unregister(SequenceEditHistory history) {
            lock (sessions) sessions.RemoveAll(entry => !entry.TryGetTarget(out var target) || ReferenceEquals(target, history));
        }
        internal static SequenceEditHistory Find(ISequenceEntity entity) {
            if (entity == null) return null;
            SequenceEditHistory[] active;
            lock (sessions) active = sessions.Select(entry => entry.TryGetTarget(out var value) ? value : null).Where(value => value != null).Reverse().ToArray();
            return active.FirstOrDefault(history => history.Graph.OwnerPath(entity, false) != null)
                ?? active.FirstOrDefault(history => history.Graph.OwnerPath(entity) != null);
        }
        internal static void Placement(ISequenceEntity owner, string label, Action action, SequenceEditOperation? operation = null) =>
            CaptureStructure(Find(owner)?.ForOwner(owner), owner, label, action, owner, operation);

        internal static void Structure(ISequenceEntity owner, string label, Action action, ISequenceEntity subject = null, SequenceEditOperation? operation = null) {
            var session = Find(owner);
            CaptureStructure(owner is ISequenceContainer container ? session?.ForContents(container) : session?.ForOwner(owner), owner, label, action, subject, operation);
        }
        private static void CaptureStructure(SequenceEditHistory history, ISequenceEntity owner, string label, Action action, ISequenceEntity subject, SequenceEditOperation? operation) {
            if (history == null) action();
            else history.CaptureStructure(string.Format(Loc.Instance[label], SequenceEditDetails.Name(owner)), action, subject, operation);
        }
        internal static void Target(ISequenceEntity owner, Action action, Func<SequencePropertyCapture> captureTarget = null) {
            SequenceEditHistory history = Find(owner)?.ForOwner(owner);
            if (history?.IsRecording != true) { action(); return; }
            string description = string.Format(Loc.Instance["Lbl_SequenceHistory_TargetAction"], SequenceEditDetails.Name(owner));
            history.CaptureEdit(description, () => captureTarget != null ? captureTarget() : SequencePropertyCapture.Target(owner), action);
        }
        internal static void Property<T>(ISequenceEntity owner, string name, Func<T> read, Action<T> write, T value) {
            SequenceEditHistory history = Find(owner)?.ForOwner(owner);
            if (history?.IsRecording != true) { write(value); return; }
            string description = string.Format(Loc.Instance["Lbl_SequenceHistory_EditAction"], SequenceEditDetails.Name(SequenceEditDetails.VisibleOwner(owner)), name);
            history.CaptureEdit(description, () => SequencePropertyCapture.Capture(description, read, write, context: SequenceEditDetails.Context(owner), summaryContext: SequenceEditDetails.Context(owner, compact: true)), () => write(value));
        }
        internal static void StepExpression(FrameworkElement editor, DependencyProperty property, string definition) {
            BindingExpression binding = BindingOperations.GetBindingExpression(editor, property);
            var owner = binding?.ResolvedSource as ISequenceEntity;
            var history = GetIsRecordingEnabled(editor) ? Find(owner)?.ForOwner(owner) : null;
            history?.Flush();
            var capture = history?.IsRecording == true ? SequencePropertyCapture.Create(owner, binding) : null;
            if (editor.GetValue(property) is Logic.Expression expression) expression.Definition = definition;
            history?.RecordApplied(capture?.Complete());
        }
        internal static void Toggle(ISequenceEntity entity, Action action) {
            SequenceEditHistory history = Find(entity)?.ForOwner(entity);
            if (history?.IsRecording != true) { action(); return; }
            string description = string.Format(Loc.Instance["Lbl_SequenceHistory_ToggleAction"], SequenceEditDetails.Name(entity));
            history.CaptureEdit(description, () => SequencePropertyCapture.Capture(description,
                () => entity.Status != SequenceEntityStatus.DISABLED,
                enabled => entity.Status = enabled ? SequenceEntityStatus.CREATED : SequenceEntityStatus.DISABLED,
                context: SequenceEditDetails.Context(entity), summaryContext: SequenceEditDetails.Context(entity, compact: true), format: enabled => Loc.Instance[enabled ? "LblEnabled" : "LblDisabled"]), action);
        }

        private sealed class HistoryCommand : ICommand {
            private readonly FrameworkElement element;
            private readonly ICommand command;
            public HistoryCommand(FrameworkElement element, ICommand command) { this.element = element; this.command = command; }
            public event EventHandler CanExecuteChanged { add => command.CanExecuteChanged += value; remove => command.CanExecuteChanged -= value; }
            public bool CanExecute(object parameter) => command.CanExecute(parameter);
            public void Execute(object parameter) {
                if (element.DataContext is not ISequenceEntity owner) { command.Execute(parameter); return; }
                ExecuteCommand(owner, GetOperation(element), command, parameter);
            }
        }
    }
}