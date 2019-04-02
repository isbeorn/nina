#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using System;
using System.Windows.Input;

namespace NINA.Utility {

    public class RelayCommand : ICommand {

        #region Fields

        /// <summary>
        /// Encapsulated the execute action
        /// </summary>
        private Action<object> execute;

        /// <summary>
        /// Encapsulated the representation for the validation of the execute method
        /// </summary>
        private Predicate<object> canExecute;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the RelayCommand class Creates a new command that can
        /// always execute.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        public RelayCommand(Action<object> execute)
            : this(execute, DefaultCanExecute) {
        }

        /// <summary>
        /// Initializes a new instance of the RelayCommand class Creates a new command.
        /// </summary>
        /// <param name="execute">   The execution logic.</param>
        /// <param name="canExecute">The execution status logic.</param>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute) {
            if (execute == null) {
                throw new ArgumentNullException("execute");
            }

            if (canExecute == null) {
                throw new ArgumentNullException("canExecute");
            }

            this.execute = execute;
            this.canExecute = canExecute;
        }

        #endregion Constructors

        #region ICommand Members

        /// <summary>
        /// An event to raise when the CanExecute value is changed
        /// </summary>
        /// <remarks>
        /// Any subscription to this event will automatically subscribe to both the local
        /// OnCanExecuteChanged method AND the CommandManager RequerySuggested event
        /// </remarks>
        public event EventHandler CanExecuteChanged {
            add {
                CommandManager.RequerySuggested += value;
                this.CanExecuteChangedInternal += value;
            }

            remove {
                CommandManager.RequerySuggested -= value;
                this.CanExecuteChangedInternal -= value;
            }
        }

        /// <summary>
        /// An event to allow the CanExecuteChanged event to be raised manually
        /// </summary>
        private event EventHandler CanExecuteChangedInternal;

        /// <summary>
        /// Defines if command can be executed
        /// </summary>
        /// <param name="parameter">the parameter that represents the validation method</param>
        /// <returns>true if the command can be executed</returns>
        public bool CanExecute(object parameter) {
            return this.canExecute != null && this.canExecute(parameter);
        }

        /// <summary>
        /// Execute the encapsulated command
        /// </summary>
        /// <param name="parameter">the parameter that represents the execution method</param>
        public void Execute(object parameter) {
            this.execute(parameter);
        }

        #endregion ICommand Members

        /// <summary>
        /// Raises the can execute changed.
        /// </summary>
        public void OnCanExecuteChanged() {
            EventHandler handler = this.CanExecuteChangedInternal;
            if (handler != null) {
                //DispatcherHelper.BeginInvokeOnUIThread(() => handler.Invoke(this, EventArgs.Empty));
                handler.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Destroys this instance.
        /// </summary>
        public void Destroy() {
            this.canExecute = _ => false;
            this.execute = _ => { return; };
        }

        /// <summary>
        /// Defines if command can be executed (default behaviour)
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <returns>Always true</returns>
        private static bool DefaultCanExecute(object parameter) {
            return true;
        }
    }
}