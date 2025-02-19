﻿using SEE.Game;
using SEE.Game.City;
using SEE.Game.SceneManipulation;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// This class propagates an undo of <see cref="DeleteAction"/> to all clients
    /// in the network, that is, it revives game objects representing a node
    /// or edge previously marked as deleted.
    ///
    /// Precondition: The objects to be revived were in fact previously deleted.
    /// </summary>
    public class ReviveNetAction : AbstractNetAction
    {
        // Note: All attributes are made public so that they will be serialized
        // for the network transfer.

        /// <summary>
        /// A serialization of the unique names of the gameObjects of a node or edge that
        /// are to be revived.
        /// </summary>
        public string GameObjectIDList;

        /// <summary>
        /// A serialization of the map of the node types to be restored.
        /// </summary>
        public string NodeTypeList;

        /// <summary>
        /// Creates a new <see cref="ReviveNetAction"/>.
        ///
        /// Preconditions:
        /// 1) Neither <paramref name="gameObjectIDs"/> nor any of its elements is null.
        /// 2) The ids in <paramref name="gameObjectIDs"/> exist in the scene, reference a
        ///    game object representing a node or edge, and were previously marked as deleted.
        ///
        /// </summary>
        /// <param name="gameObjectIDs">the list of unique names of the gameObjects representing
        /// a node or edge that have to be revived</param>
        /// <param name="nodeTypes">the map of the node types to be restored.</param>
        /// <exception cref="ArgumentNullException">thrown if <paramref name="gameObjectIDs"/>
        /// or any of its elements is null</exception>
        public ReviveNetAction(List<string> gameObjectIDs, Dictionary<string, VisualNodeAttributes> nodeTypes)
        {
            GameObjectIDList = StringListSerializer.Serialize(gameObjectIDs);
            NodeTypeList = NodeTypesSerializer.Serialize(nodeTypes);
        }

        /// <summary>
        /// Revives the game object identified by <see cref="GameObjectIDList"/> on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            ISet<string> gameObjectIDs = new HashSet<string>(StringListSerializer.Unserialize(GameObjectIDList));
            ISet<GameObject> gameObjects = SceneQueries.Find(gameObjectIDs);
            Dictionary<string, VisualNodeAttributes> nodeTypes = NodeTypesSerializer.Unserialize(NodeTypeList);
            GameElementDeleter.Revive(gameObjects, nodeTypes);
        }
    }
}
