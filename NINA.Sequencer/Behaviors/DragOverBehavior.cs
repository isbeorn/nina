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
using NINA.Core.Locale;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using System.Windows.Media;

namespace NINA.Sequencer.Behaviors {

    /// <summary>
    /// A behavior to handle dragging an item over the item where the behavior is registered.
    /// When the dragged item is dropped the behavior will look for a correspondign DragIntoBehavior that is part of the drop target or one of its parent elements
    /// </summary>
    public class DragOverBehavior : Behavior<FrameworkElement> {

        public DragOverBehavior() {
        }

        public static readonly DependencyProperty DragBelowSizeProperty = DependencyProperty.Register(nameof(DragBelowSize), typeof(double), typeof(DragOverBehavior), new PropertyMetadata(0d));
        public static readonly DependencyProperty AllowDragCenterProperty = DependencyProperty.Register(nameof(AllowDragCenter), typeof(bool), typeof(DragOverBehavior), new PropertyMetadata(true));
        public static readonly DependencyProperty AllowDragAboveProperty = DependencyProperty.Register(nameof(AllowDragAbove), typeof(bool), typeof(DragOverBehavior), new PropertyMetadata(true));
        public static readonly DependencyProperty AllowDragBelowProperty = DependencyProperty.Register(nameof(AllowDragBelow), typeof(bool), typeof(DragOverBehavior), new PropertyMetadata(true));
        public static readonly DependencyProperty DragAboveSizeProperty = DependencyProperty.Register(nameof(DragAboveSize), typeof(double), typeof(DragOverBehavior), new PropertyMetadata(0d));
        public static readonly DependencyProperty EnabledProperty = DependencyProperty.Register(nameof(Enabled), typeof(bool), typeof(DragOverBehavior), new PropertyMetadata(true));
        public static readonly DependencyProperty DragOverDisplayAnchorProperty = DependencyProperty.Register(nameof(DragOverDisplayAnchor), typeof(DragOverDisplayAnchor), typeof(DragOverBehavior), new PropertyMetadata(DragOverDisplayAnchor.Right));
        public static readonly DependencyProperty DragOverTopTextProperty = DependencyProperty.Register(nameof(DragOverTopText), typeof(string), typeof(DragOverBehavior), new PropertyMetadata(Loc.Instance["LblDragOver_TopText"]));
        public static readonly DependencyProperty DragOverBottomTextProperty = DependencyProperty.Register(nameof(DragOverBottomText), typeof(string), typeof(DragOverBehavior), new PropertyMetadata(Loc.Instance["LblDragOver_BottomText"]));
        public static readonly DependencyProperty DragOverCenterTextProperty = DependencyProperty.Register(nameof(DragOverCenterText), typeof(string), typeof(DragOverBehavior), new PropertyMetadata(Loc.Instance["LblDragOver_CenterText"]));

        private readonly Grid layoutParent = (Application.Current.MainWindow.FindName("RootGrid") as Grid);
        private DragOverAdorner dragOverAdorner;
        private FrameworkElement hitElement;

        public bool Enabled {
            get => (bool)GetValue(EnabledProperty);
            set => SetValue(EnabledProperty, value);
        }

        public double DragBelowSize {
            get => (double)GetValue(DragBelowSizeProperty);
            set => SetValue(DragBelowSizeProperty, value);
        }

        public double DragAboveSize {
            get => (double)GetValue(DragAboveSizeProperty);
            set => SetValue(DragAboveSizeProperty, value);
        }

        public DragOverDisplayAnchor DragOverDisplayAnchor {
            get => (DragOverDisplayAnchor)GetValue(DragOverDisplayAnchorProperty);
            set => SetValue(DragOverDisplayAnchorProperty, value);
        }

        public string DragOverTopText {
            get => (string)GetValue(DragOverTopTextProperty);
            set => SetValue(DragOverTopTextProperty, value);
        }

        public string DragOverBottomText {
            get => (string)GetValue(DragOverBottomTextProperty);
            set => SetValue(DragOverBottomTextProperty, value);
        }

        public string DragOverCenterText {
            get => (string)GetValue(DragOverCenterTextProperty);
            set => SetValue(DragOverCenterTextProperty, value);
        }

        public bool AllowDragCenter {
            get => (bool)GetValue(AllowDragCenterProperty);
            set => SetValue(AllowDragCenterProperty, value);
        }

        public bool AllowDragBelow {
            get => (bool)GetValue(AllowDragBelowProperty);
            set => SetValue(AllowDragBelowProperty, value);
        }

        public bool AllowDragAbove {
            get => (bool)GetValue(AllowDragAboveProperty);
            set => SetValue(AllowDragAboveProperty, value);
        }

        public DropTargetEnum DropTarget { get; set; }

        private bool hasDragOverElement = false;
        private DropTargetEnum lastDropTarget = DropTargetEnum.None;

        private void MouseInObject(object sender, MouseEventArgs e) {
            if (!Enabled) return;
            layoutParent.RaiseEvent(e);

            var layoutHitTestBase = Application.Current.MainWindow as UIElement;

            var mousePosition = e.GetPosition(layoutHitTestBase);

            // look for attached dragdropbehavior above the item
            VisualTreeHelper.HitTest(layoutHitTestBase,
                null,
                new HitTestResultCallback(FindDragDropItemAboveMyself),
                new PointHitTestParameters(mousePosition));
            VisualTreeHelper.HitTest(layoutHitTestBase,
                null,
                new HitTestResultCallback(FindFirstItemUnderDropItem),
                new PointHitTestParameters(mousePosition));

            if (hasDragOverElement /*&& hitElement.DataContext == AssociatedObject.DataContext*/) {
                // check if we can actually drop into the found element
                var behaviors = Interaction.GetBehaviors(hitElement);
                var dropIntoBehavior = behaviors.FirstOrDefault(ex => ex is DropIntoBehavior) as DropIntoBehavior;
                if (dropIntoBehavior != null && !dropIntoBehavior.CanDropInto(lastAdorner.DragDropBehavior.OriginalParentedObject.DataContext.GetType())) {
                    lastDropTarget = DropTargetEnum.None;
                    DetachAdorner();
                    return;
                }

                var mousePos = e.GetPosition(AssociatedObject);
                var dropTarget = lastDropTarget;
                // check for mouse position relative to the current object
                if (AllowDragAbove && DragAboveSize > mousePos.Y) {
                    lastDropTarget = DropTargetEnum.Top;
                } else if (AllowDragBelow && AssociatedObject.ActualHeight - DragBelowSize < mousePos.Y) {
                    lastDropTarget = DropTargetEnum.Bottom;
                } else {
                    if (AllowDragCenter) {
                        lastDropTarget = DropTargetEnum.Center;
                    } else {
                        lastDropTarget = DropTargetEnum.None;
                    }
                }

                if (lastDropTarget != dropTarget) {
                    DetachAdorner();
                    AttachAdorner();
                }
            } else {
                if (lastDropTarget != DropTargetEnum.None) {
                    lastDropTarget = DropTargetEnum.None;
                    DetachAdorner();
                }
            }
        }

        private void AttachAdorner() {
            if (lastDropTarget == DropTargetEnum.Top || lastDropTarget == DropTargetEnum.Bottom || lastDropTarget == DropTargetEnum.Center) {
                int index = -1;

                foreach (UIElement child in layoutParent.Children) {
                    if (child is DragDropAdorner) {
                        index = layoutParent.Children.IndexOf(child);
                    }
                }

                TranslateTransform movingTransform = new TranslateTransform();

                Point relativeLocation = AssociatedObject.TranslatePoint(new Point(0, 0), layoutParent);
                double height = GetVisibleHeight(AssociatedObject);

                var adornerBaseWidth = AssociatedObject.ActualWidth;

                bool leftOfElement = DragOverDisplayAnchor == DragOverDisplayAnchor.Left;

                if (lastDropTarget == DropTargetEnum.Top) {
                    dragOverAdorner = new DragOverAdorner(adornerBaseWidth, height, DragOverTopText, leftOfElement, lastDropTarget, AssociatedObject);
                    movingTransform.Y = relativeLocation.Y - dragOverAdorner.AdornerHeight / 2;
                    movingTransform.X = relativeLocation.X + (leftOfElement ? (-(dragOverAdorner.AdornerWidth - adornerBaseWidth)) : adornerBaseWidth);
                } else if (lastDropTarget == DropTargetEnum.Center) {
                    dragOverAdorner = new DragOverAdorner(adornerBaseWidth, height, DragOverCenterText, leftOfElement, lastDropTarget, AssociatedObject);
                    movingTransform.Y = relativeLocation.Y + height / 2 - dragOverAdorner.AdornerHeight / 2;
                    movingTransform.X = relativeLocation.X + (leftOfElement ? (-(dragOverAdorner.AdornerWidth - adornerBaseWidth)) : adornerBaseWidth);
                } else {
                    dragOverAdorner = new DragOverAdorner(adornerBaseWidth, height, DragOverBottomText, leftOfElement, lastDropTarget, AssociatedObject);
                    movingTransform.Y = relativeLocation.Y + AssociatedObject.ActualHeight - dragOverAdorner.AdornerHeight / 2;
                    movingTransform.X = relativeLocation.X + (leftOfElement ? (-(dragOverAdorner.AdornerWidth - adornerBaseWidth)) : adornerBaseWidth);
                }

                layoutParent.Children.Insert(index, dragOverAdorner);
                dragOverAdorner.RenderTransform = movingTransform;
                movingTransform.X = relativeLocation.X + (leftOfElement ? (-(dragOverAdorner.AdornerWidth - adornerBaseWidth)) : adornerBaseWidth);
            }
        }

        private double GetVisibleHeight(FrameworkElement element) {
            FrameworkElement scrollViewer = null;
            DependencyObject searchingElement = element;
            while (VisualTreeHelper.GetParent(searchingElement) != null) {
                if (searchingElement is ScrollViewer) {
                    scrollViewer = searchingElement as ScrollViewer;
                    break;
                }

                searchingElement = VisualTreeHelper.GetParent(searchingElement);
            }

            if (scrollViewer == null) {
                return element.ActualHeight;
            }

            var actualHeight = scrollViewer.ActualHeight + scrollViewer.TranslatePoint(new Point(0, 0), element).Y;
            actualHeight = actualHeight > element.ActualHeight ? element.ActualHeight : actualHeight;

            return actualHeight;
        }

        private bool DetachAdorner() {
            if (layoutParent.Children.Contains(dragOverAdorner)) {
                layoutParent.Children.Remove(dragOverAdorner);
                return true;
            }

            return false;
        }

        private void MouseLeftObject(object sender, MouseEventArgs e) {
            if (!Enabled) return;
            DropTarget = lastDropTarget;
            lastDropTarget = DropTargetEnum.None;
            DetachAdorner();
        }

        protected override void OnAttached() {
            base.OnAttached();
            WeakEventManager<FrameworkElement, MouseEventArgs>.AddHandler(AssociatedObject, nameof(AssociatedObject.MouseMove), MouseInObject);
            WeakEventManager<FrameworkElement, MouseEventArgs>.AddHandler(AssociatedObject, nameof(AssociatedObject.MouseEnter), MouseInObject);
            WeakEventManager<FrameworkElement, MouseEventArgs>.AddHandler(AssociatedObject, nameof(AssociatedObject.MouseLeave), MouseLeftObject);
            dragOverAdorner = null;
        }

        protected override void OnDetaching() {
            base.OnDetaching();

            if (AssociatedObject != null) {
                WeakEventManager<FrameworkElement, MouseEventArgs>.RemoveHandler(AssociatedObject, nameof(AssociatedObject.MouseMove), MouseInObject);
                WeakEventManager<FrameworkElement, MouseEventArgs>.RemoveHandler(AssociatedObject, nameof(AssociatedObject.MouseEnter), MouseInObject);
                WeakEventManager<FrameworkElement, MouseEventArgs>.RemoveHandler(AssociatedObject, nameof(AssociatedObject.MouseLeave), MouseLeftObject);
            }
        }

        private DragDropAdorner lastAdorner = null;

        public HitTestResultBehavior FindDragDropItemAboveMyself(HitTestResult result) {
            var hit = (result.VisualHit as FrameworkElement);
            if (hit != null) {
                // check for self, if we hit self, stop
                if (hit.DataContext == AssociatedObject.DataContext) {
                    hasDragOverElement = false;
                    return HitTestResultBehavior.Stop;
                }
                if (result.VisualHit is DragDropAdorner) {
                    lastAdorner = result.VisualHit as DragDropAdorner;
                    var behaviors = Interaction.GetBehaviors(AssociatedObject);
                    var dropIntoBehavior = behaviors.FirstOrDefault(ex => ex is DropIntoBehavior) as DropIntoBehavior;
                    var parent = AssociatedObject as DependencyObject;

                    //prevent infinite loop
                    var iterations = 0;
                    var maxIterations = 100;
                    while (dropIntoBehavior == null && parent != null && iterations < maxIterations) {
                        iterations++;
                        parent = VisualTreeHelper.GetParent(parent);
                        if (parent == null) {
                            break;//the command could not be found
                        }
                        behaviors = Interaction.GetBehaviors(parent);
                        dropIntoBehavior = behaviors.FirstOrDefault(ex => ex is DropIntoBehavior) as DropIntoBehavior;
                    }
                    if (dropIntoBehavior == null || string.IsNullOrEmpty(dropIntoBehavior.AllowedDragDropTypesString)) {
                        hasDragOverElement = true;
                        return HitTestResultBehavior.Stop;
                    }
                    if (dropIntoBehavior.CanDropInto(lastAdorner.DragDropBehavior.OriginalParentedObject.DataContext.GetType())) {
                        hasDragOverElement = true;
                        return HitTestResultBehavior.Stop;
                    }
                }
            }

            return HitTestResultBehavior.Continue;
        }

        public HitTestResultBehavior FindFirstItemUnderDropItem(HitTestResult result) {
            var hit = (result.VisualHit as FrameworkElement);
            if (hit != null) {
                if (result.VisualHit is DragDropAdorner || result.VisualHit is DragOverAdorner) {
                    return HitTestResultBehavior.Continue;
                }

                hitElement = hit;
                return HitTestResultBehavior.Stop;
            }

            return HitTestResultBehavior.Continue;
        }
    }
}