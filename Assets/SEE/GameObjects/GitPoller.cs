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

namespace SEE.GameObjects
{
    /// <summary>
    /// GitPoller is used to regularly fetch for new changes in the git repositories specified im <see cref="WatchedRepositories"/>.
    ///
    /// When a new commit was detected on any branch a refresh of the CodeCity is initiated.
    /// Newly added or changed nodes will be marked after the refresh.
    ///
    /// This component will be added automatically by <see cref="AllGitBranchesSingleGraphProvider"/> when <see cref="AllGitBranchesSingleGraphProvider.AutoFetch"/> is set to true.
    /// </summary>
    public class GitPoller : MonoBehaviour
    {
        /// <summary>
        /// The code city where the <see cref="AllGitBranchesSingleGraphProvider"/> graph provider was executed and
        /// which should be updated when a new commit is detected.
        /// </summary>
        public SEECity CodeCity;

        /// <summary>
        /// The full paths to the repositories which should be watched for updates
        /// </summary>
        [ShowInInspector] public HashSet<string> WatchedRepositories = new();

        /// <summary>
        /// The interval in seconds in which git should fetch
        /// </summary>
        public int PollingInterval = 5;

        /// <summary>
        /// The time in seconds for how long the node markers should be shown for newly added or modified nodes. 
        /// </summary>
        public int MarkerTime = 10;

        /// <summary>
        /// Maps the repository (path) to a list of all hashes of the branches from the repository 
        /// </summary>
        private Dictionary<string, List<string>> RepositoriesTipHashes = new();

        /// <summary>
        /// MarkerFactory for generating node markers
        /// </summary>
        private MarkerFactory markerFactory;

        /// <summary>
        /// Runs git fetch on all remotes for all branches
        /// </summary>
        private void RunGitFetch()
        {
            foreach (var repoPath in WatchedRepositories)
            {
                using (var repo = new Repository(repoPath))
                {
                    Debug.Log($"Fetch Repo {repoPath}");
                    // Fetch all remote branches
                    foreach (var remote in repo.Network.Remotes)
                    {
                        var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                        try
                        {
                            Commands.Fetch(repo, remote.Name, refSpecs, null, "");
                        }
                        catch (LibGit2SharpException e)
                        {
                            Debug.LogError($"Error while running git fetch : {e.Message}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the hashes of all tip commits from all branches in all watched repositories
        /// </summary>
        /// <returns>A Mapping from a repository path to a list of the hashes of all tip commits</returns>
        private Dictionary<string, List<string>> GetTipHashes()
        {
            Dictionary<string, List<string>> result = new();
            foreach (var repoPath in WatchedRepositories)
            {
                using (var repo = new Repository(repoPath))
                {
                    result.Add(repoPath, repo.Branches.Select(x => x.Tip.Sha).ToList());
                }
            }

            return result;
        }

        public void Start()
        {
            if (WatchedRepositories.Count == 0)
            {
                Debug.Log("No watched repositories");
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
        /// Shows a message to the user, that a new commit was detected.
        /// </summary>
        private void ShowNewCommitsMessage()
        {
            Debug.Log("New commits detected");
            ShowNotification.Info("New commits detected", "Refreshing code city");
        }

        /// <summary>
        /// Allows the comparison of two instances of <see cref="Node"/> from different graphs.
        /// </summary>
        private static readonly NodeEqualityComparer nodeEqualityComparer = new();

        /// <summary>
        /// Is called in every <see cref="PollingInterval"/> seconds.
        ///
        /// This method will fetch the newest commits and if new commits exist the code city is refreshed.
        /// </summary>
        async UniTaskVoid PollReposAsync()
        {
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

                ISet<Node> addedNodes;
                ISet<Node> changedNodes;

                CodeCity.LoadedGraph.Diff(oldGraph,
                    g => g.Nodes(),
                    (g, id) => g.GetNode(id),
                    GraphExtensions.AttributeDiff(CodeCity.LoadedGraph, oldGraph),
                    nodeEqualityComparer,
                    out addedNodes,
                    out _,
                    out changedNodes,
                    out _);
                Debug.Log($"{changedNodes.Count} changed nodes");

                foreach (var changedNode in changedNodes)
                {
                    markerFactory.MarkChanged(GraphElementIDMap.Find(changedNode.ID, true));
                }

                foreach (var addedNode in addedNodes)
                {
                    markerFactory.MarkBorn(GraphElementIDMap.Find(addedNode.ID, true));
                }

                Invoke(nameof(RemoveMarker), MarkerTime);
            }
        }

        /// <summary>
        /// This method will remove all markers after <see cref="MarkerTime"/> seconds.
        /// </summary>
        private void RemoveMarker() => markerFactory.Clear();
    }
}
