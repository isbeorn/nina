#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Microsoft.Xaml.Behaviors;
using Moq;
using NINA.Core.Enum;
using NINA.Sequencer.Behaviors;
using NINA.Sequencer.DragDrop;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINA.Test.Sequencer.Behaviors {

    [TestFixture]
    public class DragDropBehaviorTest {
        private DragDropBehavior sut;
        private FrameworkElement element;
        private Grid mainGrid;

        [SetUp]
        [Apartment(ApartmentState.STA)]
        public void Setup() {
            mainGrid = new Grid();
            element = new FrameworkElement();

            sut = new DragDropBehavior(mainGrid);

            sut.Attach(element);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void RaiseMouseDownEvent_NothingAttached_EventHandled() {
            var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Mouse.MouseDownEvent };
            element.RaiseEvent(args);

            args.Handled.Should().BeTrue();
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void RaiseMouseLeaveEvent_NothingAttached_EventHandled() {
            var args = new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.MouseLeaveEvent };
            element.RaiseEvent(args);

            args.Handled.Should().BeFalse();
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void RaiseMouseEnterEvent_NothingAttached_EventHandled() {
            var args = new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.MouseEnterEvent };
            element.RaiseEvent(args);

            args.Handled.Should().BeFalse();
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void RaiseMouseMoveEvent_NothingAttached_EventHandled() {
            var args = new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.MouseMoveEvent };
            element.RaiseEvent(args);

            args.Handled.Should().BeFalse();
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void RaiseMouseUpEvent_NothingAttached_EventHandled() {
            var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Mouse.MouseUpEvent };
            element.RaiseEvent(args);

            args.Handled.Should().BeFalse();
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void RaiseMouseWheelEvent_NothingAttached_EventHandled() {
            var args = new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, 0) { RoutedEvent = Mouse.MouseWheelEvent };
            element.RaiseEvent(args);

            args.Handled.Should().BeFalse();
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void RaiseMouseDownEvent_Attached_ButNoHeight_EventHandled() {
            mainGrid.DataContext = new object();
            element.DataContext = new object();
            mainGrid.Children.Add(element);
            var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Mouse.MouseDownEvent };
            element.RaiseEvent(args);

            args.Handled.Should().BeTrue();
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void RaiseMouseDownEvent_Attached_WithHeight_EventHandled() {
            mainGrid.DataContext = new object();
            element.DataContext = new object();

            mainGrid.Width = 1000;
            mainGrid.Height = 1000;

            element.Height = 10;
            element.Width = 100;
            mainGrid.Children.Add(element);

            mainGrid.Arrange(new Rect(0, 0, 1000, 1000));

            var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Mouse.MouseDownEvent };
            element.RaiseEvent(args);

            args.Handled.Should().BeTrue();
        }

        /// <summary>
        /// Verifies hit testing for mouse-wheel forwarding records the first visual below the dragged clone and ignores the clone itself.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void GetFirstAnythingBelowMyself_StoresFirstNonCloneElement() {
            FrameworkElement target = new FrameworkElement { DataContext = new object() };

            HitTestResultBehavior result = sut.GetFirstAnythingBelowMyself(new PointHitTestResult(target, new Point()));

            result.Should().Be(HitTestResultBehavior.Stop);
            GetPrivateField<FrameworkElement>("draggedOverElement").Should().BeSameAs(target);
        }

        /// <summary>
        /// Verifies drop hit testing resolves the first ancestor with a drop-into behavior and stores it for the eventual drop command.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void GetFirstDropIntoBehaviorBelowMyself_StoresDropIntoBehaviorFromAncestor() {
            FrameworkElement target = new FrameworkElement { DataContext = new object() };
            DropIntoBehavior dropIntoBehavior = new DropIntoBehavior();
            Interaction.GetBehaviors(target).Add(dropIntoBehavior);

            HitTestResultBehavior result = sut.GetFirstDropIntoBehaviorBelowMyself(new PointHitTestResult(target, new Point()));

            result.Should().Be(HitTestResultBehavior.Stop);
            GetPrivateField<DropIntoBehavior>("hitTestDropIntoBehavior").Should().BeSameAs(dropIntoBehavior);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void GetFirstDropIntoBehaviorBelowMyself_IgnoresNonHitTestableAncestor() {
            Grid parent = new Grid { IsHitTestVisible = false };
            FrameworkElement child = new FrameworkElement();
            DropIntoBehavior dropIntoBehavior = new DropIntoBehavior();
            parent.Children.Add(child);
            Interaction.GetBehaviors(child).Add(dropIntoBehavior);

            HitTestResultBehavior result = sut.GetFirstDropIntoBehaviorBelowMyself(new PointHitTestResult(child, new Point()));

            result.Should().Be(HitTestResultBehavior.Continue);
            GetPrivateField<DropIntoBehavior>("hitTestDropIntoBehavior").Should().BeNull();
        }

        /// <summary>
        /// Verifies drag-drop hit testing gathers foreign drag behaviors below the dragged element while ignoring the dragged element itself.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void GetAllDragDropBehaviorsBelowMyself_CollectsForeignBehaviorAndIgnoresSelf() {
            object draggedContext = new object();
            object foreignContext = new object();
            element.DataContext = draggedContext;
            FrameworkElement foreign = new FrameworkElement { DataContext = foreignContext };
            DragDropBehavior foreignBehavior = new DragDropBehavior(mainGrid);
            Interaction.GetBehaviors(foreign).Add(foreignBehavior);

            sut.GetAllDragDropBehaviorsBelowMyself(new PointHitTestResult(element, new Point())).Should().Be(HitTestResultBehavior.Continue);
            sut.GetAllDragDropBehaviorsBelowMyself(new PointHitTestResult(foreign, new Point())).Should().Be(HitTestResultBehavior.Continue);

            GetPrivateField<List<Tuple<DependencyObject, Behavior>>>("detachedForeignBehaviors")
                .Should().ContainSingle(entry => ReferenceEquals(entry.Item2, foreignBehavior));
        }

        /// <summary>
        /// Verifies the drag-drop enabled dependency property can disable and re-enable event handling without changing attachment state.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void IsEnabled_RoundTripsAndSuppressesMouseDownHandling() {
            sut.IsEnabled = false;
            MouseButtonEventArgs args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Mouse.MouseDownEvent };

            element.RaiseEvent(args);

            sut.IsEnabled.Should().BeFalse();
            args.Handled.Should().BeFalse();

            sut.IsEnabled = true;
            sut.IsEnabled.Should().BeTrue();
        }

        /// <summary>
        /// Verifies clone rendering produces a bitmap snapshot when the dragged element has arranged dimensions.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void RenderClone_WithArrangedElement_ReturnsBitmapSnapshot() {
            element.Width = 80;
            element.Height = 40;
            element.Measure(new Size(80, 40));
            element.Arrange(new Rect(0, 0, 80, 40));

            RenderTargetBitmap bitmap = (RenderTargetBitmap)InvokePrivate("RenderClone", element);

            bitmap.Should().NotBeNull();
            bitmap.PixelWidth.Should().Be(80);
        }

        /// <summary>
        /// Verifies recursive child discovery returns nested visual children used when suppressing drag/drop behaviors during a drag.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void AllChildren_ReturnsNestedVisualChildren() {
            Grid parent = new Grid();
            Border child = new Border();
            TextBlock grandchild = new TextBlock();
            child.Child = grandchild;
            parent.Children.Add(child);

            List<DependencyObject> result = (List<DependencyObject>)InvokePrivate("AllChildren", parent);

            result.Should().Contain(child).And.Contain(grandchild);
        }

        /// <summary>
        /// Verifies previously detached own and foreign behaviors are attached back to their original visual elements after a drag completes.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void AttachPreviouslyUnwantedBehaviors_ReattachesStoredBehaviors() {
            FrameworkElement foreign = new FrameworkElement();
            FrameworkElement ownChild = new FrameworkElement();
            DragDropBehavior foreignBehavior = new DragDropBehavior(mainGrid);
            DropIntoBehavior ownChildBehavior = new DropIntoBehavior();
            DragOverBehavior originalDragOver = new DragOverBehavior(mainGrid);
            DropIntoBehavior originalDropInto = new DropIntoBehavior();
            GetPrivateField<List<Tuple<DependencyObject, Behavior>>>("detachedForeignBehaviors")
                .Add(new Tuple<DependencyObject, Behavior>(foreign, foreignBehavior));
            GetPrivateField<List<Tuple<DependencyObject, Behavior>>>("detachedOwnChildrenBehaviors")
                .Add(new Tuple<DependencyObject, Behavior>(ownChild, ownChildBehavior));
            SetPrivateField("dragOverBehavior", originalDragOver);
            SetPrivateField("dropIntoBehavior", originalDropInto);
            sut.OriginalParentedObject = element;

            InvokePrivate("AttachPreviouslyUnwantedBehaviors");

            GetAssociatedObject(foreignBehavior).Should().BeSameAs(foreign);
            GetAssociatedObject(ownChildBehavior).Should().BeSameAs(ownChild);
            GetAssociatedObject(originalDragOver).Should().BeSameAs(element);
            GetAssociatedObject(originalDropInto).Should().BeSameAs(element);
        }

        /// <summary>
        /// Verifies detaching unwanted behaviors removes drag/drop behaviors from the dragged element and its children while recording them for restoration.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void DetachUnwantedBehaviors_RemovesOwnAndChildBehaviorsForDrag() {
            Grid parent = new Grid { DataContext = element.DataContext };
            Border child = new Border { DataContext = element.DataContext };
            parent.Children.Add(child);
            mainGrid.Children.Add(parent);
            DragOverBehavior parentDragOver = new DragOverBehavior(mainGrid);
            DropIntoBehavior parentDropInto = new DropIntoBehavior();
            DragOverBehavior childDragOver = new DragOverBehavior(mainGrid);
            DropIntoBehavior childDropInto = new DropIntoBehavior();
            Interaction.GetBehaviors(parent).Add(parentDragOver);
            Interaction.GetBehaviors(parent).Add(parentDropInto);
            Interaction.GetBehaviors(child).Add(childDragOver);
            Interaction.GetBehaviors(child).Add(childDropInto);
            MouseEventArgs args = new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.MouseMoveEvent };

            InvokePrivate("DetachUnwantedBehaviors", parent, args);

            GetAssociatedObject(parentDragOver).Should().BeNull();
            GetAssociatedObject(parentDropInto).Should().BeNull();
            GetAssociatedObject(childDragOver).Should().BeNull();
            GetAssociatedObject(childDropInto).Should().BeNull();
            GetPrivateField<List<Tuple<DependencyObject, Behavior>>>("detachedOwnChildrenBehaviors")
                .Should().HaveCount(4);
        }

        /// <summary>
        /// Verifies forced leave handling raises a mouse-leave event on the current drag-over element and resets tracking state.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void HandleLeaveObject_RaisesMouseLeaveAndClearsCurrentElement() {
            FrameworkElement current = new FrameworkElement();
            bool left = false;
            current.MouseLeave += (sender, args) => left = true;
            SetPrivateField("currentMouseOverElement", current);
            SetPrivateField("overBehaviorElement", true);

            InvokePrivate("HandleLeaveObject");

            left.Should().BeTrue();
            GetPrivateField<UIElement>("currentMouseOverElement").Should().BeNull();
            GetPrivateField<bool>("overBehaviorElement").Should().BeFalse();
        }

        /// <summary>
        /// Verifies drag-over hit testing stops on self/children and otherwise finds the first ancestor with a drag-over behavior.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void GetFirstDraggedOverBehaviorElementBelowMyself_HandlesSelfAndDragOverAncestor() {
            FrameworkElement selfChild = new FrameworkElement();
            GetPrivateField<List<DependencyObject>>("selfAndChildren").Add(selfChild);
            sut.GetFirstDraggedOverBehaviorElementBelowMyself(new PointHitTestResult(selfChild, new Point()))
                .Should().Be(HitTestResultBehavior.Stop);

            FrameworkElement target = new FrameworkElement();
            DragOverBehavior dragOverBehavior = new DragOverBehavior(mainGrid);
            Interaction.GetBehaviors(target).Add(dragOverBehavior);

            sut.GetFirstDraggedOverBehaviorElementBelowMyself(new PointHitTestResult(target, new Point()))
                .Should().Be(HitTestResultBehavior.Stop);
            GetPrivateField<FrameworkElement>("draggedOverElement").Should().BeSameAs(target);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void GetFirstDraggedOverBehaviorElementBelowMyself_IgnoresDisabledAncestor() {
            Grid parent = new Grid { IsEnabled = false };
            FrameworkElement child = new FrameworkElement();
            DragOverBehavior dragOverBehavior = new DragOverBehavior(mainGrid);
            parent.Children.Add(child);
            Interaction.GetBehaviors(child).Add(dragOverBehavior);

            HitTestResultBehavior result = sut.GetFirstDraggedOverBehaviorElementBelowMyself(new PointHitTestResult(child, new Point()));

            result.Should().Be(HitTestResultBehavior.Continue);
            GetPrivateField<FrameworkElement>("draggedOverElement").Should().BeNull();
        }

        /// <summary>
        /// Verifies drop-into hit testing stops for dragged children and continues when no drop behavior exists.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void GetFirstDropIntoBehaviorBelowMyself_HandlesDraggedChildrenAndMissingBehavior() {
            FrameworkElement selfChild = new FrameworkElement();
            FrameworkElement plain = new FrameworkElement();
            GetPrivateField<List<DependencyObject>>("selfAndChildren").Add(selfChild);

            sut.GetFirstDropIntoBehaviorBelowMyself(new PointHitTestResult(selfChild, new Point()))
                .Should().Be(HitTestResultBehavior.Stop);
            sut.GetFirstDropIntoBehaviorBelowMyself(new PointHitTestResult(plain, new Point()))
                .Should().Be(HitTestResultBehavior.Continue);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void GetDropSource_UsesSourceProviderResult() {
            IDroppable source = Mock.Of<IDroppable>();
            TestSourceProvider provider = new TestSourceProvider(source);
            element.DataContext = provider;
            sut.OriginalParentedObject = element;

            IDroppable result = (IDroppable)InvokePrivate("GetDropSource");

            result.Should().BeSameAs(source);
            provider.Modifiers.Should().Be(Keyboard.Modifiers);
        }

        private T GetPrivateField<T>(string fieldName) {
            FieldInfo field = typeof(DragDropBehavior).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.Should().NotBeNull();
            return (T)field.GetValue(sut);
        }

        private void SetPrivateField(string fieldName, object value) {
            FieldInfo field = typeof(DragDropBehavior).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.Should().NotBeNull();
            field.SetValue(sut, value);
        }

        private object InvokePrivate(string methodName, params object[] args) {
            MethodInfo method = typeof(DragDropBehavior).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Should().NotBeNull();
            return method.Invoke(sut, args);
        }

        private static DependencyObject GetAssociatedObject(Behavior behavior) {
            PropertyInfo property = typeof(Behavior).GetProperty("AssociatedObject", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            property.Should().NotBeNull();
            return (DependencyObject)property.GetValue(behavior);
        }

        private sealed class TestSourceProvider : IDroppableSourceProvider {
            private readonly IDroppable source;

            public TestSourceProvider(IDroppable source) {
                this.source = source;
            }

            public ModifierKeys Modifiers { get; private set; }

            public IDroppable GetDropSource(ModifierKeys modifiers) {
                Modifiers = modifiers;
                return source;
            }
        }
    }
}