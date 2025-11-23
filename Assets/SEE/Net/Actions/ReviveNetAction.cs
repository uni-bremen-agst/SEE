using SEE.Game;
using SEE.Game.City;
using SEE.Game.SceneManipulation;
using SEE.Utils;
using System;
using System.Collections.Generic;

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
        public ReviveNetAction(List<string> gameObjectIDs, Dictionary<string, VisualNodeAttributes> nodeTypes, Action undoAction)
            : base(nodeTypes, undoAction)
        {
            GraphElementIDs = StringListSerializer.Serialize(gameObjectIDs);
        }

        /// <summary>
        /// This is used to update the local versioning in an intermediate step.
        /// </summary>
        /// <param name="recipients"></param>
        public new void Execute(ulong[] recipients = null)
        {
            base.Execute(recipients);
            SetVersions(true, out _);
        }

        /// <summary>
        /// Only updates the versioning on the server.
        /// </summary>
        public override void ExecuteOnServer()
        {
            SetVersions(true, out _);
        }

        /// <summary>
        /// Revives the game object identified by <see cref="GraphElementIDs"/> on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            // First we need to restore the versioning.
            SetVersions(true, out List<string> graphElements);

            GameElementDeleter.Revive(SceneQueries.Find(new HashSet<string>(graphElements)),
                                      ToMap());
        }

        /// <summary>
        /// Undoes the ReviveAction locally if the server rejects it.
        /// </summary>
        public override void Undo()
        {
            UndoAction.Invoke();
            // First we need to restore the versioning.
            SetVersions(false, out _);
            RollbackNotification();
        }

        /// <summary>
        /// Used to customize versioning locally.
        /// </summary>
        /// <param name="isRevived"><c>True</c> sets versions to 1<br></br>
        /// <c>False</c> sets versions to -1</param>
        public void SetVersions(bool isRevived, out List<string> graphElements)
        {
            int version = isRevived ? 1 : -1;
            graphElements = StringListSerializer.Unserialize(GraphElementIDs);
            foreach (string id in graphElements)
            {
                Network.ActionNetworkInst.Value.SetObjectVersion(id, version);
            }
        }

        /// <summary>
        /// Creates a list for the concurrency check.
        /// </summary>
        /// <returns>List of GameObject-IDs.</returns>
        public override List<string> GetRegenerateList()
        {
            return StringListSerializer.Unserialize(GraphElementIDs);
        }
    }
}