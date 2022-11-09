using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

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
        /// (1) implementation -> architecture: interpreted as an architecture mapping
        /// (2) implementation -> implementation: interpreted as a restructuring in the implementation
        /// (3) architecture -> architecture: interpreted as a restructuring in the architecture
        /// (4) architecture -> implementation: makes no sense; will be ignored
        /// </summary>
        /// <param name="mappingSource">the node to be mapped</param>
        /// <param name="mappingTarget">the target which <paramref name="mappingSource"/> is mapped onto</param>
        /// <returns></returns>
        internal static Edge MapTo(GameObject mappingSource, GameObject mappingTarget)
        {
            Debug.Log($"MapTo({mappingSource.name} -> {mappingTarget.name})\n");

            SEEReflexionCity reflexionCity = mappingSource.ContainingCity<SEEReflexionCity>();
            if (reflexionCity == null)
            {
                throw new Exception($"The mapped node {mappingSource.name} is not contained in a {nameof(SEEReflexionCity)}.");
            }

            Edge mapsToEdge = null;

            // The mapping is only possible if mapping target is actually a node.
            if (mappingTarget.TryGetNode(out Node target))
            {
                // The source of the mapping
                Node source = mappingSource.GetNode();

                if (source.ItsGraph != target.ItsGraph)
                {
                    Debug.LogError("For a mapping, both nodes must be in the same graph.\n");
                    return mapsToEdge;
                }

                // implementation -> architecture
                if (source.IsInImplementation() && target.IsInArchitecture())
                {
                    // If there is a previous mapping that already mapped the node
                    // on the current target, nothing needs to be done.
                    // If there is a previous mapping that mapped the node onto
                    // another target, the previous mapping must be reverted and the
                    // node must be mapped onto the new target.

                    if (mapsToEdge == null)
                    {
                        // If there is no previous mapping, we can just map the node.
                        mapsToEdge = MapTo(reflexionCity, source, target);
                    }
                    else // If there is a previous mapping.
                    {
                        Assert.IsTrue(reflexionCity.LoadedGraph.ContainsEdge(mapsToEdge));
                        // A temporary mapping exists already. This mapping can only be from an implementation
                        // node onto an architecture node.
                        Assert.IsTrue(mapsToEdge.Source == source);
                        // If the mapping hasn't changed, there is nothing to do.
                        if (mapsToEdge.Target != target)
                        {
                            // The grabbed object was previously temporarily mapped onto another target.
                            // The temporary mapping must be reverted.
                            reflexionCity.ReflexionGraph.RemoveFromMapping(mapsToEdge);
                            mapsToEdge = MapTo(reflexionCity, source, target);
                        }
                    }
                }
                // implementation -> implementation
                else if (source.IsInImplementation() && target.IsInImplementation())
                {
                    // This changes the node hierarchy in the implementation only.
                    reflexionCity.ReflexionGraph.UnparentInImplementation(source);
                    reflexionCity.ReflexionGraph.AddChildInImplementation(source, target);
                }
                // architecture -> architecture
                else if (source.IsInArchitecture() && target.IsInArchitecture())
                {
                    // This changes the node hierarchy in the implementation only.
                    reflexionCity.ReflexionGraph.UnparentInArchitecture(source);
                    reflexionCity.ReflexionGraph.AddChildInArchitecture(source, target);
                }
                // architecture -> implementation: forbidden
                else
                {
                    // nothing to be done
                }
            }

            return mapsToEdge;

            /// <summary>
            /// Adds a mapping from <paramref name="source"/> to <paramref name="target"/> to the
            /// reflexion analysis overriding any existing mapping.
            /// </summary>
            /// <param name="source">the source node of the maps-to edge</param>
            /// <param name="target">the target node of the maps-to edge</param>
            /// <returns>the newly added maps-to edge</returns>
            static Edge MapTo(SEEReflexionCity reflexionCity, Node source, Node target)
            {
                // If we are in a reflexion city, we will simply
                // trigger the incremental reflexion analysis here.
                // That way, the relevant code is in one place
                // and edges will be colored on hover (#451).
                return reflexionCity.ReflexionGraph.AddToMapping(source, target, overrideMapping: true);
            }
        }

        /// <summary>
        /// Removes the architecture mapping for <paramref name="gameNode"/>.
        /// </summary>
        /// <param name="gameNode">a game object representing an implementation node that was
        /// explicitly mapped onto an architecture node and whose mapping is to be removed</param>
        /// <exception cref="Exception">thrown if <paramref name="gameNode"/> is not contained
        /// in a <see cref="SEEReflexionCity"/> or if does not represent a node or if has
        /// zero or more than one explicit mappings</exception>
        internal static void Unmap(GameObject gameNode)
        {
            Debug.Log($"Unmap({gameNode.name})\n");
            SEEReflexionCity reflexionCity = gameNode.ContainingCity<SEEReflexionCity>();
            if (reflexionCity == null)
            {
                throw new Exception($"The node {gameNode.name} to be unmapped is not contained in a {nameof(SEEReflexionCity)}.");
            }
            if (gameNode.TryGetNode(out Node node))
            {
                IEnumerable<Edge> mapsToEdges = node.OutgoingsOfType(ReflexionGraph.MapsToType);
                if (mapsToEdges.Count() != 1)
                {
                    throw new Exception($"The node {gameNode.name} has {mapsToEdges.Count()} mapping(s).");
                }
                Edge mapsTo = mapsToEdges.First();
                reflexionCity.ReflexionGraph.RemoveFromMapping(mapsTo);
            }
            else
            {
                throw new Exception($"The object {gameNode.name} does not represent a node.");
            }
        }
    }
}
