using System.Collections;

namespace SEE.Utils
{
    /// <summary>
    /// A job in a new thread.
    /// Code by Bunny83 November 2012
    /// https://answers.unity.com/questions/357033/unity3d-and-c-coroutines-vs-threading.html
    /// </summary>
    public class ThreadJob
    {
        /// <summary>
        /// Whether the job was done.
        /// </summary>
        private bool isDone = false;

        /// <summary>
        /// A handle for synchronization.
        /// </summary>
        private readonly object handle = new object();

        /// <summary>
        /// The thread executing the job.
        /// </summary>
        private System.Threading.Thread thread = null;

        /// <summary>
        /// The status of the thread.
        /// </summary>
        public bool IsDone
        {
            get
            {
                bool tmp;
                lock (handle)
                {
                    tmp = isDone;
                }
                return tmp;
            }
            set
            {
                lock (handle)
                {
                    isDone = value;
                }
            }
        }

        /// <summary>
        /// Starts a job in a new thread.
        /// </summary>
        public virtual void Start()
        {
            thread = new System.Threading.Thread(Run);
            thread.Start();
        }

        /// <summary>
        /// Aborts the job.
        /// </summary>
        public virtual void Abort()
        {
            thread.Abort();
        }

        /// <summary>
        /// The thread function, should be overwritten by a specific implementation.
        /// </summary>
        protected virtual void ThreadFunction() { }

        /// <summary>
        /// That function is called on the end.
        /// </summary>
        protected virtual void OnFinished() { }

        public virtual bool Update()
        {
            if (IsDone)
            {
                OnFinished();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Used to manage the thread in a coroutine, e.g.,
        ///  yield return StartCoroutine(myJob.WaitFor());
        /// </summary>
        /// <returns>co-routine enumerator</returns>
        public IEnumerator WaitFor()
        {
            while (!Update())
            {
                yield return null;
            }
        }

        /// <summary>
        /// Runs the <see cref="ThreadFunction"/>.
        /// </summary>
        private void Run()
        {
            ThreadFunction();
            IsDone = true;
        }
    }
}