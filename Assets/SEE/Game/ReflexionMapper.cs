using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Helper class for mapping game nodes for the reflexion analysis.
    ///
    /// Used by user actions.
    /// </summary>
    static class ReflexionMapper
    {
        /// <summary>
        /// Maps <paramref name="mappingSource"/> onto <paramref name="mappingTarget"/> distinguishing
        /// the following four cases regarding to which domains <paramref name="mappingSource"/>
        /// and <paramref name="mappingTarget"/> belong to:
        /// (1) implementation -> architecture: interpreted as an architecture mapping,
        /// i.e., <paramref name="mappingSource"/> is mapped onto <paramref name="mappingTarget"/>
        /// in the architecture.
        /// (2) implementation -> implementation: interpreted as a restructuring in the implementation
        /// (3) architecture -> architecture: interpreted as a restructuring in the architecture
        /// (4) architecture -> implementation: makes no sense; will be ignored
        ///
        /// In cases (2)-(3), <paramref name="mappingSource"/> becomes a child graph node of
        /// <paramref name="mappingTarget"/> in the underlying graph.
        /// In cases (1)-(3), <paramref name="mappingSource"/> becomes a child game object of
        /// <paramref name="mappingTarget"/>. In all theses cases, the reflexion data is updated.
        /// </summary>
        /// <param name="mappingSource">the node to be mapped</param>
        /// <param name="mappingTarget">the target which <paramref name="mappingSource"/> is mapped onto</param>
        /// <exception cref="Exception">thrown if <paramref name="mappingSource"/>
        /// is not contained in a <see cref="SEEReflexionCity"/> of the graph nodes associated
        /// with <paramref name="mappingSource"/> and <paramref name="mappingTarget"/> are not
        /// contained in the same graph</exception>
        /// <remarks>This method changes only the parentship in the game-object hierarchy
        /// and the graph-node hierarchy and updates the reflexion data. It does not change
        /// any visual attribute of either of the two nodes.</remarks>
        internal static void SetParent(GameObject mappingSource, GameObject mappingTarget)
        {
            SEEReflexionCity reflexionCity = mappingSource.ContainingCity<SEEReflexionCity>();
            if (reflexionCity == null)
            {
                throw new Exception($"The mapped node {mappingSource.name} is not contained in a {nameof(SEEReflexionCity)}.");
            }

            // The mapping is only possible if mapping target is actually a node.
            if (mappingTarget.TryGetNode(out Node target))
            {
                // The source of the mapping
                Node source = mappingSource.GetNode();

                if (source.ItsGraph != target.ItsGraph)
                {
                    throw new Exception("For a mapping, both nodes must be in the same graph.");
                }

                // implementation -> architecture
                if (source.IsInImplementation() && target.IsInArchitecture())
                {
                    // If there is a previous mapping that already mapped the node
                    // on the current target, nothing needs to be done.
                    // If there is a previous mapping that mapped the node onto
                    // another target, the previous mapping must be reverted and the
                    // node must be mapped onto the new target.

                    reflexionCity.ReflexionGraph.AddToMapping(source, target, overrideMapping: true);
                    mappingSource.transform.SetParent(mappingTarget.transform);
                }
                // implementation -> implementation
                else if (source.IsInImplementation() && target.IsInImplementation())
                {
                    // This changes the node hierarchy in the implementation only.
                    reflexionCity.ReflexionGraph.UnparentInImplementation(source);
                    reflexionCity.ReflexionGraph.AddChildInImplementation(source, target);
                    mappingSource.transform.SetParent(mappingTarget.transform);
                }
                // architecture -> architecture
                else if (source.IsInArchitecture() && target.IsInArchitecture())
                {
                    // This changes the node hierarchy in the architecture only.
                    reflexionCity.ReflexionGraph.UnparentInArchitecture(source);
                    reflexionCity.ReflexionGraph.AddChildInArchitecture(source, target);
                    mappingSource.transform.SetParent(mappingTarget.transform);
                }
                // architecture -> implementation: forbidden
                else
                {
                    // Nothing to be done.
                }
            }
        }

        /// <summary>
        /// Returns true if there is an outgoing maps-to edge of
        /// <paramref name="node"/>.  That edge will be set in the out
        /// parameter <paramref name="mapsToEdge"/>. If no such edge
        /// exists, <c>false</c> is returned and <paramref name="mapsToEdge"/>
        /// will be <c>null</c>.
        /// </summary>
        /// <param name="node">node whose outgoing maps-to edge is requested</param>
        /// <param name="mapsToEdge">the outgoing maps-to edge of <paramref name="node"/> or null</param>
        /// <returns>true if and only if <paramref name="node"/> has a single
        /// outgoing maps-to edge</returns>
        /// <exception cref="Exception">thrown in case <paramref name="node"/> has more
        /// than one outgoing maps-to edge</exception>
        private static bool TryGetMapsToEdge(Node node, out Edge mapsToEdge)
        {
            IEnumerable<Edge> mapsToEdges = node.OutgoingsOfType(ReflexionGraph.MapsToType);
            switch (mapsToEdges.Count())
            {
                case 0:
                    mapsToEdge = null;
                    return false;
                case 1:
                    mapsToEdge = mapsToEdges.First();
                    return true;
                default:
                    throw new Exception($"The node {node.ID} has {mapsToEdges.Count()} mapping(s).");
            }
        }
    }
}
