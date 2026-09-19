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
using NINA.Sequencer.Container;
using NINA.Sequencer.Logic;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace NINA.Sequencer.Editing {
    internal interface ISequenceEditCapture { ISequenceEdit Complete(); }

    /// <summary>Captures only the configuration addressed by an editor, never the whole live graph.</summary>
    internal sealed class SequencePropertyCapture : ISequenceEditCapture {
        private readonly Func<ISequenceEdit> complete;
        private SequencePropertyCapture(Func<ISequenceEdit> complete) { this.complete = complete; }
        public ISequenceEdit Complete() => complete();

        internal static SequencePropertyCapture Capture<T>(string description, Func<T> read, Action<T> write, Func<T, T, bool> equal = null, string context = null, Func<T, string> format = null, string summaryContext = null) {
            T before = read();
            format ??= value => SequenceEditDetails.Value(value);
            string beforeText = format(before);
            equal ??= EqualityComparer<T>.Default.Equals;
            return new SequencePropertyCapture(() => {
                T after = read();
                if (equal(before, after)) return null;
                string afterText = format(after);
                return new PropertySequenceEdit<T>(description, read, write, before, after, equal,
                    SequenceEditDetails.Change(context, beforeText, afterText),
                    SequenceEditDetails.Change(summaryContext ?? context, beforeText, afterText));
            });
        }

        public static SequencePropertyCapture Create(ISequenceEntity owner, BindingExpression binding) {
            if (binding == null || owner == null) return null;
            object source = binding.ResolvedSource;
            string name = binding.ResolvedSourcePropertyName;
            if (source == null || string.IsNullOrEmpty(name)) return null;
            PropertyInfo[] path = FindPath(owner, source);
            if (Excluded.Contains(name)) return null;
            if (source is Expression && name != nameof(Expression.Definition)) return null;
            ISequenceContainer parent = owner.Parent;
            object Resolve() {
                if (!ReferenceEquals(owner.Parent, parent)) throw new SequenceEditConflictException();
                object current = owner;
                foreach (PropertyInfo part in path) current = part.GetValue(current);
                return current;
            }
            string description = string.Format(Loc.Instance["Lbl_SequenceHistory_EditAction"], SequenceEditDetails.Name(owner), SequenceEditDetails.Field(path ?? Array.Empty<PropertyInfo>(), name));
            string context = SequenceEditDetails.Context(owner);
            string summaryContext = SequenceEditDetails.Context(owner, compact: true);
            var weakBinding = new WeakReference<BindingExpression>(binding);
            void RefreshBinding() {
                if (!weakBinding.TryGetTarget(out BindingExpression current) || current.Target == null) return;
                BindingExpressionBase target = BindingOperations.GetBindingExpressionBase(current.Target, current.TargetProperty);
                if (!ReferenceEquals(target, current) && target is not MultiBindingExpression) return;
                if (target is BindingExpression single) single.UpdateTarget();
                else if (target is MultiBindingExpression multi) multi.UpdateTarget();
                if (current.Target is TextBoxBase { IsUndoEnabled: true } text) {
                    text.SetCurrentValue(TextBoxBase.IsUndoEnabledProperty, false);
                    text.SetCurrentValue(TextBoxBase.IsUndoEnabledProperty, true);
                }
            }
            if (owner is ISequenceCustomPropertyEditProvider provider && provider.TryCapturePropertyState(source, name, out ISequenceEditSnapshot before)) {
                if (before == null) return new SequencePropertyCapture(() => null);
                var initial = new BoundSnapshot(before, owner, parent, RefreshBinding);
                return new SequencePropertyCapture(() => {
                    if (initial.IsCurrent) return null;
                    object current = path != null ? Resolve() : weakBinding.TryGetTarget(out BindingExpression active) ? active.ResolvedSource : null;
                    if (!provider.TryCapturePropertyState(current, name, out ISequenceEditSnapshot after) || after == null) {
                        throw new InvalidOperationException("The entity did not supply the completed edit snapshot.");
                    }
                    return new StateSequenceEdit(description, initial, new BoundSnapshot(after, owner, parent, RefreshBinding),
                        before.Description == null && after.Description == null ? context : SequenceEditDetails.Change(context, before.Description, after.Description),
                        summary: before.Description == null && after.Description == null ? summaryContext : SequenceEditDetails.Change(summaryContext, before.Description, after.Description));
                });
            }
            if (path == null) return null;
            if (source is InputCoordinates) {
                return Capture(description, () => SequenceCoordinateValue.Capture((InputCoordinates)Resolve()),
                    value => { value.Restore((InputCoordinates)Resolve()); RefreshBinding(); },
                    context: context, summaryContext: summaryContext, format: value => SequenceEditDetails.Coordinates(value.ToCoordinates(), value.NegativeDec));
            }
            PropertyInfo property = source.GetType().GetProperty(name);
            if (property?.CanRead != true || property.SetMethod?.IsPublic != true || property.GetIndexParameters().Length != 0) return null;
            if (typeof(Expression).IsAssignableFrom(property.PropertyType)) {
                return Capture(description, () => ((Expression)property.GetValue(Resolve())).Definition,
                    value => { ((Expression)property.GetValue(Resolve())).Definition = value; RefreshBinding(); }, context: context, summaryContext: summaryContext);
            }
            if (property.IsDefined(typeof(NINA.Sequencer.Generators.IsExpressionAttribute), true)
                && source.GetType().GetProperty(name + "Expression") is PropertyInfo expressionProperty) {
                return Capture(description, () => ((Expression)expressionProperty.GetValue(Resolve())).Definition,
                    value => { ((Expression)expressionProperty.GetValue(Resolve())).Definition = value; RefreshBinding(); }, context: context, summaryContext: summaryContext);
            }
            Type type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            // Reference-valued plugin state requires a custom edit, rather than a shallow copy
            // which silently changes along with the live object.
            bool selection = binding.Target is Selector && (binding.TargetProperty == Selector.SelectedItemProperty || binding.TargetProperty == Selector.SelectedValueProperty);
            bool scalar = type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
                || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan)
                || type == typeof(DateOnly) || type == typeof(TimeOnly) || type == typeof(Guid);
            if (!scalar && !selection) return null;
            string displayMemberPath = binding.Target is Selector selector && binding.TargetProperty == Selector.SelectedItemProperty ? selector.DisplayMemberPath : null;
            return Capture<object>(description, () => property.GetValue(Resolve()), value => { property.SetValue(Resolve(), value); RefreshBinding(); },
                context: context, summaryContext: summaryContext, format: value => SequenceEditDetails.Value(value, displayMemberPath));
        }

        private sealed class BoundSnapshot : ISequenceEditSnapshot {
            private readonly ISequenceEditSnapshot snapshot;
            private readonly ISequenceEntity owner;
            private readonly ISequenceContainer parent;
            private readonly Action refresh;
            public BoundSnapshot(ISequenceEditSnapshot snapshot, ISequenceEntity owner, ISequenceContainer parent, Action refresh) {
                this.snapshot = snapshot;
                this.owner = owner;
                this.parent = parent;
                this.refresh = refresh;
            }
            public string Description => snapshot.Description;
            public bool IsCurrent => ReferenceEquals(owner.Parent, parent) && snapshot.IsCurrent;
            public void Restore() {
                if (!ReferenceEquals(owner.Parent, parent)) throw new SequenceEditConflictException();
                snapshot.Restore();
                refresh();
            }
        }

        private static readonly HashSet<string> Excluded = new(StringComparer.Ordinal) {
            "Parent", "Status", "IsExpanded", "Expanded", "ShowMenu", "CompletedIterations",
            "Progress", "ProgressExposures", "Executed", "ExposureInfoListExpanded"
        };

        internal static SequencePropertyCapture Target(ISequenceEntity owner) {
            string description = string.Format(Loc.Instance["Lbl_SequenceHistory_TargetAction"], SequenceEditDetails.Name(owner));
            string context = SequenceEditDetails.Context(owner);
            string summaryContext = SequenceEditDetails.Context(owner, compact: true);
            if (owner is not IDeepSkyObjectContainer dso) return null;
            return Capture(description,
                () => (dso.Name, Target: SequenceTargetState.Capture(dso.Target)),
                value => { dso.Name = value.Name; value.Target.Restore(dso.Target); },
                context: context, summaryContext: summaryContext, format: value => SequenceEditDetails.Target(value.Target));
        }

        // Search only the selected entity's configuration objects. No traversal of Items,
        // Parent, services or the sequence tree occurs during field editing.
        private static PropertyInfo[] FindPath(ISequenceEntity owner, object target) {
            var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
            PropertyInfo[] Visit(object current, List<PropertyInfo> path) {
                if (ReferenceEquals(current, target)) return path.ToArray();
                if (current == null || path.Count >= 8 || !visited.Add(current)) return null;
                foreach (PropertyInfo property in current.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                    Type type = property.PropertyType;
                    if (!property.CanRead || property.GetIndexParameters().Length != 0 || type.IsValueType || type == typeof(string)
                        || Excluded.Contains(property.Name) || typeof(ISequenceEntity).IsAssignableFrom(type)
                        || typeof(System.Collections.IEnumerable).IsAssignableFrom(type)) continue;
                    bool configuration = property.IsDefined(typeof(JsonPropertyAttribute), true)
                        || typeof(Expression).IsAssignableFrom(type) || typeof(InputTarget).IsAssignableFrom(type)
                        || typeof(InputCoordinates).IsAssignableFrom(type);
                    if (!configuration) continue;
                    object value = property.GetValue(current);
                    path.Add(property);
                    PropertyInfo[] found = Visit(value, path);
                    path.RemoveAt(path.Count - 1);
                    if (found != null) return found;
                }
                return null;
            }
            return Visit(owner, new List<PropertyInfo>());
        }

    }
}