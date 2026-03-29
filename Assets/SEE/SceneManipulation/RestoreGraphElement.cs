using System;
using UnityEngine;

namespace SEE.Game.SceneManipulation
{
    /// <summary>
    /// Abstract base class representing a helper for restoring
    /// serialized graph elements (either nodes or edges)
    /// in the scene.
    /// </summary>
    [Serializable]
    public abstract class RestoreGraphElement
    {
        /// <summary>
        /// The unique identifier of the graph element.
        /// </summary>
        public string ID;
    }

    /// <summary>
    /// Serializable representation of a node in the graph,
    /// used to restore it with its properties.
    /// </summary>
    [Serializable]
    public class RestoreNodeElement : RestoreGraphElement
    {
        /// <summary>
        /// ID of the parent node.
        /// </summary>
        public string ParentID;

        /// <summary>
        /// The position of the node.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The scale of the node.
        /// </summary>
        public Vector3 Scale;

        /// <summary>
        /// The corresponding node type.
        /// </summary>
        public string NodeType;

        /// <summary>
        /// Source name of the node.
        /// </summary>
        public string Name;

        /// <summary>
        /// The node level.
        /// </summary>
        public int Level;

        /// <summary>
        /// Creates a new instance of a node element for restoration.
        /// </summary>
        /// <param name="parentID">ID of the parent node.</param>
        /// <param name="nodeID">Unique identifier of the node.</param>
        /// <param name="pos">Position of the node.</param>
        /// <param name="scale">Scale of the node.</param>
        /// <param name="nodeType">Type of the node.</param>
        /// <param name="sourceName">Name of the node.</param>
        /// <param name="level">Level of the node.</param>
        public RestoreNodeElement
            (string parentID,
             string nodeID,
             Vector3 pos,
             Vector3 scale,
             string nodeType,
             string sourceName,
             int level)
        {
            ParentID = parentID;
            ID = nodeID;
            Position = pos;
            Scale = scale;
            NodeType = nodeType;
            Name = sourceName;
            Level = level;
        }
    }

    /// <summary>
    /// Serializable representation of an edge.
    /// </summary>
    [Serializable]
    public class RestoreEdgeElement : RestoreGraphElement
    {
        /// <summary>
        /// ID of the source node.
        /// </summary>
        public string FromID;

        /// <summary>
        /// ID of the target node.
        /// </summary>
        public string ToID;

        /// <summary>
        /// Type of the edge.
        /// </summary>
        public string EdgeType;

        /// <summary>
        /// Creates a new instance of an edge element for restoration.
        /// </summary>
        /// <param name="id">Unique identifier of the edge.</param>
        /// <param name="fromID">ID of the source node.</param>
        /// <param name="toID">ID of the target node.</param>
        /// <param name="edgeType">Type of the edge.</param>
        public RestoreEdgeElement(string id, string fromID, string toID, string edgeType)
        {
            ID = id;
            FromID = fromID;
            ToID = toID;
            EdgeType = edgeType;
        }
    }
}
