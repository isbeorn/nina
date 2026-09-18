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
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Trigger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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

        public static readonly DependencyProperty HistoryProperty = DependencyProperty.RegisterAttached(
            "History", typeof(ISequenceEditHistory), typeof(SequenceEditContext),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));
        public static ISequenceEditHistory GetHistory(DependencyObject element) {
            var history = (ISequenceEditHistory)element.GetValue(HistoryProperty);
            return history is SequenceEditHistory session ? session.ActiveHistory : history;
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
            foreach (SequenceEditHistory history in active) {
                for (ISequenceEntity current = entity; current != null; current = current.Parent) {
                    if (ReferenceEquals(current, history.Root)) return history;
                }
            }
            // Trigger action sets intentionally have a separate execution parent context.
            return active.FirstOrDefault(history => FindEditorPath(history.Root, entity) != null);
        }
        internal static IReadOnlyList<ISequenceEntity> FindEditorPath(ISequenceEntity root, ISequenceEntity target) {
            var visited = new HashSet<ISequenceEntity>(ReferenceEqualityComparer.Instance);
            var path = new List<ISequenceEntity>();
            bool Visit(ISequenceEntity current) {
                if (!visited.Add(current)) return false;
                path.Add(current);
                if (ReferenceEquals(current, target) || EditorChildren(current).Any(Visit)) return true;
                path.RemoveAt(path.Count - 1);
                return false;
            }
            return Visit(root) ? path : null;
        }
        private static IEnumerable<ISequenceEntity> EditorChildren(ISequenceEntity current) {
            if (current is Trigger.Utility.CustomTrigger { TriggerSource: not null } custom) yield return custom.TriggerSource;
            if (current is ISequenceTrigger trigger) {
                foreach (ISequenceContainer actions in EditableTriggerContainers(trigger)) yield return actions;
            }
            if (current is ISequenceContainer container) {
                foreach (ISequenceEntity item in container.GetItemsSnapshot()) yield return item;
                if (container is IConditionable conditions) foreach (ISequenceEntity condition in conditions.GetConditionsSnapshot()) yield return condition;
                if (container is ITriggerable triggers) foreach (ISequenceEntity child in triggers.GetTriggersSnapshot()) yield return child;
            }
        }
        internal static IEnumerable<ISequenceContainer> EditableTriggerContainers(ISequenceTrigger trigger) {
            if (trigger is SequenceTrigger { TriggerRunner: not null } sequence) yield return sequence.TriggerRunner;
            if (trigger is ISequenceTriggerEditor editor) {
                foreach (ISequenceContainer container in editor.GetAdditionalEditorContainers()) {
                    if (container != null) yield return container;
                }
            }
        }
        internal static void Structure(ISequenceEntity owner, string label, Action action) {
            SequenceEditHistory history = Find(owner)?.ForOwner(owner);
            if (history == null) action();
            else history.CaptureStructure(string.Format(Loc.Instance[label], SequenceEditDetails.Name(owner)), action);
        }
        internal static void Target(ISequenceEntity owner, Action action, Func<SequencePropertyCapture> captureTarget = null) {
            SequenceEditHistory history = Find(owner)?.ForOwner(owner);
            if (history?.IsRecording != true) { action(); return; }
            history.Flush();
            SequencePropertyCapture capture = captureTarget != null ? captureTarget() : SequencePropertyCapture.Target(owner);
            action();
            history.RecordApplied(capture?.Complete());
        }
        internal static void Property<T>(ISequenceEntity owner, string name, Func<T> read, Action<T> write, T value) {
            SequenceEditHistory history = Find(owner)?.ForOwner(owner);
            if (history?.IsRecording != true) { write(value); return; }
            history.Flush();
            var capture = SequencePropertyCapture.Capture(string.Format(Loc.Instance["Lbl_SequenceHistory_EditAction"], SequenceEditDetails.Name(owner), name), read, write, context: SequenceEditDetails.Context(owner));
            write(value);
            history.RecordApplied(capture.Complete());
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
            if (history?.IsRecording != true || history.IsCapturingToggle) { action(); return; }
            history.Flush();
            var capture = SequencePropertyCapture.Capture(string.Format(Loc.Instance["Lbl_SequenceHistory_ToggleAction"], SequenceEditDetails.Name(entity)),
                () => entity.Status != SequenceEntityStatus.DISABLED,
                enabled => entity.Status = enabled ? SequenceEntityStatus.CREATED : SequenceEntityStatus.DISABLED, context: SequenceEditDetails.Context(entity),
                format: enabled => Loc.Instance[enabled ? "LblEnabled" : "LblDisabled"]);
            history.IsCapturingToggle = true;
            try { action(); }
            finally {
                history.IsCapturingToggle = false;
                history.RecordApplied(capture.Complete());
            }
        }

        private sealed class HistoryCommand : ICommand {
            private readonly FrameworkElement element;
            private readonly ICommand command;
            public HistoryCommand(FrameworkElement element, ICommand command) { this.element = element; this.command = command; }
            public event EventHandler CanExecuteChanged { add => command.CanExecuteChanged += value; remove => command.CanExecuteChanged -= value; }
            public bool CanExecute(object parameter) => command.CanExecute(parameter);
            public void Execute(object parameter) {
                if (element.DataContext is not ISequenceEntity owner) { command.Execute(parameter); return; }
                BindingExpressionBase binding = BindingOperations.GetBindingExpressionBase(element, CommandProperty);
                if (binding is PriorityBindingExpression priority) binding = priority.ActiveBindingExpression;
                string name = (binding as BindingExpression)?.ResolvedSourcePropertyName;
                // The built-in clear command starts its transaction after its confirmation.
                if (owner is SequenceRootContainer && name == "DetachCommand") { command.Execute(parameter); return; }
                if (name == "DisableEnableCommand") Toggle(owner, () => command.Execute(parameter));
                else {
                    string label = name switch {
                        "DetachCommand" => "Lbl_SequenceHistory_DeleteAction",
                        "AddCloneToParentCommand" => "Lbl_SequenceHistory_DuplicateAction",
                        _ => "Lbl_SequenceHistory_MoveAction"
                    };
                    Structure(owner, label, () => command.Execute(parameter));
                }
            }
        }
    }
}