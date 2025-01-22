using OpenCVForUnity.FaceModule;
using SEE.Game.City;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.SEE.Utils
{
    /// <summary>
    /// A serializer for maps of node types.
    /// </summary>
    public class NodeTypesSerializer
    {
        /// <summary>
        /// We are wrapping the serialized map of node types into this
        /// class so that it can be serialized.
        /// </summary>
        [Serializable]
        private class Wrapper
        {
            /// <summary>
            /// The list of node types.
            /// </summary>
            public List<NodeType> NodeTypes = new();
        }

        /// <summary>
        /// A serializable class for node types, which contains both the <see cref="VisualNodeAttributes"/>
        /// and the name of the node type.
        /// </summary>
        [Serializable]
        private class NodeType
        {
            public string Name;
            public VisualNodeAttributes Attributes;
        }

        /// <summary>
        /// Serializes the given <paramref name="nodeTypes"/> as a string.
        /// Do not make any assumption about the kind of serialization.
        /// Instead always use <see cref="Unserialize(string)"/> to retrieve
        /// the original string list.
        ///
        /// Precondition: Neither <paramref name="nodeTypes"/> nor any of its
        /// elements is null.
        /// </summary>
        /// <param name="nodeTypes">map to be serialized</param>
        /// <returns>serialization of <paramref name="nodeTypes"/></returns>
        /// <exception cref="ArgumentNullException">thrown if <paramref name="nodeTypes"/>
        /// is null or have no elements.</exception>
        public static string Serialize(Dictionary<string, VisualNodeAttributes> nodeTypes)
        {
            if (nodeTypes == null || nodeTypes.Count == 0)
            {
                throw new System.ArgumentNullException();
            }
            Wrapper wrapper = new();
            foreach (KeyValuePair<string, VisualNodeAttributes> pair in nodeTypes)
            {
                wrapper.NodeTypes.Add(new NodeType { Name = pair.Key, Attributes = pair.Value });
            }
            return JsonUtility.ToJson(wrapper);
        }

        /// <summary>
        /// Unserializes the given <paramref name="serializedMap"/> back to
        /// a map of node types.
        ///
        /// Assumption: <paramref name="serializedMap"/> is the result of
        /// <see cref="Serialize"/>.
        ///
        /// Postcondition: Unserialize(Serialize(X)) is equal to X for every X
        /// where X is not null and none of its elements is null.
        /// </summary>
        /// <param name="serializedMap">list of strings to be unserialized</param>
        /// <returns>original dictionary of node types that was serialized by <see cref="Serialize"/>.</returns>
        public static Dictionary<string, VisualNodeAttributes> Unserialize(string serializedMap)
        {
            Dictionary<string, VisualNodeAttributes> nodeTypes = new();
            foreach(NodeType nodeType in JsonUtility.FromJson<Wrapper>(serializedMap).NodeTypes)
            {
                if (!nodeTypes.ContainsKey(nodeType.Name))
                {
                    nodeTypes.Add(nodeType.Name, nodeType.Attributes);
                }
            }
            return nodeTypes;
        }
    }
}
