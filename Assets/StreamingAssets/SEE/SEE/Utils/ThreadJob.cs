using System.Collections;


/// <summary>
/// An abstract Class for a job in a new Thread.
/// Code by Bunny83 November 2012
/// https://answers.unity.com/questions/357033/unity3d-and-c-coroutines-vs-threading.html
/// </summary>
public class ThreadJob
{
    private bool m_IsDone = false;
    private object m_Handle = new object();
    private System.Threading.Thread m_Thread = null;

    /// <summary>
    /// The status of the thread
    /// </summary>
    public bool IsDone
    {
        get
        {
            bool tmp;
            lock (m_Handle)
            {
                tmp = m_IsDone;
            }
            return tmp;
        }
        set
        {
            lock (m_Handle)
            {
                m_IsDone = value;
            }
        }
    }

    /// <summary>
    /// starts a job in a new thread
    /// </summary>
    public virtual void Start()
    {
        m_Thread = new System.Threading.Thread(Run);
        m_Thread.Start();
    }

    /// <summary>
    /// Aborts the job
    /// </summary>
    public virtual void Abort()
    {
        m_Thread.Abort();
    }

    /// <summary>
    /// The thread function, should be overwritten by a specific implementation
    /// </summary>
    protected virtual void ThreadFunction() { }

    /// <summary>
    /// That function is called on the end
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
    /// Used to manage the thread in a coroutine 
    ///  yield return StartCoroutine(myJob.WaitFor());
    /// </summary>
    /// <returns></returns>
    public IEnumerator WaitFor()
    {
        while (!Update())
        {
            yield return null;
        }
    }

    /// <summary>
    /// Runs the Threadfunction
    /// </summary>
    private void Run()
    {
        ThreadFunction();
        IsDone = true;
    }
}