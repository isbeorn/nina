#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

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
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.Utility {

    public abstract class AsyncCommandBase : BaseINPC, IAsyncCommand {

        public abstract bool CanExecute(object parameter);

        public abstract Task ExecuteAsync(object parameter);

        public async void Execute(object parameter) {
            await ExecuteAsync(parameter);
        }

        public event EventHandler CanExecuteChanged {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        protected void RaiseCanExecuteChanged() {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public interface IAsyncCommand : ICommand {

        Task ExecuteAsync(object parameter);
    }

    public class AsyncCommand<TResult> : AsyncCommandBase {
        private readonly Func<object, Task<TResult>> _command;
        private NotifyTaskCompletion<TResult> _execution;

        /// <summary>
        /// Encapsulated the representation for the validation of the execute method
        /// </summary>
        private Predicate<object> _canExecute;

        /// <summary>
        /// Defines if command can be executed (default behaviour)
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <returns>Always true</returns>
        private static bool DefaultCanExecute(object parameter) {
            return true;
        }

        public AsyncCommand(Func<object, Task<TResult>> command) {
            _command = command;
            _canExecute = DefaultCanExecute;
        }

        public AsyncCommand(Func<object, Task<TResult>> command, Predicate<object> canExecute) {
            _command = command;
            _canExecute = canExecute;
        }

        public AsyncCommand(Func<Task<TResult>> command) {
            _command = (object o) => {
                return command();
            };
            _canExecute = DefaultCanExecute;
        }

        public AsyncCommand(Func<Task<TResult>> command, Predicate<object> canExecute) {
            _command = (object o) => {
                return command();
            };
            _canExecute = canExecute;
        }

        public override bool CanExecute(object parameter) {
            return (this._canExecute != null && this._canExecute(parameter)) && (Execution == null || Execution.IsCompleted);
        }

        public override async Task ExecuteAsync(object parameter) {
            Execution = new NotifyTaskCompletion<TResult>(_command(parameter));
            RaiseCanExecuteChanged();
            if (!Execution.IsCompleted) {
                await Execution.TaskCompletion;
            }
            RaiseCanExecuteChanged();
        }

        // Raises PropertyChanged
        public NotifyTaskCompletion<TResult> Execution { get { return _execution; } private set { _execution = value; RaisePropertyChanged(); } }
    }
}