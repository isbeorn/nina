//
// This work is licensed under a Creative Commons Attribution 3.0 Unported License.
//
// Thomas Dideriksen (thomas@dideriksen.com)
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using Timer = System.Timers.Timer;

namespace Nikon
{
    internal class NikonTaskQueue
    {
        Queue<NikonTask> _tasks;
        bool _shuttingDown;
        AutoResetEvent _haveTask;
        List<AutoResetEvent> _taskDoneEvents;
        List<Timer> _timers;
        Exception _asyncException;

        internal NikonTaskQueue()
        {
            _tasks = new Queue<NikonTask>();
            _shuttingDown = false;
            _haveTask = new AutoResetEvent(false);
            _taskDoneEvents = new List<AutoResetEvent>();
            _timers = new List<Timer>();
            _asyncException = null;
        }

        public void SchedulePeriodicTask(Delegate d, double interval)
        {
            Timer timer = new Timer(interval);
            timer.AutoReset = true;
            timer.Elapsed += (s, e) =>
            {
                // Start asynchronous task every time the timer elapses
                BeginInvoke(d);
            };

            lock (_timers)
            {
                _timers.Add(timer);
            }

            timer.Start();
        }

        public void BeginInvoke(Delegate d, params object[] args)
        {
            // Schedule asynchronous task

            NikonTask task = new NikonTask(d, args);

            EnqueueTask(task);
        }

        public object Invoke(Delegate d, params object[] args)
        {
            // Schedule synchronous task

            AutoResetEvent taskDone = new AutoResetEvent(false);

            lock (_taskDoneEvents)
            {
                _taskDoneEvents.Add(taskDone);
            }

            NikonTask task = new NikonTask(d, args, taskDone);

            EnqueueTask(task);

            taskDone.WaitOne();

            lock (_taskDoneEvents)
            {
                _taskDoneEvents.Remove(taskDone);
            }

            // If an exception occurred during execution of the
            // task (on the worker thread), re-throw it here, on
            // the waiting thread.
            if (task.Exception != null)
            {
                throw task.Exception;
            }

            return task.Result;
        }

        internal Exception AsyncException
        {
            get { return _asyncException; }
        }

        void EnqueueTask(NikonTask task)
        {
            // Add task to queue
            lock (_tasks)
            {
                _tasks.Enqueue(task);
            }

            // Signal the run loop that we have a pending task
            _haveTask.Set();
        }

        public void Run()
        {
            // Enter the run loop
            while (!_shuttingDown)
            {
                // Get next task in queue
                NikonTask task = null;

                lock (_tasks)
                {
                    if (_tasks.Count > 0)
                    {
                        task = _tasks.Dequeue();
                    }
                }

                // If we have a task, execute it. Otherwise wait for a new task to arrive
                if (task != null)
                {
                    task.Execute();

                    // Store first exceptions caused by an asynchronous task
                    if (task.Exception != null && !task.IsSynchronous && _asyncException == null)
                    {
                        Interlocked.Exchange<Exception>(ref _asyncException, task.Exception);
                    }
                }
                else
                {
                    _haveTask.WaitOne();
                }
            }
        }

        public void Shutdown()
        {
            // Set 'shutting down' flag
            _shuttingDown = true;

            // Stop all timers
            lock (_timers)
            {
                foreach (Timer timer in _timers)
                {
                    timer.Stop();
                }
            }

            // Stop waiting for all synchronous tasks
            lock (_taskDoneEvents)
            {
                foreach (AutoResetEvent taskDone in _taskDoneEvents)
                {
                    taskDone.Set();
                }
            }

            // Pretend we have a new task. In case we're waiting for
            // tasks, this will force an iteration in the run loop,
            // causing it to check the '_shuttingDown' flag.
            _haveTask.Set();
        }
    }

    internal class NikonTask
    {
        Delegate _delegate;
        object[] _args;
        AutoResetEvent _done;
        Exception _exception;
        object _result;

        internal NikonTask(Delegate d, object[] args)
            : this(d, args, null)
        {
        }

        internal NikonTask(Delegate d, object[] args, AutoResetEvent done)
        {
            _delegate = d;
            _args = args;
            _done = done;
            _exception = null;
            _result = null;
        }

        internal bool IsSynchronous
        {
            get { return _done != null; }
        }

        internal Exception Exception
        {
            get { return _exception; }
        }

        internal object Result
        {
            get { return _result; }
        }

        internal void Execute()
        {
            try
            {
                _result = _delegate.DynamicInvoke(_args);
            }
            catch (Exception ex)
            {
                _exception = FindFirstNonTargetInvocationException(ex);
            }

            if (IsSynchronous)
            {
                // This is a synchronous task. Signal the waiting thread
                // that we are done.
                Debug.Assert(_done != null);
                _done.Set();
            }
        }

        Exception FindFirstNonTargetInvocationException(Exception ex)
        {
            // Note:
            // When exceptions are thrown inside a function invoked via
            // a DynamicInvoke, the actual exceptions (that contain the
            // interesting information about the error) is wrapped inside
            // one or more TargetInvocationExceptions.

            Exception result = ex;

            while (result is TargetInvocationException)
            {
                result = result.InnerException;
            }

            return (result == null) ? ex : result;
        }
    }
}
