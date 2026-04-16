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
using NINA.Core.Enum;
using NINA.Sequencer.Behaviors;
using NUnit.Framework;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NINA.Test.Sequencer.Behaviors {

    [TestFixture]
    public class DragOverBehaviorTest {
        private Grid layoutParent;
        private FrameworkElement element;
        private DragOverBehavior sut;

        [SetUp]
        [Apartment(ApartmentState.STA)]
        public void SetUp() {
            layoutParent = new Grid();
            element = new FrameworkElement {
                DataContext = new object(),
                Width = 100,
                Height = 40
            };
            layoutParent.Children.Add(element);
            layoutParent.Measure(new Size(200, 200));
            layoutParent.Arrange(new Rect(0, 0, 200, 200));

            sut = new DragOverBehavior(layoutParent);
            sut.Attach(element);
        }

        /// <summary>
        /// Verifies the drag-over dependency properties round-trip their configured values without requiring the application shell.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void DependencyProperties_RoundTripConfiguredDragZonesAndText() {
            sut.Enabled = false;
            sut.AllowDragAbove = false;
            sut.AllowDragBelow = false;
            sut.AllowDragCenter = false;
            sut.DragAboveSize = 12;
            sut.DragBelowSize = 18;
            sut.DragOverDisplayAnchor = DragOverDisplayAnchor.Left;
            sut.DragOverTopText = "top";
            sut.DragOverBottomText = "bottom";
            sut.DragOverCenterText = "center";

            sut.Enabled.Should().BeFalse();
            sut.AllowDragAbove.Should().BeFalse();
            sut.AllowDragBelow.Should().BeFalse();
            sut.AllowDragCenter.Should().BeFalse();
            sut.DragAboveSize.Should().Be(12);
            sut.DragBelowSize.Should().Be(18);
            sut.DragOverDisplayAnchor.Should().Be(DragOverDisplayAnchor.Left);
            sut.DragOverTopText.Should().Be("top");
            sut.DragOverBottomText.Should().Be("bottom");
            sut.DragOverCenterText.Should().Be("center");
        }

        /// <summary>
        /// Verifies the drag-over hit test stops immediately when it encounters the associated element itself.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void FindDragDropItemAboveMyself_SelfHitStopsAndClearsDragState() {
            SetPrivateField("hasDragOverElement", true);

            HitTestResultBehavior result = sut.FindDragDropItemAboveMyself(new PointHitTestResult(element, new Point()));

            result.Should().Be(HitTestResultBehavior.Stop);
            GetPrivateField<bool>("hasDragOverElement").Should().BeFalse();
        }

        /// <summary>
        /// Verifies a drag-drop adorner above the item marks the element as a valid drag-over target when no type restrictions are present.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void FindDragDropItemAboveMyself_DragDropAdornerWithoutRestrictionsAllowsDrop() {
            DragDropAdorner adorner = CreateDragDropAdorner(new object());

            HitTestResultBehavior result = sut.FindDragDropItemAboveMyself(new PointHitTestResult(adorner, new Point()));

            result.Should().Be(HitTestResultBehavior.Stop);
            GetPrivateField<bool>("hasDragOverElement").Should().BeTrue();
            GetPrivateField<DragDropAdorner>("lastAdorner").Should().BeSameAs(adorner);
        }

        /// <summary>
        /// Verifies drag-over hit testing walks up the visual tree to find a parent drop-into behavior with compatible allowed source types.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void FindDragDropItemAboveMyself_ParentDropIntoBehaviorAllowsCompatibleSource() {
            Grid parent = new Grid();
            FrameworkElement child = new FrameworkElement { DataContext = element.DataContext };
            parent.Children.Add(child);
            Interaction.GetBehaviors(parent).Add(new DropIntoBehavior {
                AllowedDragDropTypesString = typeof(object).AssemblyQualifiedName
            });
            DragOverBehavior behavior = new DragOverBehavior(layoutParent);
            behavior.Attach(child);
            DragDropAdorner adorner = CreateDragDropAdorner(new object());

            HitTestResultBehavior result = behavior.FindDragDropItemAboveMyself(new PointHitTestResult(adorner, new Point()));

            result.Should().Be(HitTestResultBehavior.Stop);
            GetPrivateField<bool>(behavior, "hasDragOverElement").Should().BeTrue();
        }

        /// <summary>
        /// Verifies drag-over hit testing keeps searching when a drop-into behavior rejects the dragged source type.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void FindDragDropItemAboveMyself_IncompatibleDropIntoBehaviorContinuesSearch() {
            Interaction.GetBehaviors(element).Add(new DropIntoBehavior {
                AllowedDragDropTypesString = typeof(string).AssemblyQualifiedName
            });
            DragDropAdorner adorner = CreateDragDropAdorner(new object());

            HitTestResultBehavior result = sut.FindDragDropItemAboveMyself(new PointHitTestResult(adorner, new Point()));

            result.Should().Be(HitTestResultBehavior.Continue);
            GetPrivateField<bool>("hasDragOverElement").Should().BeFalse();
        }

        /// <summary>
        /// Verifies hit testing for the item below the dragged adorner ignores drag visuals and records the first real framework element.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void FindFirstItemUnderDropItem_IgnoresDragAdornerAndStoresElement() {
            DragDropAdorner adorner = CreateDragDropAdorner(new object());
            FrameworkElement target = new FrameworkElement();

            sut.FindFirstItemUnderDropItem(new PointHitTestResult(adorner, new Point())).Should().Be(HitTestResultBehavior.Continue);
            sut.FindFirstItemUnderDropItem(new PointHitTestResult(target, new Point())).Should().Be(HitTestResultBehavior.Stop);

            GetPrivateField<FrameworkElement>("hitElement").Should().BeSameAs(target);
        }

        /// <summary>
        /// Verifies leaving a drag-over element commits the last calculated target and resets the pending drop target.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void MouseLeftObject_CommitsLastDropTargetAndResetsPendingTarget() {
            SetPrivateField("lastDropTarget", DropTargetEnum.Bottom);
            MouseEventArgs args = new MouseEventArgs(Mouse.PrimaryDevice, 0);

            InvokePrivate("MouseLeftObject", element, args);

            sut.DropTarget.Should().Be(DropTargetEnum.Bottom);
            GetPrivateField<DropTargetEnum>("lastDropTarget").Should().Be(DropTargetEnum.None);
        }

        /// <summary>
        /// Verifies visible-height calculation falls back to the element's arranged height when there is no containing scroll viewer.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void GetVisibleHeight_WithoutScrollViewerReturnsElementHeight() {
            double height = (double)InvokePrivate("GetVisibleHeight", element);

            height.Should().Be(element.ActualHeight);
        }

        /// <summary>
        /// Verifies attaching and detaching the behavior can be repeated against an injected layout parent without depending on Application.Current.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void AttachAndDetach_WithInjectedLayoutParent_DoesNotRequireApplicationShell() {
            sut.Detach();

            Action attach = () => sut.Attach(element);
            Action detach = () => sut.Detach();

            attach.Should().NotThrow();
            detach.Should().NotThrow();
        }

        private DragDropAdorner CreateDragDropAdorner(object sourceContext) {
            DragDropBehavior dragDropBehavior = new DragDropBehavior(layoutParent) {
                OriginalParentedObject = new FrameworkElement { DataContext = sourceContext }
            };
            RenderTargetBitmap bitmap = new RenderTargetBitmap(1, 1, 96, 96, PixelFormats.Pbgra32);
            return new DragDropAdorner(dragDropBehavior, layoutParent, bitmap);
        }

        private T GetPrivateField<T>(string fieldName) {
            return GetPrivateField<T>(sut, fieldName);
        }

        private static T GetPrivateField<T>(DragOverBehavior behavior, string fieldName) {
            FieldInfo field = typeof(DragOverBehavior).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.Should().NotBeNull();
            return (T)field.GetValue(behavior);
        }

        private void SetPrivateField(string fieldName, object value) {
            FieldInfo field = typeof(DragOverBehavior).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.Should().NotBeNull();
            field.SetValue(sut, value);
        }

        private object InvokePrivate(string methodName, params object[] args) {
            MethodInfo method = typeof(DragOverBehavior).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Should().NotBeNull();
            return method.Invoke(sut, args);
        }
    }
}
