using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.GraphProviders;
using SEE.UI.Notification;
using SEE.VCS;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// <see cref="GitPoller"/> is used to regularly fetch for new changes in the
    /// git repositories specified im <see cref="WatchedRepositories"/>.
    ///
    /// When a new commit was detected on any branch, a refresh of the CodeCity is initiated.
    /// Newly added or changed nodes will be marked after the refresh.
    ///
    /// This component will be added automatically by <see cref="AllGitBranchesSingleGraphProvider"/>
    /// if <see cref="AllGitBranchesSingleGraphProvider.AutoFetch"/> is set to true.
    /// </summary>
    public class GitPoller : MonoBehaviour
    {
        /// <summary>
        /// The code city where the <see cref="AllGitBranchesSingleGraphProvider"/> graph provider
        /// was executed and which should be updated when a new commit is detected.
        /// </summary>
        public BranchCity CodeCity;

        /// <summary>
        /// The interval in seconds in which git should be polled.
        /// </summary>
        public int PollingInterval = 5;

        /// <summary>
        /// The time in seconds for how long the node markers should be shown for newly
        /// added or modified nodes.
        /// </summary>
        public int MarkerTime = 10;

        /// <summary>
        /// Maps the repository (path) to a list of all hashes of the branches from
        /// the repository.
        /// </summary>
        private IList<string> TipHashes = new List<string>();

        /// <summary>
        /// MarkerFactory for generating node markers.
        /// </summary>
        private MarkerFactory markerFactory;

        /// <summary>
        /// Specifies that the poller should currently not run.
        /// This is set to true when git fetch is in progress.
        /// </summary>
        private bool doNotPoll = false;

        /// <summary>
        /// The <see cref="GitRepository"/> to poll.
        /// </summary>
        public GitRepository Repository { set; private get; }

        /// <summary>
        /// Runs git fetch on all remotes for all branches.
        /// </summary>
        private void RunGitFetch()
        {
            Repository.FetchRemotes();
        }

        /// <summary>
        /// Gets the hashes of all tip commits from all branches in <see cref="Repository"/>.
        /// </summary>
        /// <returns>All tip commits.</returns>
        private IList<string> GetTipHashes()
        {
            return Repository.GetTipHashes();
        }

        /// <summary>
        /// Starts the actual poller.
        /// </summary>
        private void Start()
        {
            if (Repository == null)
            {
                Debug.Log("No watched repositories.\n");
                return;
            }

            markerFactory = new MarkerFactory(CodeCity.MarkerAttributes);

            InitialPoll().Forget();
            return;

            async UniTaskVoid InitialPoll()
            {
                TipHashes = await UniTask.RunOnThreadPool(GetTipHashes);
                InvokeRepeating(nameof(PollReposAsync), PollingInterval, PollingInterval);
            }
        }

        /// <summary>
        /// Shows a message to the user that a new commit was detected.
        /// </summary>
        private void ShowNewCommitsMessage()
        {
            ShowNotification.Info("New commits detected", "Refreshing code city.");
        }

        /// <summary>
        /// Allows the comparison of two instances of <see cref="Node"/> from different graphs.
        /// </summary>
        private static readonly NodeEqualityComparer nodeEqualityComparer = new();

        /// <summary>
        /// Is called in every <see cref="PollingInterval"/> seconds.
        ///
        /// This method will fetch the newest commits and, if new commits exist, the
        /// code city is refreshed.
        /// </summary>
        private async UniTaskVoid PollReposAsync()
        {
            if (!doNotPoll)
            {
                doNotPoll = true;
                IList<string> newHashes = await UniTask.RunOnThreadPool(() =>
                {
                    RunGitFetch();
                    return GetTipHashes();
                });

                if (!newHashes.SequenceEqual(TipHashes))
                {
                    ShowNewCommitsMessage();

                    TipHashes = newHashes;
                    // Backup old graph
                    Graph oldGraph = CodeCity.LoadedGraph.Clone() as Graph;
                    await CodeCity.LoadDataAsync();
                    try
                    {
                        CodeCity.ReDrawGraph();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }

                    CodeCity.LoadedGraph.Diff(oldGraph,
                        g => g.Nodes(),
                        (g, id) => g.GetNode(id),
                        GraphExtensions.AttributeDiff(CodeCity.LoadedGraph, oldGraph),
                        nodeEqualityComparer,
                        out ISet<Node> addedNodes,
                        out ISet<Node> removedNodes,
                        out ISet<Node> changedNodes,
                        out _);

                    foreach (Node changedNode in changedNodes)
                    {
                        markerFactory.MarkChanged(GraphElementIDMap.Find(changedNode.ID, true));
                    }

                    foreach (Node addedNode in addedNodes)
                    {
                        markerFactory.MarkBorn(GraphElementIDMap.Find(addedNode.ID, true));
                    }

                    ShowRemovedNodes(removedNodes);

                    // Removed nodes are not marked, because they are not in the graph anymore.
                    // But we may want animate their removal.

                    Invoke(nameof(RemoveMarker), MarkerTime);
                }

                doNotPoll = false;
            }
        }

        /// <summary>
        /// Notifies the user about removed files in the repository.
        /// </summary>
        /// <param name="removedNodes">The list of removed nodes.</param>
        private void ShowRemovedNodes(ISet<Node> removedNodes)
        {
            foreach (Node removedNode in removedNodes)
            {
                ShowNotification.Info("File removed", $"File {removedNode.ID} was removed from the repository {Repository.RepositoryPath.Path}.");
            }
        }

        /// <summary>
        /// Removes all markers.
        /// </summary>
        private void RemoveMarker() => markerFactory.Clear();
    }
}
