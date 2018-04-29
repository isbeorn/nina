using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace NINA.Utility {

    public sealed class NotifyTaskCompletion<TResult> : INotifyPropertyChanged {

        public NotifyTaskCompletion(Task<TResult> task) {
            Task = task;
            if (!task.IsCompleted)
                TaskCompletion = WatchTaskAsync(task);
        }

        public Task TaskCompletion { get; private set; }

        private async Task WatchTaskAsync(Task task) {
            try {
                await task;
            } catch (Exception ex) {
                Logger.Error(ex);
            }
            var propertyChanged = PropertyChanged;
            if (propertyChanged == null)
                return;
            propertyChanged(this, new PropertyChangedEventArgs(nameof(Status)));
            propertyChanged(this, new PropertyChangedEventArgs(nameof(IsCompleted)));
            propertyChanged(this, new PropertyChangedEventArgs(nameof(IsNotCompleted)));
            if (task.IsCanceled) {
                propertyChanged(this, new PropertyChangedEventArgs(nameof(IsCanceled)));
            } else if (task.IsFaulted) {
                propertyChanged(this, new PropertyChangedEventArgs(nameof(IsFaulted)));
                propertyChanged(this, new PropertyChangedEventArgs(nameof(Exception)));
                propertyChanged(this,
                  new PropertyChangedEventArgs(nameof(InnerException)));
                propertyChanged(this, new PropertyChangedEventArgs(nameof(ErrorMessage)));
            } else {
                propertyChanged(this,
                  new PropertyChangedEventArgs(nameof(IsSuccessfullyCompleted)));
                propertyChanged(this, new PropertyChangedEventArgs(nameof(Result)));
            }
        }

        public Task<TResult> Task { get; private set; }

        public TResult Result {
            get {
                return (Task.Status == TaskStatus.RanToCompletion) ?
Task.Result : default(TResult);
            }
        }

        public TaskStatus Status { get { return Task.Status; } }
        public bool IsCompleted { get { return Task.IsCompleted; } }
        public bool IsNotCompleted { get { return !Task.IsCompleted; } }

        public bool IsSuccessfullyCompleted {
            get {
                return Task.Status ==
TaskStatus.RanToCompletion;
            }
        }

        public bool IsCanceled { get { return Task.IsCanceled; } }
        public bool IsFaulted { get { return Task.IsFaulted; } }
        public AggregateException Exception { get { return Task.Exception; } }

        public Exception InnerException {
            get {
                return (Exception == null) ?
null : Exception.InnerException;
            }
        }

        public string ErrorMessage {
            get {
                return (InnerException == null) ?
null : InnerException.Message;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}