using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using SEE.GraphProviders;
using SEE.UI.Notification;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SEE.Game.CityRendering
{
    /// <summary>
    /// Renders transitions in a <see cref="BranchCity"/> when new commits are detected.
    /// </summary>
    public class TransitionRenderer
    {
        /// <summary>
        /// The time in seconds for how long the node markers should be shown for newly
        /// added or modified nodes.
        /// </summary>
        public int MarkerTime = 10;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="branchCity">the city to be re-drawn</param>
        /// <param name="poller">the poller notifying when there are changes</param>
        /// <param name="markerTime">the time in seconds the new markers should be drawn for;
        /// after that they will be removed again</param>
        public TransitionRenderer(BranchCity branchCity, GitPoller poller, int markerTime)
        {
            this.branchCity = branchCity;
            this.MarkerTime = markerTime;
            this.poller = poller;
            markerFactory = new MarkerFactory(this.branchCity.MarkerAttributes);
            poller.OnChangeDetected += Render;
        }

        /// <summary>
        /// Finalizer to unsubscribe from the poller event.
        /// </summary>
        ~TransitionRenderer()
        {
            poller.OnChangeDetected -= Render;
        }

        /// <summary>
        /// The poller notifying when there are changes.
        /// </summary>
        private readonly GitPoller poller;

        /// <summary>
        /// Renders the transition when new commits were detected.
        /// This method is used as a delegate for <see cref="GitPoller.OnChangeDetected"/>.
        /// </summary>
        private void Render()
        {
            RenderAsync().Forget();
        }

        /// <summary>
        /// Renders the transition when new commits were detected.
        /// This method implements the actual rendering.
        /// </summary>
        /// <returns>task</returns>
        private async UniTask RenderAsync()
        {
            ShowNewCommitsMessage();

            // Backup old graph
            Graph oldGraph = branchCity.LoadedGraph.Clone() as Graph;
            await branchCity.LoadDataAsync();
            try
            {
                branchCity.ReDrawGraph();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            branchCity.LoadedGraph.Diff(oldGraph,
                g => g.Nodes(),
                (g, id) => g.GetNode(id),
                GraphExtensions.AttributeDiff(branchCity.LoadedGraph, oldGraph),
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

        /// <summary>
        /// Allows the comparison of two instances of <see cref="Node"/> from different graphs.
        /// </summary>
        private static readonly NodeEqualityComparer nodeEqualityComparer = new();

        /// <summary>
        /// Notifies the user about removed files in the repository.
        /// </summary>
        /// <param name="removedNodes">The list of removed nodes.</param>
        private void ShowRemovedNodes(ISet<Node> removedNodes)
        {
            foreach (Node removedNode in removedNodes)
            {
                ShowNotification.Info("File removed", $"File {removedNode.ID} was removed from the repository.");
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

        /// <summary>
        /// The code city where the <see cref="GitBranchesGraphProvider"/> graph provider
        /// was executed and which should be updated when a new commit is detected.
        /// </summary>
        private readonly BranchCity branchCity;

        /// <summary>
        /// MarkerFactory for generating node markers.
        /// </summary>
        private readonly MarkerFactory markerFactory;

        /// <summary>
        /// Shows a message to the user that a new commit was detected.
        /// </summary>
        private void ShowNewCommitsMessage()
        {
            ShowNotification.Info("New commits detected", "Refreshing code city.");
        }
    }
}
