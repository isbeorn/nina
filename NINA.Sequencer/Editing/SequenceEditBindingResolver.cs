#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace NINA.Sequencer.Editing {
    internal sealed record SequenceEditBinding(FrameworkElement Editor, BindingExpressionBase ModelBinding,
        BindingExpressionBase InputBinding, SequenceEditHistory Session, SequencePropertyCapture Capture) {
        public void UpdateSource() {
            static void Update(BindingExpressionBase binding) {
                if (binding is BindingExpression single) single.UpdateSource();
                else if (binding is MultiBindingExpression multi) multi.UpdateSource();
            }
            // A composite control can defer both its inner text and outer model binding.
            if (!ReferenceEquals(InputBinding, ModelBinding)) Update(InputBinding);
            Update(ModelBinding);
        }
    }

    internal static class SequenceEditBindingResolver {
        internal static SequenceEditBinding Resolve(DependencyObject source, FrameworkElement editorRoot, SequenceEditHistory history) {
            BindingExpressionBase input = null;
            ISequenceEntity visualOwner = Ancestors(source).OfType<FrameworkElement>().Select(view => view.DataContext).OfType<ISequenceEntity>().FirstOrDefault();
            foreach (FrameworkElement element in Ancestors(source).OfType<FrameworkElement>()) {
                foreach (DependencyProperty property in BoundProperties(element)) {
                    BindingExpressionBase expression = BindingOperations.GetBindingExpressionBase(element, property);
                    foreach (BindingExpression leaf in Leaves(expression)) {
                        input ??= expression;
                        try {
                            ISequenceEntity boundOwner = leaf.ResolvedSource as ISequenceEntity
                                ?? (leaf.ResolvedSource as Logic.Expression)?.Symbol
                                ?? (leaf.ResolvedSource as Logic.Expression)?.Context;
                            foreach (ISequenceEntity owner in new[] { visualOwner, boundOwner }.Distinct()) {
                                if (owner == null) continue;
                                SequenceEditHistory session = history.ForOwner(owner);
                                if (session == null) continue;
                                SequencePropertyCapture capture = SequencePropertyCapture.Create(owner, leaf);
                                if (capture != null) return new(element, expression, input, session, capture);
                            }
                        } catch (Exception ex) { Logger.Error("Unable to capture sequence edit", ex); }
                    }
                }
                if (ReferenceEquals(element, editorRoot)) break;
            }
            return null;
        }

        private static readonly HashSet<string> EditableProperties = new() {
            "Text", "Value", "IsChecked", "SelectedItem", "SelectedValue", "SelectedIndex", "SelectedDate", "SelectedTime"
        };
        private static readonly ConcurrentDictionary<Type, DependencyProperty[]> editorProperties = new();
        private static IEnumerable<DependencyProperty> BoundProperties(FrameworkElement element) {
            // Template and style bindings are not local values. Discover editor DPs once
            // per control type, including wrappers supplied by plugins.
            var properties = new HashSet<DependencyProperty>(editorProperties.GetOrAdd(element.GetType(), type =>
                TypeDescriptor.GetProperties(type).Cast<PropertyDescriptor>()
                    .Where(property => EditableProperties.Contains(property.Name))
                    .Select(DependencyPropertyDescriptor.FromProperty)
                    .Where(property => property != null).Select(property => property.DependencyProperty).ToArray()));
            LocalValueEnumerator values = element.GetLocalValueEnumerator();
            while (values.MoveNext()) if (EditableProperties.Contains(values.Current.Property.Name)) properties.Add(values.Current.Property);
            return properties;
        }
        private static IEnumerable<BindingExpression> Leaves(BindingExpressionBase expression) {
            if (expression is BindingExpression binding) {
                if (WritesSource(binding.ParentBinding.Mode, binding)) yield return binding;
            } else if (expression is MultiBindingExpression multi) {
                if (!WritesSource(multi.ParentMultiBinding.Mode, multi)) yield break;
                var children = multi.BindingExpressions.OfType<BindingExpression>().ToArray();
                if (children.Length != multi.BindingExpressions.Count) yield break;
                var editable = children.Where(child => WritesSource(child.ParentBinding.Mode == BindingMode.Default
                    ? multi.ParentMultiBinding.Mode : child.ParentBinding.Mode, multi)).ToArray();
                // Read-only inputs can provide display context, such as the current camera gain.
                // Only a single writable value can use ordinary property capture.
                if (editable.Length == 1) {
                    yield return editable[0];
                } else if (editable.Length > 1 && children.FirstOrDefault()?.ResolvedSource is NINA.Astrometry.InputCoordinates or NINA.Astrometry.InputTopocentricCoordinates
                    && children.All(child => ReferenceEquals(child.ResolvedSource, children[0].ResolvedSource))) {
                    // Declination sign and degrees are committed together by the existing converter.
                    // The coordinate adapter captures the complete value, including negative zero.
                    yield return children[0];
                }
            } else if (expression is PriorityBindingExpression priority && priority.ActiveBindingExpression != null) {
                foreach (BindingExpression leaf in Leaves(priority.ActiveBindingExpression)) yield return leaf;
            }
        }
        private static bool WritesSource(BindingMode mode, BindingExpressionBase expression) =>
            mode is BindingMode.TwoWay or BindingMode.OneWayToSource
            || (mode == BindingMode.Default && expression.TargetProperty.GetMetadata(expression.Target) is FrameworkPropertyMetadata { BindsTwoWayByDefault: true });

        internal static bool IsWithin(DependencyObject source, DependencyObject ancestor) => Ancestors(source).Any(x => ReferenceEquals(x, ancestor));
        internal static IEnumerable<DependencyObject> Ancestors(DependencyObject element) {
            while (element != null) {
                yield return element;
                DependencyObject parent = element is Visual ? VisualTreeHelper.GetParent(element) : null;
                element = parent ?? (element as FrameworkElement)?.Parent ?? LogicalTreeHelper.GetParent(element);
            }
        }
    }
}