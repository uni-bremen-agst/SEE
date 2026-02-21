using System;
using Sirenix.OdinInspector;
using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using MoreLinq.Extensions;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.UI;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// Although the creation of a <see cref="Mesh"/> for a single edge is
    /// fast enough for 60 fps, a larger number of edges (several dozens) can
    /// lead to massive performance issues (see <see cref="SEESpline"/> for
    /// more details on spline meshes). It is therefore necessary to
    /// coordinate the creation of the meshes centrally. This class serves as
    /// some sort of "scheduler" (or "driver") that takes on this task per city.
    /// It stores a queue of edges whose mesh needs to be created. Each frame, a
    /// fixed number of edges (<see cref="EdgesPerFrame"/>) is taken from the
    /// queue and processed (using <see cref="SEESpline.CreateMesh"/>; note
    /// that this also removes the <see cref="LineRenderer"/> of the edge).
    /// New edges are registered automatically whenever a new edge is added
    /// to the graph assigned to this scheduler.
    ///
    /// Note: This component needs to be initialized via
    /// <see cref="Init(EdgeLayoutAttributes, EdgeSelectionAttributes, Graph)"/>.
    /// </summary>
    internal class EdgeMeshScheduler : SerializedMonoBehaviour, IObserver<ChangeEvent>
    {
        /// <summary>
        /// Number of edges to be processed in each frame (i.e., when
        /// <see cref="Update"/> is called). The default value is 5, which
        /// turned out to be a good compromise between load and speed.
        /// </summary>
        [Min(1)]
        public int EdgesPerFrame = 5;

        /// <summary>
        /// Edges to be processed. New edges can be registered with
        /// <see cref="Add(GameObject)"/>.
        /// </summary>
        private readonly Queue<Edge> edges = new();

        /// <summary>
        /// Layout settings.
        /// </summary>
        private EdgeLayoutAttributes layout;

        /// <summary>
        /// Selection settings.
        /// </summary>
        private EdgeSelectionAttributes selection;

        /// <summary>
        /// Indicates whether initial graph edges have all been converted yet.
        /// </summary>
        private bool initialEdgesDone;

        /// <summary>
        /// The text that shall be shown in the loading spinner while the initial edges are being processed.
        /// </summary>
        private string LoadingText => $"Creating edge meshes for {gameObject.name}...";

        /// <summary>
        /// Event that is triggered once all graph edges have been processed for the first time.
        /// </summary>
        public event Action OnInitialEdgesDone;

        /// <summary>
        /// Takes the number of remaining initial edges to be processed and updates the loading spinner accordingly.
        /// </summary>
        private Action<int> UpdateInitialEdgesProgress;

        /// <summary>
        /// Initialize this component with given settings.
        ///
        /// Precondition: The given parameters are not null.
        /// </summary>
        /// <param name="layoutSettings">Layout settings.</param>
        /// <param name="selectionSettings">Selection settings.</param>
        /// <param name="cityGraph">Graph on which to listen for new edges.</param>
        public void Init(
            EdgeLayoutAttributes layoutSettings,
            EdgeSelectionAttributes selectionSettings,
            Graph cityGraph)
        {
            layout = layoutSettings.AssertNotNull("layoutSettings");
            selection = selectionSettings.AssertNotNull("selectionSettings");
            Graph graph = cityGraph.AssertNotNull("City Graph");
            graph.Subscribe(this);

            // When we're initialized, we also convert all existing edges into meshes first.
            graph.Edges().ForEach(edges.Enqueue);
            int totalEdges = edges.Count;
            if (totalEdges > 0)
            {
                using (LoadingSpinner.ShowDeterminate(LoadingText, out Action<float> updateProgress))
                {
                    UpdateInitialEdgesProgress = remaining => updateProgress(1 - remaining / (float)totalEdges);
                }
            }
        }

        /// <summary>
        /// Returns the corresponding GameObject for the given <paramref name="edge"/>.
        /// If <paramref name="edge"/> already has a mesh (i.e., a
        /// <see cref="MeshFilter"/> is attached to it), null is returned.
        /// Likewise, if <paramref name="edge"/> is not associated with any
        /// game object, null is returned.
        /// </summary>
        /// <param name="edge">Edge to be registered.</param>
        /// <returns>Corresponding GameObject or null if edge can be ignored.</returns>
        private static GameObject GetGameEdge(Edge edge)
        {
            GameObject gameEdge = GraphElementIDMap.Find(edge.ID);
            if (gameEdge != null && !gameEdge.TryGetComponent(out MeshFilter _))
            {
                return gameEdge;
            }
            return null;
        }

        /// <summary>
        /// Processes the next (up to) <see cref="EdgesPerFrame"/> edges.
        private void LateUpdate()
        {
            if (!initialEdgesDone && edges.Count == 0)
            {
                // We're done with the initial edges.
                initialEdgesDone = true;
                OnInitialEdgesDone?.Invoke();
                LoadingSpinner.Hide(LoadingText);
                UpdateInitialEdgesProgress = null;
            }
            // We will loop until either we converted `EdgesPerFrame` many edges,
            // or until there are no further edges to convert to meshes.
            int remaining = Mathf.Min(edges.Count, EdgesPerFrame);
            for (int i = 0; i < remaining; i++)
            {
                UpdateInitialEdgesProgress?.Invoke(edges.Count);
                Edge edge = edges.Dequeue();
                if (edge == null)
                {
                    Debug.LogWarning("Edge is null. Ignoring.\n");
                    continue;
                }
                GameObject gameEdge = GetGameEdge(edge);
                if (gameEdge == null)
                {
                    // Edge doesn't exist or is already a mesh. See `GetGameEdge`.
                    continue;
                }

                // fail-safe
                if (!gameEdge.TryGetComponent(out SEESpline spline))
                {
                    Debug.LogWarning($"Game object without {nameof(SEESpline)} component. Ignoring.\n");
                    continue;
                }

                bool hideSplines;
                // fail-safe
                if (layout == null)
                {
                    Debug.LogWarning("Layout settings are missing. Falling back to defaults.\n");
                    hideSplines = false;
                }
                else
                {
                    spline.Radius = layout.EdgeWidth / 4;
                    hideSplines = layout.AnimationKind == EdgeAnimationKind.Buildup;
                }

                // fail-safe
                if (selection == null)
                {
                    Debug.LogWarning("Selection settings are missing. Falling back to defaults.\n");
                }
                else
                {
                    spline.TubularSegments = selection.TubularSegments;
                    spline.RadialSegments = selection.RadialSegments;
                    spline.IsSelectable = selection.AreSelectable && !edge.HasToggle(Edge.IsHiddenToggle);
                }

                spline.CreateMesh();

                if (hideSplines && edge.HasToggle(Edge.IsHiddenToggle))
                {
                    spline.VisibleSegmentEnd = 0;
                }
            }
        }

        /// <summary>
        /// Called by the observed graph when it is being disposed.
        /// As a consequence, this component destroys itself.
        /// </summary>
        public void OnCompleted()
        {
            Destroyer.Destroy(this);
        }

        /// <summary>
        /// Called by the observed graph when an error occurs.
        /// The error is ignored here, as someone else should handle it.
        /// </summary>
        /// <param name="error">Not used.</param>
        public void OnError(Exception error)
        {
            // We don't care. Someone else should handle the error.
        }

        /// <summary>
        /// Called by the observed graph when a change occurs. The change
        /// is described by <paramref name="changeEvent"/>. If it is an
        /// <see cref="EdgeEvent"/> describing an addition of an edge, the
        /// new edge is registered for mesh creation. For all other events,
        /// nothing happens.
        /// </summary>
        /// <param name="changeEvent">A description of the state change; this method
        /// reacts only on an <see cref="EdgeEvent"/>.</param>
        public void OnNext(ChangeEvent changeEvent)
        {
            if (changeEvent is EdgeEvent { Change: ChangeType.Addition } edgeEvent)
            {
                // If this is an added edge, we are going to need to turn it into a mesh.
                // TODO (#722): The loading spinner loops forever for edges newly added as
                // part of the reflexion analysis.
                // LoadingSpinner.ShowIndeterminate(LoadingText);
                edges.Enqueue(edgeEvent.Edge);
            }

            // We don't care about other event types.
        }
    }
}
