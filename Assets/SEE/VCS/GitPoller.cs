using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using LibGit2Sharp;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.GO;
using SEE.GraphProviders;
using SEE.UI.Notification;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.VCS
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
        public SEECity CodeCity;

        /// <summary>
        /// The full paths to the repositories that should be watched for updates.
        /// </summary>
        [ShowInInspector] public HashSet<string> WatchedRepositories = new();

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
        private Dictionary<string, List<string>> RepositoriesTipHashes = new();

        /// <summary>
        /// MarkerFactory for generating node markers.
        /// </summary>
        private MarkerFactory markerFactory;

        /// <summary>
        /// Specifies that the poller should not run currently.
        /// This is set to true when git fetch is in progress.
        /// </summary>
        private bool doNotPool = false;

        /// <summary>
        /// Runs git fetch on all remotes for all branches.
        /// </summary>
        private void RunGitFetch()
        {
            foreach (string repoPath in WatchedRepositories)
            {
                using Repository repo = new(repoPath);
                // Fetch all remote branches
                foreach (Remote remote in repo.Network.Remotes)
                {
                    IEnumerable<string> refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                    try
                    {
                        Commands.Fetch(repo, remote.Name, refSpecs, null, "");
                    }
                    catch (LibGit2SharpException e)
                    {
                        Debug.LogError($"Error while running git fetch for repository path {repoPath} and remote name {remote.Name}: {e.Message}.\n");
                    }
                }
            }
        }

        /// <summary>
        /// Gets the hashes of all tip commits from all branches in all watched repositories.
        /// </summary>
        /// <returns>A mapping from a repository path to a list of the hashes of all tip
        /// commits.</returns>
        private Dictionary<string, List<string>> GetTipHashes()
        {
            Dictionary<string, List<string>> result = new();
            foreach (string repoPath in WatchedRepositories)
            {
                using Repository repo = new Repository(repoPath);
                result.Add(repoPath, repo.Branches.Select(x => x.Tip.Sha).ToList());
            }

            return result;
        }

        /// <summary>
        /// Starts the actual poller.
        /// </summary>
        private void Start()
        {
            if (WatchedRepositories.Count == 0)
            {
                Debug.Log("No watched repositories.\n");
                return;
            }

            markerFactory = new MarkerFactory(CodeCity.MarkerAttributes);

            InitialPoll().Forget();
            return;

            async UniTaskVoid InitialPoll()
            {
                RepositoriesTipHashes = await UniTask.RunOnThreadPool(GetTipHashes);
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
        /// This method will fetch the newest commits and if new commits exist the
        /// code city is refreshed.
        /// </summary>
        private async UniTaskVoid PollReposAsync()
        {
            if (!doNotPool)
            {
                doNotPool = true;
                Dictionary<string, List<string>> newHashes = await UniTask.RunOnThreadPool(() =>
                {
                    RunGitFetch();
                    return GetTipHashes();
                });

                if (!newHashes.All(x => RepositoriesTipHashes[x.Key].SequenceEqual(x.Value)))
                {
                    ShowNewCommitsMessage();

                    RepositoriesTipHashes = newHashes;
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
                        out _,
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

                    Invoke(nameof(RemoveMarker), MarkerTime);
                }

                doNotPool = false;
            }
        }

        /// <summary>
        /// Removes all markers.
        /// </summary>
        private void RemoveMarker() => markerFactory.Clear();
    }
}
