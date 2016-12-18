using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
        private readonly Func<Task<TResult>> _command;
        private NotifyTaskCompletion<TResult> _execution;
        
        public AsyncCommand(Func<Task<TResult>> command) {
            _command = command;
        }
        public override bool CanExecute(object parameter) {
            return Execution == null || Execution.IsCompleted;
        }
        public override async Task ExecuteAsync(object parameter) {
            Execution = new NotifyTaskCompletion<TResult>(_command());
            RaiseCanExecuteChanged();
            await Execution.TaskCompletion;
            RaiseCanExecuteChanged();
        }
        // Raises PropertyChanged        
        public NotifyTaskCompletion<TResult> Execution { get { return _execution; } private set { _execution = value; RaisePropertyChanged(); } }        
    }
}
