using OdinSerializer;
using SEE.Game.City;
using SEE.GO;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// Although the creation of a <see cref="Mesh"/> for a single edge is
    /// fast enough for 60 fps, a larger number of edges (several dozens) can
    /// lead to massive performance issues (see <see cref="SEESpline"/> for
    /// more details on spline meshes). It is therefore necessary to
    /// coordinate the creation of the meshes centrally. This class serves as
    /// some sort of "scheduler" (or "driver") that takes on this task. It
    /// stores a queue of edges whose mesh needs to be created. Each frame, a
    /// fixed number of edges (<see cref="EdgesPerFrame"/>) is taken from the
    /// queue and processed (using <see cref="SEESpline.CreateMesh"/>; note
    /// that this also removes the <see cref="LineRenderer"/> of the edge).
    /// New edges can be registered from anywhere at any time via
    /// <see cref="Add(GameObject)"/>.
    ///
    /// Note: This component needs to be initialized via
    /// <see cref="Init(EdgeLayoutAttributes, EdgeSelectionAttributes)"/>.
    /// </summary>
    public class EdgeMeshScheduler : SerializedMonoBehaviour
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
        private Queue<GameObject> edges = new Queue<GameObject>();

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
        public void Init(
            EdgeLayoutAttributes layoutSettings,
            EdgeSelectionAttributes selectionSettings)
        {
            layout = Assertions.AssertNotNull(layoutSettings, "layoutSettings");
            selection = Assertions.AssertNotNull(selectionSettings, "selectionSettings");
        }

        /// <summary>
        /// Registers the given edge object for mesh creation. If
        /// <paramref name="edge"/> already has a mesh (i.e., a
        /// <see cref="MeshFilter"/> is attached to it), it is ignored.
        /// </summary>
        /// <param name="edge">Edge to be registered</param>
        public void Add(GameObject edge)
        {
            if (edge == null) return;
            if (!edge.TryGetComponent<MeshFilter>(out var _))
            {
                edges.Enqueue(edge);
            }
        }

        /// <summary>
        /// Called once per frame. Starts <see cref="CreateMeshes"/> as
        /// coroutine (<see cref="MonoBehaviour.StartCoroutine(IEnumerator)"/>).
        /// </summary>
        private void Update()
        {
            StartCoroutine(CreateMeshes());
        }

        /// <summary>
        /// Processes the next (up to) <see cref="EdgesPerFrame"/> edges. This
        /// method is started as coroutine by <see cref="Update"/>. For more
        /// details on coroutines, please have a look at:
        ///
        ///     https://docs.unity3d.com/Manual/Coroutines.html
        /// </summary>
        /// <returns>enumerate as to whether to continue</returns>
        private IEnumerator CreateMeshes()
        {
            for (int i = 0; i < EdgesPerFrame; i++)
            {
                if (edges.Count == 0) break;
                GameObject edge = edges.Dequeue();

                // fail-safe
                if (!edge.TryGetComponent<SEESpline>(out var spline))
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
                yield return null;
            }
        }
    }
}