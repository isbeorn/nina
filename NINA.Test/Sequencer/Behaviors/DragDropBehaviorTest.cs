#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using FluentAssertions;
using Moq;
using NINA.Sequencer.Behaviors;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
    }
}