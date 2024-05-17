using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using LibGit2Sharp;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.UI.Notification;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.GO
{
    public class GitPoller : MonoBehaviour
    {
        public SEECity CodeCity;


        [ShowInInspector] public HashSet<string> WatchedRepositories = new();

        public int PollingInterval = 5;

        public int MarkerTime = 10;

        private Dictionary<string, List<string>> RepositoriesTipHashes = new();


        private MarkerFactory markerFactory;

        private void RunGitFetch()
        {
            foreach (var repoPath in WatchedRepositories)
            {
                using (var repo = new Repository(repoPath))
                {
                    Debug.Log($"Fetch Repo {repoPath}");
                    // Fetch all remote branches
                    var remote = repo.Network.Remotes["origin"];
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

        private void ShowNewCommitsMessage()
        {
            Debug.Log("New commits");
            ShowNotification.Info("New commits detected", "Refreshing code city");
        }

        /// <summary>
        /// Allows the comparison of two instances of <see cref="Node"/> from different graphs.
        /// </summary>
        private static readonly NodeEqualityComparer nodeEqualityComparer = new();

        async UniTaskVoid PollReposAsync()
        {
            Debug.Log("Poll");
            Dictionary<string, List<string>> newHashes = await UniTask.RunOnThreadPool(() =>
            {
                RunGitFetch();
                return GetTipHashes();
            });


            if (!newHashes.All(x => RepositoriesTipHashes[x.Key].SequenceEqual(x.Value)))
            {
                ShowNewCommitsMessage();
                Graph oldGraph = CodeCity.LoadedGraph.Clone() as Graph;
                // CodeCity.Reset();
                await CodeCity.LoadDataAsync(); // LoadAndDrawGraphAsync().Forget();
                try
                {
                    CodeCity.ReDrawGraph();
                }
                catch (Exception e)
                {
                }

                ISet<Node> addedNodes;
                ISet<Node> changedNodes;
                ISet<Node> removedNodes;
                ISet<Node> equalNodes;

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


            RepositoriesTipHashes = newHashes;
        }

        private void RemoveMarker()
        {
            Debug.Log("Remove Marker");
            markerFactory.Clear();
        }
    }
}
