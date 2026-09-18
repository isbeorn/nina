#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Windows.Input;

namespace NINA.Sequencer.Editing {
    /// <summary>The configuration operation performed by a host editor button.</summary>
    public enum SequenceEditOperation { Move, Delete, Duplicate, Toggle, Place }

    // Marks the command as owning its recording boundary. Host views only wrap unmodified commands.
    internal sealed class SequenceEditCommand : ICommand {
        private readonly ICommand command;
        private readonly Action<object> execute;
        public SequenceEditCommand(ICommand command, Action<object> execute) { this.command = command; this.execute = execute; }
        public event EventHandler CanExecuteChanged { add => command.CanExecuteChanged += value; remove => command.CanExecuteChanged -= value; }
        public bool CanExecute(object parameter) => command.CanExecute(parameter);
        public void Execute(object parameter) => execute(parameter);
    }
}