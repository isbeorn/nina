#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Sequencer.DragDrop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace NINA.Sequencer.Behaviors {

    /// <summary>
    /// This behavior will allow an element to be dragged and dropped to another place
    /// </summary>
    public class DragDropBehavior : Behavior<FrameworkElement> {

        public DragDropBehavior(Grid layoutParent) {
            this.layoutParent = layoutParent;
        }

        public DragDropBehavior() : this(Application.Current.MainWindow.FindName("RootGrid") as Grid) {
        }

        private MouseEventArgs mouseOverEventArgs = new MouseEventArgs(Mouse.PrimaryDevice, 0);
        private readonly List<Tuple<DependencyObject, Behavior>> detachedForeignBehaviors = new List<Tuple<DependencyObject, Behavior>>();
        private readonly List<Tuple<DependencyObject, Behavior>> detachedOwnChildrenBehaviors = new List<Tuple<DependencyObject, Behavior>>();
        private Behavior dragOverBehavior;
        private Behavior dropIntoBehavior;
        private readonly List<FrameworkElement> hitTestResults = new List<FrameworkElement>();
        private DropIntoBehavior hitTestDropIntoBehavior;
        private FrameworkElement draggedOverElement;
        private bool overBehaviorElement = false;
        private Effect previousEffect;
        private readonly Grid layoutParent;
        private UIElement currentMouseOverElement;
        private readonly TranslateTransform movingTransform = new TranslateTransform();

        public bool IsClone = false;

        private DragDropAdorner dragDropAdorner;
        public FrameworkElement OriginalParentedObject;
        private FrameworkElement originalObject;
        private Point mouseInElement;
        private List<DependencyObject> selfAndChildren = new List<DependencyObject>();
        private DateTime lastUpdate = DateTime.Now;

        private void AssociatedObject_MouseDown(object sender, MouseEventArgs e) {
            if (IsClone) return;
            // detach all own dropinto behaviors, remove own dragover behavior and remove all visually "below" dragdrop behaviors

            var currentDataContext = AssociatedObject.DataContext;
            originalObject = AssociatedObject;
            var parentObject = AssociatedObject as DependencyObject;
            var previousParent = AssociatedObject as DependencyObject;
            do {
                if ((parentObject as FrameworkElement)?.DataContext != currentDataContext) {
                    parentObject = previousParent;
                    break;
                }
                previousParent = parentObject;
            } while ((parentObject = VisualTreeHelper.GetParent(parentObject)) != null);

            if (parentObject != null) {
                DetachUnwantedBehaviors(parentObject, e);
            }

            // render clone of current to be dragged object
            RenderTargetBitmap rtb = RenderClone(parentObject as FrameworkElement);

            if (rtb != null) {
                // add blur effect to existing object
                previousEffect = (parentObject as FrameworkElement).Effect;
                BlurEffect blur = new BlurEffect() { Radius = 0 };
                DoubleAnimation blurEnable = new DoubleAnimation(0, 10, TimeSpan.FromSeconds(0)) { BeginTime = TimeSpan.Zero };
                (parentObject as FrameworkElement).Effect = blur;
                blur.BeginAnimation(BlurEffect.RadiusProperty, blurEnable);

                selfAndChildren.Clear();
                selfAndChildren.Add(parentObject);
                selfAndChildren.AddRange(AllChildren(parentObject));

                // spawn popup for current dragged object
                SpawnClonedDragDropAdorner(parentObject, e, rtb);

                // detach from original object now and move to the draggablePopup which is the visual clone
                // this whole fuckery is to avoid dragging around the visual tree of the parent object
                IsClone = true;
                OriginalParentedObject = parentObject as FrameworkElement;

                Detach();

                Attach(dragDropAdorner);
            }

            e.Handled = true;
        }

        private void SpawnClonedDragDropAdorner(DependencyObject obj, MouseEventArgs e, RenderTargetBitmap rtb) {
            dragDropAdorner = new DragDropAdorner(this, layoutParent, rtb) {
                IsHitTestVisible = true,
            };

            e.MouseDevice.OverrideCursor = Cursors.Cross;

            layoutParent.Children.Add(dragDropAdorner);
            // add popup to grid and move it to the original position so the mouse cursor is on top of it
            var relativeMousePosition = e.GetPosition(layoutParent);
            mouseInElement = e.GetPosition(obj as FrameworkElement);
            movingTransform.X = (relativeMousePosition.X - mouseInElement.X);
            movingTransform.Y = (relativeMousePosition.Y - mouseInElement.Y);
            dragDropAdorner.RenderTransform = movingTransform;
        }

        private RenderTargetBitmap RenderClone(FrameworkElement obj) {
            if (obj != null && obj.ActualWidth > 0 && obj.ActualHeight > 0) {
                RenderTargetBitmap rtb = new RenderTargetBitmap((int)obj.ActualWidth, (int)obj.ActualWidth, 96, 96, PixelFormats.Pbgra32);
                DrawingVisual visual = new DrawingVisual();
                using (DrawingContext context = visual.RenderOpen()) {
                    VisualBrush brush = new VisualBrush(obj) {
                        Stretch = Stretch.None,
                        Opacity = 0.4
                    };
                    context.DrawRectangle(brush, null, new Rect(new Point(), new Size((int)obj.ActualWidth, (int)obj.ActualHeight)));
                }
                rtb.Render(visual);
                return rtb;
            }
            return null;
        }

        private void AssociatedObject_MouseMove(object sender, MouseEventArgs e) {
            if (!IsClone) return;
            var pos = e.GetPosition(layoutParent);
            movingTransform.X = (pos.X - mouseInElement.X);
            movingTransform.Y = (pos.Y - mouseInElement.Y);

            HandleDragOver(e);
        }

        private void AssociatedObject_MouseUp(object sender, MouseEventArgs e) {
            if (!IsClone) return;

            // remove the draggable clone itself again
            layoutParent.Children.Remove(dragDropAdorner);

            // restore previous effect
            OriginalParentedObject.Effect = previousEffect;

            // send mouse leave event to previously dragged over object
            HandleLeaveObject();

            HandleDrop(e);

            // mouse event should not be further propagated
            e.Handled = true;
            e.MouseDevice.OverrideCursor = null;

            // detach behavior from clone and move to original object
            Detach();
            Attach(originalObject);
            IsClone = false;

            // reattach previously removed behaviors
            AttachPreviouslyUnwantedBehaviors();
        }

        private void AttachPreviouslyUnwantedBehaviors() {
            // restore detached foreign behaviors
            foreach (var item in detachedForeignBehaviors) {
                item.Item2?.Attach(item.Item1);
            }

            foreach (var item in detachedOwnChildrenBehaviors) {
                item.Item2?.Attach(item.Item1);
            }

            // restore dragoverbehavior if it existed
            dragOverBehavior?.Attach(OriginalParentedObject);
            dropIntoBehavior?.Attach(OriginalParentedObject);
        }

        private List<DependencyObject> AllChildren(DependencyObject parent) {
            var list = new List<DependencyObject> { };
            for (int count = 0; count < VisualTreeHelper.GetChildrenCount(parent); count++) {
                var child = VisualTreeHelper.GetChild(parent, count);
                if (child is DependencyObject) {
                    list.Add(child as DependencyObject);
                }
                list.AddRange(AllChildren(child));
            }
            return list;
        }

        private void DetachUnwantedBehaviors(DependencyObject obj, MouseEventArgs e) {
            // remove own dragOver behavior if exists
            var dragOverBehaviors = Interaction.GetBehaviors(obj);
            dragOverBehavior = dragOverBehaviors.FirstOrDefault(ex => ex is DragOverBehavior);
            dropIntoBehavior = dragOverBehaviors.FirstOrDefault(ex => ex is DropIntoBehavior);
            dragOverBehavior?.Detach();
            dropIntoBehavior?.Detach();

            detachedOwnChildrenBehaviors.Clear();
            detachedOwnChildrenBehaviors.Add(new Tuple<DependencyObject, Behavior>(obj, dragOverBehavior));
            detachedOwnChildrenBehaviors.Add(new Tuple<DependencyObject, Behavior>(obj, dropIntoBehavior));

            // remove all dropinto and dragover behaviors of own children
            foreach (var child in AllChildren(obj)) {
                var dropInto = Interaction.GetBehaviors(child).FirstOrDefault(f => f is DropIntoBehavior);
                if (dropInto != null) {
                    detachedOwnChildrenBehaviors.Add(new Tuple<DependencyObject, Behavior>(child, dropInto));
                    dropInto?.Detach();
                }
                var dragOver = Interaction.GetBehaviors(child).FirstOrDefault(f => f is DragOverBehavior);
                if (dragOver != null) {
                    detachedOwnChildrenBehaviors.Add(new Tuple<DependencyObject, Behavior>(child, dragOver));
                    dragOver?.Detach();
                }
            }

            // find all items below me of defined type to detach dragdrop behaviors to not conflict with this one
            // detach all dragdrop behavior of items below me
            detachedForeignBehaviors.Clear();
            var mousePosition = e.GetPosition(layoutParent);
            VisualTreeHelper.HitTest(layoutParent,
            null,
            new HitTestResultCallback(GetAllDragDropBehaviorsBelowMyself),
            new PointHitTestParameters(mousePosition));

            foreach (var behavior in detachedForeignBehaviors) {
                behavior.Item2.Detach();
            }
        }

        private void HandleDragOver(MouseEventArgs e) {
            if ((DateTime.Now - lastUpdate).TotalSeconds < 0.05) { return; }
            lastUpdate = DateTime.Now;
            // get mouse position and find the first item below myself
            mouseOverEventArgs = new MouseEventArgs(Mouse.PrimaryDevice, 0);
            var mousePos = e.GetPosition(layoutParent);
            draggedOverElement = null;
            VisualTreeHelper.HitTest(layoutParent,
                null,
                new HitTestResultCallback(GetFirstDraggedOverBehaviorElementBelowMyself),
                new PointHitTestParameters(mousePos));

            // if we find nothing, send a leave to the previous item
            if (draggedOverElement == null) {
                HandleLeaveObject();
                return;
            }

            //Debug.WriteLine("!> Sending mouse event to " + draggedOverElement.GetHashCode() +
            //    ", previous element " + +currentMouseOverElement?.GetHashCode());

            // check if we are in a different element spontaneously
            if (currentMouseOverElement != null && currentMouseOverElement != draggedOverElement) {
                overBehaviorElement = false;
                mouseOverEventArgs.RoutedEvent = UIElement.MouseLeaveEvent;
                currentMouseOverElement.RaiseEvent(mouseOverEventArgs);
                //Debug.WriteLine("<< Left element naturally " + currentMouseOverElement.GetHashCode());
            }

            // if we are still inside the same element just forward mouse move
            if (overBehaviorElement == true) {
                mouseOverEventArgs.RoutedEvent = UIElement.MouseMoveEvent;
                currentMouseOverElement.RaiseEvent(mouseOverEventArgs);
                //Debug.WriteLine("~~ Moving in element " + currentMouseOverElement.GetHashCode());
                return;
            }

            // we are in a new element
            currentMouseOverElement = draggedOverElement;
            //Debug.WriteLine(">> Entered element " + currentMouseOverElement.GetHashCode());
            mouseOverEventArgs.RoutedEvent = UIElement.MouseEnterEvent;
            currentMouseOverElement.RaiseEvent(mouseOverEventArgs);
            overBehaviorElement = true;
            return;
        }

        private void HandleDrop(MouseEventArgs e) {
            hitTestDropIntoBehavior = null;
            hitTestResults.Clear();
            draggedOverElement = null;
            var mousePos = e.GetPosition(layoutParent);
            VisualTreeHelper.HitTest(layoutParent,
                            null,
                            new HitTestResultCallback(GetFirstDropIntoBehaviorBelowMyself),
                            new PointHitTestParameters(mousePos));
            VisualTreeHelper.HitTest(layoutParent,
                null,
                new HitTestResultCallback(GetFirstDraggedOverBehaviorElementBelowMyself),
                new PointHitTestParameters(mousePos));

            if (hitTestDropIntoBehavior == null) return;

            if (draggedOverElement?.DataContext == OriginalParentedObject.DataContext) return;

            var behavior = draggedOverElement == null ? null : Interaction.GetBehaviors(draggedOverElement).FirstOrDefault(i => i is DragOverBehavior) as DragOverBehavior;

            hitTestDropIntoBehavior.ExecuteDropInto(new DropIntoParameters(OriginalParentedObject.DataContext as IDroppable,
                draggedOverElement?.DataContext as IDroppable,
                behavior?.DropTarget));

            return;
        }

        private void HandleLeaveObject() {
            if (currentMouseOverElement != null) {
                mouseOverEventArgs = new MouseEventArgs(Mouse.PrimaryDevice, 0);
                //Debug.WriteLine("<< Left element forcefully " + currentMouseOverElement.GetHashCode());
                mouseOverEventArgs.RoutedEvent = UIElement.MouseLeaveEvent;
                currentMouseOverElement.RaiseEvent(mouseOverEventArgs);
                currentMouseOverElement = null;
                overBehaviorElement = false;
            }
        }

        protected override void OnAttached() {
            //if (!(AssociatedObject.DataContext is IDroppable) && !(AssociatedObject is DragDropAdorner)) throw new ArgumentException("DragDropBehavior needs to be attached to an IDroppable");
            //Debug.WriteLine("++ DragDropBehavior attached to " + AssociatedObject.GetHashCode());

            if (AssociatedObject != null) {
                WeakEventManager<FrameworkElement, MouseEventArgs>.AddHandler(AssociatedObject, nameof(AssociatedObject.MouseLeftButtonDown), AssociatedObject_MouseDown);
                WeakEventManager<FrameworkElement, MouseEventArgs>.AddHandler(AssociatedObject, nameof(AssociatedObject.MouseLeftButtonUp), AssociatedObject_MouseUp);                
                WeakEventManager<FrameworkElement, MouseEventArgs>.AddHandler(layoutParent, nameof(layoutParent.MouseMove), AssociatedObject_MouseMove);
                WeakEventManager<FrameworkElement, MouseWheelEventArgs>.AddHandler(AssociatedObject, nameof(AssociatedObject.MouseWheel), AssociatedObject_MouseWheel);
            }
            base.OnAttached();
        }

        private void AssociatedObject_MouseWheel(object sender, MouseWheelEventArgs e) {
            if (!IsClone) return;
            mouseOverEventArgs = new MouseWheelEventArgs(Mouse.PrimaryDevice, e.Timestamp, e.Delta);
            mouseOverEventArgs.RoutedEvent = UIElement.MouseWheelEvent;
            var mousePos = e.GetPosition(layoutParent);
            draggedOverElement = null;
            VisualTreeHelper.HitTest(layoutParent,
                null,
                new HitTestResultCallback(GetFirstAnythingBelowMyself),
                new PointHitTestParameters(mousePos));

            // we are in a new element
            //Debug.WriteLine("\\> Wheel over element " + draggedOverElement?.GetHashCode());
            mouseOverEventArgs.RoutedEvent = UIElement.MouseWheelEvent;
            draggedOverElement?.RaiseEvent(mouseOverEventArgs);
        }

        protected override void OnDetaching() {
            //Debug.WriteLine("-- DragDropBehavior detached from " + AssociatedObject?.GetHashCode());

            if (AssociatedObject != null) {
                WeakEventManager<FrameworkElement, MouseEventArgs>.RemoveHandler(AssociatedObject, nameof(AssociatedObject.MouseLeftButtonDown), AssociatedObject_MouseDown);
                WeakEventManager<FrameworkElement, MouseEventArgs>.RemoveHandler(AssociatedObject, nameof(AssociatedObject.MouseLeftButtonUp), AssociatedObject_MouseUp);
                WeakEventManager<FrameworkElement, MouseEventArgs>.RemoveHandler(layoutParent, nameof(layoutParent.MouseMove), AssociatedObject_MouseMove);
                WeakEventManager<FrameworkElement, MouseWheelEventArgs>.RemoveHandler(AssociatedObject, nameof(AssociatedObject.MouseWheel), AssociatedObject_MouseWheel);
            }
            base.OnDetaching();
        }

        public HitTestResultBehavior GetAllDragDropBehaviorsBelowMyself(HitTestResult result) {
            var hit = (result.VisualHit as FrameworkElement);
            // check for self, if we hit self, ignore
            if (hit is DragDropAdorner || (result.VisualHit is FrameworkElement elem && AssociatedObject.DataContext == elem.DataContext)) {
                return HitTestResultBehavior.Continue;
            }
            // add item below self, continue
            var possibleBehaviorItem = hit.GetSelfAndAncestors().FirstOrDefault(ancestor => {
                var possibleBehavior = Interaction.GetBehaviors(ancestor).FirstOrDefault(b => b is DragDropBehavior);
                if (possibleBehavior == null) return false;
                detachedForeignBehaviors.Add(new Tuple<DependencyObject, Behavior>(ancestor, possibleBehavior));
                return true;
            });
            return HitTestResultBehavior.Continue;
        }

        public HitTestResultBehavior GetFirstAnythingBelowMyself(HitTestResult result) {
            var hit = (result.VisualHit as FrameworkElement);
            // add first item below self, then stop or stop if first item is self
            if (result.VisualHit is DragDropAdorner) return HitTestResultBehavior.Continue;
            if (selfAndChildren.Contains(hit)) {
                //Debug.WriteLine("?? DraggedOver search found child " + hit.GetHashCode());
                return HitTestResultBehavior.Continue;
            }
            draggedOverElement = result.VisualHit as FrameworkElement;
            //Debug.WriteLine("?? WheelOver search found " + draggedOverElement);
            return HitTestResultBehavior.Stop;
        }

        public HitTestResultBehavior GetFirstDraggedOverBehaviorElementBelowMyself(HitTestResult result) {
            var hit = (result.VisualHit as FrameworkElement);
            // add first item below self, then stop or stop if first item is self
            if (result.VisualHit is DragOverAdorner) return HitTestResultBehavior.Continue;
            if (selfAndChildren.Contains(hit)) {
                //Debug.WriteLine("?? DraggedOver search found child " + hit.GetHashCode());
                return HitTestResultBehavior.Stop;
            }
            var possibleBehaviorItem = hit.GetSelfAndAncestors().FirstOrDefault(ancestor => {
                var possibleBehavior = Interaction.GetBehaviors(ancestor).FirstOrDefault(b => b is DragOverBehavior);
                if (possibleBehavior == null) return false;
                draggedOverElement = ancestor as FrameworkElement;
                //Debug.WriteLine("?? DraggedOver Search Found " + draggedOverElement.GetHashCode());
                return true;
            });
            if (draggedOverElement == null) return HitTestResultBehavior.Continue;
            else return HitTestResultBehavior.Stop;
        }

        public HitTestResultBehavior GetFirstDropIntoBehaviorBelowMyself(HitTestResult result) {
            var hit = (result.VisualHit as FrameworkElement);
            // add first item below self, then stop
            if (result.VisualHit is DragOverAdorner) return HitTestResultBehavior.Continue;
            if (selfAndChildren.Contains(hit)) {
                return HitTestResultBehavior.Stop;
            }
            var possibleBehaviorItem = hit.GetSelfAndAncestors().FirstOrDefault(ancestor => {
                var possibleBehavior = Interaction.GetBehaviors(ancestor).FirstOrDefault(b => b is DropIntoBehavior);
                if (possibleBehavior == null) return false;
                hitTestDropIntoBehavior = possibleBehavior as DropIntoBehavior;
                hitTestResults.Add(ancestor as FrameworkElement);
                return true;
            });
            if (possibleBehaviorItem == null) return HitTestResultBehavior.Continue;
            else return HitTestResultBehavior.Stop;
        }
    }
}