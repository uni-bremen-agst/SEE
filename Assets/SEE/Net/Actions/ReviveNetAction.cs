using SEE.Game;
using SEE.Game.City;
using SEE.Game.SceneManipulation;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// This class revives nodes and/or edges previously deleted at all clients
    /// in the network, that is, it revives game objects representing a node
    /// or edge previously marked as deleted (but not actually deleted).
    ///
    /// Precondition: The objects to be revived were in fact previously
    /// marked as deleted.
    /// </summary>
    /// <remarks>If the objects to be revived were actually deleted, use
    /// <see cref="RestoreNetAction"/> instead.</remarks>
    public class ReviveNetAction : RegenerateNetAction
    {
        // Note: All attributes are made public so that they will be serialized
        // for the network transfer.

        /// <summary>
        /// A serialization of the unique names of the gameObjects representing either
        /// a node or an edge that are to be revived.
        /// The type is assumed to be a list of strings, where each string is a unique name
        /// identifying a game object representing either a node or edge in the scene.
        /// </summary>
        public string GraphElementIDs;

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
            : base(nodeTypes)
        {
            GraphElementIDs = StringListSerializer.Serialize(gameObjectIDs);
        }

        /// <summary>
        /// Revives the game object identified by <see cref="GraphElementIDs"/> on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameElementDeleter.Revive(SceneQueries.Find
                                          (new HashSet<string>(StringListSerializer.Unserialize(GraphElementIDs))),
                                      ToMap());
        }
    }
}
