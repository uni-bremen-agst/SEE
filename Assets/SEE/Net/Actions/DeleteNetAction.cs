using SEE.Utils;
using SEE.DataModel.DG;
using SEE.Game.SceneManipulation;
using SEE.GO;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// This class propagates a <see cref="DeleteAction"/> to all clients in the network.
    /// </summary>
    public class DeleteNetAction : ConcurrentNetAction
    {
        // Note: All attributes are made public so that they will be serialized
        // for the network transfer.

        /// <summary>
        /// Indicates whether unused node types should be removed.
        /// Only applicable for the clear variant.
        /// The clear variant refers to cleaning an architecture or implementation root node.
        /// Architecture and implementation subroot nodes must not be deleted, as they
        /// cannot be added again at runtime. If the deletion action is applied to one of
        /// these subroot nodes, only their child nodes are removed.
        /// The subroot node itself remains intact.
        /// </summary>
        public bool RemoveNodeTypes;

        /// <summary>
        /// List of elements that need to be deleted.
        /// Used for Object versioning or deletion of many objects.
        /// </summary>
        public string RemovedElementIDs;

        /// <summary>
        /// List of elements that need to be deleted.
        /// Used for Object versioning or deletion of many objects.
        /// </summary>
        public string RemovedElementVersions;

        /// <summary>
        /// Creates a new DeleteNetAction for a single deletion.
        /// </summary>
        /// <param name="gameObjectID">the unique name of the gameObject of a node or edge
        /// that has to be deleted</param>
        public DeleteNetAction(string gameObjectID, Action undoDelete, bool removeNodeTypes = false) : base(gameObjectID)
        {
            UseObjectVersion(gameObjectID);
            NewVersion = -1;                  // Object will be flagged as deleted.
            UsesVersioning = false;
            UndoAction = undoDelete;
            RemoveNodeTypes = removeNodeTypes;

            RemovedElementIDs = StringListSerializer.Serialize(new List<string> { gameObjectID });
        }

        /// <summary>
        /// Creates a new DeleteNetAction for multiple deletions.
        /// </summary>
        /// <param name="gameObjectID">the unique name of the gameObject of a node or edge
        /// that has to be deleted</param>
        public DeleteNetAction(List<string> removedElements, Action undoDelete, bool removeNodeTypes = false) : base("DELETION")
        {
            List<string> objectVersions = new();
            foreach (string element in removedElements)
            {
                objectVersions.Add(Network.ActionNetworkInst.Value.GetObjectVersion(element) + "");
            }
            RemovedElementIDs = StringListSerializer.Serialize(removedElements);
            RemovedElementVersions = StringListSerializer.Serialize(objectVersions);
            UndoAction = undoDelete;
        }

        /// <summary>
        /// Only updates the versioning on the server.
        /// </summary>
        public override void ExecuteOnServer()
        {
            UpdateVersioning();
        }

        /// <summary>
        /// Deletes the game object identified by <see cref="GameObjectID"/> on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameObject objToDelete;
            List<string> removedElements = StringListSerializer.Unserialize(RemovedElementIDs);
            for (int i = 0; i < removedElements.Count; i++)
            {
                objToDelete = Find(removedElements[i]);
                if (objToDelete.TryGetNode(out Node node) && node.IsRoot())
                {
                    GameElementDeleter.DeleteRoot(objToDelete);
                }
                else
                {
#pragma warning disable VSTHRD110
                    GameElementDeleter.Delete(objToDelete, RemoveNodeTypes);
#pragma warning restore VSTHRD110
                }
                Network.ActionNetworkInst.Value.SetObjectVersion(removedElements[i], -1);
            }
        }

        /// <summary>
        /// Undos the DeleteAction locally if the server rejects it.
        /// </summary>
        public override void Undo()
        {
            UndoAction.Invoke();
            RollbackNotification();
        }

        /// <summary>
        /// Only updates the versioning in the ObjectVersion-Dictionary.
        /// </summary>
        public void UpdateVersioning()
        {
            List<string> removedElements = StringListSerializer.Unserialize(RemovedElementIDs);
            for (int i = 0; i < removedElements.Count; i++)
            {
                Network.ActionNetworkInst.Value.SetObjectVersion(removedElements[i], -1);
            }
        }

        /// <summary>
        /// Creates a dictionary of objects and their corresponding version.
        /// </summary>
        /// <returns>A Dictionary of &lt;ID, Version&gt; for deleted objects and<br/>
        /// <c>null</c> otherwise.</returns>
        public Dictionary<string, int> GetVersionedObjects()
        {
            List<string> removedElements = StringListSerializer.Unserialize(RemovedElementIDs);
            List<string> removedVersions = StringListSerializer.Unserialize(RemovedElementVersions);
            if (removedElements.Count == removedVersions.Count)
            {
                Dictionary<string, int> versionDictionary = new();
                for (int i = 0; i < removedElements.Count; i++)
                {
                    versionDictionary.Add(removedElements[i], int.Parse(removedVersions[i]));
                }
                return versionDictionary;
            }
            return null;
        }
    }
}
