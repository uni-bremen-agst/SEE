using System;
using System.Threading;
using JetBrains.Annotations;

#if NETFX_CORE
using System.Threading.Tasks;
#endif

namespace Dissonance.Threading
{
    internal interface IThread
    {
        bool IsStarted { get; }

        void Start();

        void Join();
    }

#if NETFX_CORE
    internal class DThread
        : IThread
    {
        private readonly System.Threading.Tasks.Task _task;

        private readonly ManualResetEvent _finishedEvent = new ManualResetEvent(false);

        public bool IsStarted { get; private set; }

        public DThread(Action action)
        {
            _task = new System.Threading.Tasks.Task(action, System.Threading.Tasks.TaskCreationOptions.LongRunning);
            _task.ContinueWith(_ => {
                _finishedEvent.Set();
            });
        }

        public void Start()
        {
            _task.Start();
            IsStarted = true;
        }

        public void Join()
        {
            _finishedEvent.WaitOne();
        }
    }
#else
    internal class DThread
        : IThread
    {
        private readonly Thread _thread;

        public bool IsStarted { get; private set; }

        public DThread([NotNull] Action action)
        {
            _thread = new Thread(new ThreadStart(action));
        }

        public void Start()
        {
            _thread.Start();
            IsStarted = true;
        }

        public void Join()
        {
            if (_thread.ThreadState != ThreadState.Unstarted)
                _thread.Join();
        }
    }
#endif
}
