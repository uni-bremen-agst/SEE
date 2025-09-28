using Cysharp.Threading.Tasks;
using SEE.Utils;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.GraphProviders;
using SEE.UI.Notification;
using SEE.VCS;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
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
    /// This component will be added automatically by <see cref="GitBranchesGraphProvider"/>
    /// if <see cref="GitBranchesGraphProvider.AutoFetch"/> is set to true.
    /// </summary>
    public class GitPoller : PollerBase
    {
        /// <summary>
        /// The code city where the <see cref="GitBranchesGraphProvider"/> graph provider
        /// was executed and which should be updated when a new commit is detected.
        /// </summary>
        public BranchCity CodeCity;

        /// <summary>
        /// The time in seconds for how long the node markers should be shown for newly
        /// added or modified nodes.
        /// </summary>
        public int MarkerTime = 10;

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
        /// Starts the actual poller.
        /// </summary>
        public override void Start()
        {
            base.Start();

            if (Repository == null)
            {
                Debug.Log("No watched repositories.\n");
                return;
            }

            markerFactory = new MarkerFactory(CodeCity.MarkerAttributes);

            timer.Elapsed += OnTimedEvent;
        }

        /// <summary>
        /// Executed on every timer event. Runs the <see cref="PollReposAsync"/> method.
        /// </summary>
        private void OnTimedEvent(object source, ElapsedEventArgs events)
        {
            PollReposAsync().Forget();
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
                    ShowNewCommitsMessage();

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

                    // Removed nodes are not marked, because they are not in the graph anymore.
                    // In the futre, we may want animate their removal.
                    ShowRemovedNodes(removedNodes);
                    RemoveMarkerAsync().Forget();
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
        /// Removes all markers after <see cref="MarkerTime"/> seconds.
        /// </summary>
        private async UniTaskVoid RemoveMarkerAsync()
        {
            await Task.Delay(MarkerTime);
            markerFactory.Clear();
        }
    }
}
