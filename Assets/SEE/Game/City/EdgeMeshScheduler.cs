using System;
using Sirenix.OdinInspector;
using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using SEE.DataModel;
using SEE.DataModel.DG;
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
        [SerializeField, Min(1)]
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
        /// Initialize this component with given settings.
        ///
        /// Precondition: The given parameters are not null.
        /// </summary>
        /// <param name="layoutSettings">Layout settings</param>
        /// <param name="selectionSettings">Selection settings</param>
        /// <param name="graph">Graph on which to listen for new edges</param>
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
        }

        /// <summary>
        /// Returns the corresponding GameObject for the given <paramref name="edge"/>.
        /// If <paramref name="edge"/> already has a mesh (i.e., a
        /// <see cref="MeshFilter"/> is attached to it), null is returned.
        /// Likewise, if <paramref name="edge"/> is not associated with any
        /// game object, null is returned.
        /// </summary>
        /// <param name="edge">Edge to be registered</param>
        /// <returns>Corresponding GameObject or null if edge can be ignored</returns>
        private static GameObject GetGameEdge(Edge edge)
        {
            GameObject gameEdge = GraphElementIDMap.Find(edge.ID);
            if (gameEdge == null)
            {
                Debug.LogWarning($"No GameObject for Spline {edge.ToShortString()}. Ignoring.\n");
            }
            else if (!gameEdge.TryGetComponent(out MeshFilter _))
            {
                return gameEdge;
            }

            return null;
        }

        /// <summary>
        /// Processes the next (up to) <see cref="EdgesPerFrame"/> edges.
        private void LateUpdate()
        {
            // We will loop until either we converted `EdgesPerFrame` many edges,
            // or until there are no further edges to convert to meshes.
            int remaining = Mathf.Min(edges.Count, EdgesPerFrame);
            for (int i = 0; i < remaining; i++)
            {
                Edge edge = edges.Dequeue();
                GameObject gameEdge = GetGameEdge(edge);
                if (gameEdge == null)
                {
                    // Edge doesn't exist or is already a mesh. See `GetGameEdge`.
                    continue;
                }

                // fail-safe
                if (!gameEdge.TryGetComponent(out SEESpline spline))
                {
                    Debug.LogWarning("Game object without SEESpline component. Ignoring.\n");
                    continue;
                }

                // fail-safe
                if (layout == null)
                {
                    Debug.LogWarning("Layout settings are missing. Falling back to defaults.\n");
                }
                else
                {
                    spline.Radius = layout.EdgeWidth / 2;
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
                }

                spline.CreateMesh();
            }
        }

        public void OnCompleted()
        {
            Destroyer.Destroy(this);
        }

        public void OnError(Exception error)
        {
            // We don't care. Someone else should handle the error.
        }

        public void OnNext(ChangeEvent value)
        {
            if (value is EdgeEvent { Change: ChangeType.Addition } edgeEvent)
            {
                // If this is an added edge, we are going to need to turn it into a mesh.
                edges.Enqueue(edgeEvent.Edge);
            }
            
            // We don't care about other event types.
        }
    }
}
