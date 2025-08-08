#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace NINA.Sequencer.Behaviors {

    /// <summary>
    /// This behavior handles the command to execute when an element is dropped into it. The DragOverBehavior will try to find this command here and invoke it
    /// </summary>
    public class DropIntoBehavior : Behavior<UIElement> {
        public static readonly DependencyProperty OnDropCommandProperty = DependencyProperty.Register(nameof(OnDropCommand), typeof(string), typeof(DropIntoBehavior));

        public static readonly DependencyProperty AllowedDragDropTypesProperty = DependencyProperty.Register(nameof(AllowedDragDropTypesString),
            typeof(string), typeof(DropIntoBehavior));

        public DropIntoBehavior() {
            AllowedDragDropTypesString = string.Empty;
        }

        public List<Type> AllowedDragDropTypes {
            get {
                var types = AllowedDragDropTypesString.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                List<Type> outputTypes = new List<Type>();

                foreach (var type in types) {
                    Type foundType = Type.GetType(type, false, true);
                    if (foundType != null) outputTypes.Add(foundType);
                }

                return outputTypes;
            }
        }

        public string AllowedDragDropTypesString {
            get => (string)GetValue(AllowedDragDropTypesProperty);
            set => SetValue(AllowedDragDropTypesProperty, value);
        }

        public string OnDropCommand {
            get => (string)GetValue(OnDropCommandProperty);

            set => SetValue(OnDropCommandProperty, value);
        }

        protected override void OnAttached() {
            base.OnAttached();
            //Debug.WriteLine("++ DropIntoBehavior attached to " + AssociatedObject.GetHashCode());
        }

        protected override void OnDetaching() {
            base.OnDetaching();
            //Debug.WriteLine("-- DropIntoBehavior detached from " + AssociatedObject?.GetHashCode() ?? "");
        }

        public bool CanDropInto(Type type) {
            if (type == null) { return false; }
            if (AssociatedObject?.IsEnabled != true) { return false; }
            if (!AllowedDragDropTypes.Any()) return true;
            else return AllowedDragDropTypes.Any(t => t.IsAssignableFrom(type));
        }

        public void ExecuteDropInto(DropIntoParameters parameter) {
            if (!CanDropInto(parameter?.Source?.GetType())) return;
            if (AssociatedObject == null) return;
            if (!AssociatedObject.IsEnabled) { return; }
            if (parameter.Position == null) parameter.Position = DropTargetEnum.Center;
            if (parameter.Target == null) parameter.Target = (AssociatedObject as FrameworkElement).DataContext as ISequenceContainer;
            if (Keyboard.IsKeyDown(Key.LeftAlt)) {
                parameter.Duplicate = true;
            }

            if (AssociatedObject is FrameworkElement drop) {
                if (drop.DataContext != null) {
                    var prop = drop.DataContext.GetType().GetProperty(OnDropCommand);
                    if (prop != null) {
                        var value = prop.GetValue(drop.DataContext) as ICommand;
                        if (value != null) {
                            value.Execute(parameter);
                        }
                    }
                }
            }
        }
    }
}