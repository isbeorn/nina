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
using System.Windows.Threading;
using System.Diagnostics;
using Nikon;

namespace Nikon
{
    internal class NikonWorkerThread
    {
        Thread _thread;
        NikonTaskQueue _queue;

        internal NikonWorkerThread(string name)
        {
            _queue = new NikonTaskQueue();

            _thread = new Thread(ThreadFunc);
            _thread.Name = name;
            _thread.Start();
        }

        internal int ThreadId
        {
            get { return _thread.ManagedThreadId; }
        }

        void ThreadFunc(object o)
        {
           _queue.Run();
        }

        internal void Shutdown()
        {
            _queue.Shutdown();
            _queue = null;

            _thread.Join();
        }

        void RethrowAsyncExceptionIfAny()
        {
            // Note:
            // An exception thrown by an asynchronus task is considered
            // a fatal event. So once it happens ALL subsequent calls to
            // Invoke, BeginInvoke or SchedulePeriodicTask will rethrow the
            // asynchronous exception on the calling thread.
            if (_queue.AsyncException != null)
            {
                throw _queue.AsyncException;
            }
        }

        internal object Invoke(Delegate d, params object[] args)
        {
            RethrowAsyncExceptionIfAny();

            return _queue.Invoke(d, args);
        }

        internal void BeginInvoke(Delegate d, params object[] args)
        {
            RethrowAsyncExceptionIfAny();

            _queue.BeginInvoke(d, args);
        }

        internal void SchedulePeriodicTask(Delegate d, double interval)
        {
            RethrowAsyncExceptionIfAny();

            _queue.SchedulePeriodicTask(d, interval);
        }
    }

    internal class NikonScheduler
    {
        NikonWorkerThread _worker;
        NikonWorkerThread _callback;
        SynchronizationContext _context;

        internal NikonScheduler()
            : this(null)
        {
        }

        internal NikonScheduler(SynchronizationContext context)
        {
            _worker = new NikonWorkerThread("NikonScheduler worker thread");

            _context = context;

            if (_context == null)
            {
                _callback = new NikonWorkerThread("NikonScheduler callback thread");
            }
        }

        internal int WorkerThreadId
        {
            get { return _worker.ThreadId; }
        }

        internal void AddOrRemoveEvent(Action a)
        {
            if (_context != null)
            {
                _context.Post(new SendOrPostCallback((o) =>
                {
                    a.Invoke();
                }), null);
            }
            else
            {
                // Note:
                // We add or remove events on the callback thread, since this
                // is where events are fired. Also, we use 'BeginInvoke' to avoid
                // deadlocks if the user attempts to hook up an event from within
                // another event handler (example: Hook up ImageReady from within
                // DeviceAdded)
                _callback.BeginInvoke(a);
            }
        }

        internal void Callback(Delegate d, params object[] args)
        {
            if (_context != null)
            {
                _context.Post(new SendOrPostCallback((o) =>
                {
                    d.DynamicInvoke(args);
                }), null);
            }
            else
            {
                _callback.BeginInvoke(d, args);
            }
        }

        internal object Invoke(Delegate d, params object[] args)
        {
            return _worker.Invoke(d, args);
        }

        internal object Invoke(Action a)
        {
            return Invoke((Delegate)a);
        }

        internal void BeginInvoke(Delegate d, params object[] args)
        {
            _worker.BeginInvoke(d, args);
        }

        internal void BeginInvoke(Action a)
        {
            BeginInvoke((Delegate)a);
        }

        internal void SchedulePeriodicTask(Delegate d, double interval)
        {
            _worker.SchedulePeriodicTask(d, interval);
        }

        internal void SchedulePeriodicTask(Action a, double interval)
        {
            SchedulePeriodicTask((Delegate)a, interval);
        }

        internal void Shutdown()
        {
            _worker.Shutdown();

            if (_callback != null)
            {
                _callback.Shutdown();
            }
        }
    }
}
