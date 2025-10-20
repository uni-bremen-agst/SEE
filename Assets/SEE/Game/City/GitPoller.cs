using Cysharp.Threading.Tasks;
using SEE.GraphProviders;
using SEE.Utils;
using SEE.VCS;
using System.Timers;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// <see cref="GitPoller"/> is used to regularly fetch for new changes in a given
    /// <see cref="Repository"/>.
    ///
    /// When a new commit was detected on any branch, a refresh of the CodeCity is initiated.
    /// Newly added or changed nodes will be marked after the refresh.
    ///
    /// This component will be added automatically by <see cref="GitBranchesGraphProvider"/>
    /// if <see cref="GitBranchesGraphProvider.AutoFetch"/> is set to true.
    /// </summary>
    public class GitPoller : PollerBase
    {
        /// <summary>
        /// Specifies that the poller should currently not run.
        /// This is set to true when git fetch is in progress.
        /// </summary>
        private bool doNotPoll = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pollingInterval">time in seconds for the polling period</param>
        /// <param name="gitRepository">repository to be polled</param>
        public GitPoller(int pollingInterval, GitRepository gitRepository)
        {
            PollingInterval = pollingInterval;
            Repository = gitRepository;
        }

        /// <summary>
        /// Releases the reference to the <see cref="Repository"/>.
        /// </summary>
        ~GitPoller()
        {
            Stop();
            Repository = null;
        }

        /// <summary>
        /// The <see cref="GitRepository"/> to poll.
        /// </summary>
        private GitRepository Repository { set; get; }

        /// <summary>
        /// Starts the actual poller. Observers on <see cref="OnChangeDetected"/> will be
        /// notified when a change is detected. The repository will be fetched periodically
        /// every <see cref="PollingInterval"/> seconds.
        /// </summary>
        public override void Start()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{nameof(GitPoller)} works only while the game is playing.\n");
                return;
            }

            base.Start();

            if (Repository == null)
            {
                Debug.Log("No watched repositories.\n");
                return;
            }
            Debug.Log($"Starting GitPoller on {Repository.RepositoryPath.Path}...\n");
            timer.Elapsed += OnTimedEvent;
        }

        /// <summary>
        /// Stops the poller. Observers on <see cref="OnChangeDetected"/> will no longer be
        /// notified.
        /// </summary>
        public override void Stop()
        {
            if (timer != null)
            {
                Debug.Log($"Stopping GitPoller on {Repository.RepositoryPath.Path}...\n");
                timer.Elapsed -= OnTimedEvent;
            }
            base.Stop();
        }

        /// <summary>
        /// Delegate to be called when a change was detected in the repository.
        /// </summary>
        public delegate void ChangeDetected();

        /// <summary>
        /// Occurs when a change is detected in the monitored <see cref="Repository"/>.
        /// </summary>
        /// <remarks>Subscribe to this event to handle changes as they occur. The event is triggered
        /// whenever the poller detects a relevant change, and the associated event handler will be invoked.</remarks>
        public event ChangeDetected OnChangeDetected;

        /// <summary>
        /// Executed on every timer event. Runs the <see cref="PollReposAsync"/> method.
        /// </summary>
        private void OnTimedEvent(object source, ElapsedEventArgs events)
        {
            PollReposAsync().Forget();
        }

        /// <summary>
        /// Is called in every <see cref="PollingInterval"/> seconds.
        ///
        /// This method will fetch the newest commits of all remote branches of all remote
        /// repository and, if new commits exist, the code city is refreshed.
        /// </summary>
        private async UniTaskVoid PollReposAsync()
        {
            if (!doNotPoll)
            {
                doNotPoll = true;
                bool needsUpdate = await UniTask.RunOnThreadPool(() =>
                {
                    return Repository.FetchRemotes();
                });

                if (needsUpdate)
                {
                    Debug.Log($"Change detected in repository {Repository.RepositoryPath.Path}...\n");
                    OnChangeDetected?.Invoke();
                }

                doNotPoll = false;
            }
        }
    }
}
