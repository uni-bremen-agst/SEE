using System.Collections.Generic;
using UnityEngine;

namespace SEE.DataModel
{
    public interface ISceneGraph : IGraph
    {
        /// <summary>
        /// Returns the game object representing the graph in the scene.
        /// </summary>
        /// <returns>game object representing the graph in the scene</returns>
        GameObject GetGraph();

        /// <summary>
        /// Returns all game objects representing the nodes of the graph in the scene.
        /// </summary>
        /// <returns>all node game objects</returns>
        List<GameObject> GetNodes();

        /// <summary>
        /// Destroys the GameObjects of the graph's nodes and edges including the
        /// associated INode and IEdge components as well as the GameObject of the graph 
        /// itself (and its IGraph component). The graph is unusable afterward.
        /// </summary>
        void Destroy();

        /// <summary>
        /// Returns all game objects representing the edges of the graph in the scene.
        /// </summary>
        /// <returns>all edge game objects</returns>
        List<GameObject> GetEdges();
    }
}
