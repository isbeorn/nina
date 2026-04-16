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
using NINA.Core.Enum;
using NINA.Sequencer.Behaviors;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.SequenceItem;
using NUnit.Framework;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SequenceItemBase = NINA.Sequencer.SequenceItem.SequenceItem;

namespace NINA.Test.Sequencer.Behaviors {

    [TestFixture]
    public class DropIntoBehaviorTest {

        /// <summary>
        /// Verifies the Allowed Drag Drop Types Parses Assignable Types And Can Drop Into Honors Enabled State scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void AllowedDragDropTypes_ParsesAssignableTypesAndCanDropIntoHonorsEnabledState() {
            Button target = new Button();
            DropIntoBehavior sut = new DropIntoBehavior {
                AllowedDragDropTypesString = typeof(SequenceItemBase).AssemblyQualifiedName
            };
            sut.Attach(target);

            sut.AllowedDragDropTypes.Should().ContainSingle().Which.Should().Be(typeof(SequenceItemBase));
            sut.CanDropInto(typeof(UnknownSequenceItem)).Should().BeTrue();
            sut.CanDropInto(typeof(string)).Should().BeFalse();
            sut.CanDropInto(null).Should().BeFalse();

            target.IsEnabled = false;

            sut.CanDropInto(typeof(UnknownSequenceItem)).Should().BeFalse();
        }

        /// <summary>
        /// Verifies the Execute Drop Into Defaults Center And Target Then Invokes Named Command scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void ExecuteDropInto_DefaultsCenterAndTargetThenInvokesNamedCommand() {
            CommandContainer target = new CommandContainer();
            Button targetElement = new Button { DataContext = target };
            DropIntoBehavior sut = new DropIntoBehavior {
                OnDropCommand = nameof(CommandContainer.DropCommand),
                AllowedDragDropTypesString = typeof(SequenceItemBase).AssemblyQualifiedName
            };
            sut.Attach(targetElement);
            UnknownSequenceItem source = new UnknownSequenceItem("Missing");

            sut.ExecuteDropInto(new DropIntoParameters(source));

            target.ReceivedParameter.Should().NotBeNull();
            target.ReceivedParameter.Source.Should().BeSameAs(source);
            target.ReceivedParameter.Target.Should().BeSameAs(target);
            target.ReceivedParameter.Position.Should().Be(DropTargetEnum.Center);
        }

        /// <summary>
        /// Verifies the Execute Drop Into Ignores Disabled Or Disallowed Drops scenario for the sequencer behavior under test.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void ExecuteDropInto_IgnoresDisabledOrDisallowedDrops() {
            CommandContainer target = new CommandContainer();
            Button targetElement = new Button { DataContext = target };
            DropIntoBehavior sut = new DropIntoBehavior {
                OnDropCommand = nameof(CommandContainer.DropCommand),
                AllowedDragDropTypesString = typeof(SequentialContainer).AssemblyQualifiedName
            };
            sut.Attach(targetElement);

            sut.ExecuteDropInto(new DropIntoParameters(new UnknownSequenceItem("Missing")));
            target.ReceivedParameter.Should().BeNull();

            sut.AllowedDragDropTypesString = string.Empty;
            targetElement.IsEnabled = false;
            sut.ExecuteDropInto(new DropIntoParameters(new UnknownSequenceItem("Missing")));

            target.ReceivedParameter.Should().BeNull();
        }

        private sealed class CommandContainer : SequentialContainer {
            public DropIntoParameters ReceivedParameter { get; private set; }

            public ICommand DropCommand => new CaptureCommand(parameter => ReceivedParameter = (DropIntoParameters)parameter);
        }

        private sealed class CaptureCommand : ICommand {
            private readonly System.Action<object> execute;

            public CaptureCommand(System.Action<object> execute) {
                this.execute = execute;
            }

            public bool CanExecute(object parameter) {
                return true;
            }

            public void Execute(object parameter) {
                execute(parameter);
            }

            public event System.EventHandler CanExecuteChanged;
        }
    }
}
