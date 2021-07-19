using System;
using System.Collections.Generic;
using System.Linq;
using SEE.DataModel.DG;
using SEE.GO;
using UnityEngine;

namespace SEE.Game
{
    /// <summary>
    /// Class providing methods to bulk update graph elements, e.g updating edge points when moving nodes.
    /// </summary>
    public static class GameElementUpdater
    {
        
        /// <summary>
        /// Updates the rendered node style depending on the new graph depth.
        /// </summary>
        public static void UpdateNodeStyles()
        {
            SEECityArchitecture city = SceneQueries.FindArchitectureCity();
            city.Renderer.RefreshNodeStyle(city.gameObject,SceneQueries.AllGameNodesInScene(true, true));
        }

        /// <summary>
        /// Updates all edges that are connected to the <paramref name="gameObject"/> or one of its descendants.
        /// If <paramref name="recalculateMesh"/> is true, the mesh for edge selection is recalculated.
        /// Do Not recalculate the mesh each frame as it has a heavy performance impact.
        /// </summary>
        /// <param name="gameObject">The origin node.</param>
        /// <param name="recalculateMesh">Whether to recalculate the mesh for selection.</param>
        public static void UpdateEdgePoints(GameObject gameObject, bool recalculateMesh = false)
        {
            // Find the City 
            SEECityArchitecture city = SceneQueries.FindArchitectureCity();
            if (gameObject.TryGetNode(out Node node))
            {
                List<EdgeRef> edges = SceneQueries.FindAllConnectingEdges(node);
                UpdateEdgePoints(gameObject, node, edges, city.EdgeLayoutSettings.isEdgeSelectable, recalculateMesh);
            }
            else
            {
                throw new Exception($"The game node {gameObject.name} has no valid graph element attached.\n");
            }

        }
        
        private static void UpdateEdgePoints(GameObject gameObject, Node node, List<EdgeRef> gameEdges, bool edgeSelectable, 
            bool recalculateMesh)
        {
            if (gameEdges.Count < 1)
            {
                return;
            }

            foreach (EdgeRef edgeRef in gameEdges)
            {
                GameObject gameEdge = edgeRef.gameObject;
                Vector3 newPoint = gameEdge.transform.InverseTransformPoint(gameObject.transform.position);
                if (edgeRef.Value.Source.ID == node.ID)
                {
                    //Handle outgoing
                    gameEdge.GetComponent<LineRenderer>().SetPosition(0, newPoint);
                    gameEdge.GetComponent<Points>().linePoints[0] = newPoint;
                }
                else if (edgeRef.Value.Target.ID == node.ID)
                {
                    //Handle incoming edge
                    gameEdge.GetComponent<LineRenderer>().SetPosition(1, newPoint);
                    gameEdge.GetComponent<Points>().linePoints[1] = newPoint;
                }

                if (edgeSelectable && recalculateMesh)
                {
                    MeshCollider collider = gameEdge.GetComponent<MeshCollider>();
                    MeshFilter filter = gameEdge.GetComponent<MeshFilter>();

                    Mesh mesh = RecalculateMesh(gameEdge.GetComponent<Points>().linePoints.ToList());
                    collider.sharedMesh = mesh;
                    filter.sharedMesh = mesh;
                }
            }
            // Update the edges recursively for all childs.
            foreach (Transform child in gameObject.transform)
            {
                if (child.TryGetComponent(out NodeRef nodeRef))
                {
                    UpdateEdgePoints(child.gameObject, nodeRef.Value, gameEdges, edgeSelectable, recalculateMesh);
                }
            }
                
            
        }
        
        
        /// <summary>
        /// Recalculates the mesh for the collider mesh. Avoid using this in Update(), due to its performance impact.
        /// </summary>
        /// <param name="points">The world points for this edge.</param>
        /// <returns>The recalculated mesh</returns>
        private static Mesh RecalculateMesh(List<Vector3> points)
        {
            return Tubular.Tubular.Build(new Curve.CatmullRomCurve(points), 50, 0.005f, 8, false);
        }

        
    }
}