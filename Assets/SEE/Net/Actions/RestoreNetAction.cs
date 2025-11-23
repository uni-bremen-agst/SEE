using SEE.Game.City;
using SEE.Game.SceneManipulation;
using SEE.Utils;
using System;
using System.Collections.Generic;

namespace SEE.Net.Actions
{
    /// <summary>
    /// This class re-creates nodes and/or edges previously deleted at all clients
    /// in the network.
    ///
    /// Precondition: The objects to be re-created were in fact previously deleted.
    /// </summary>
    /// <remarks>If the objects to be re-created were actually only marked as deleted,
    ///  use <see cref="ReviveNetAction"/> instead.</remarks>
    public class RestoreNetAction : RegenerateNetAction
    {
        // Note: All attributes are made public so that they will be serialized
        // for the network transfer.

        /// <summary>
        /// A serialization of the map of the graph elements
        /// and their corresponding layouts, if applicable.
        /// The type is assumed to be a list of <see cref="RestoreGraphElement"/>.
        /// </summary>
        public string NodesOrEdges;

        /// <summary>
        /// The constructor.
        /// It serializes the maps into strings.
        /// </summary>
        /// <param name="nodesOrEdges">The deleted graph elements and their corresponding
        /// layouts, if applicable.</param>
        /// <param name="nodeTypes">The deleted node types.</param>
        public RestoreNetAction
            (List<RestoreGraphElement> nodesOrEdges,
             Dictionary<string, VisualNodeAttributes> nodeTypes,
             Action undoAction)
            : base(nodeTypes, undoAction)
        {
            NodesOrEdges = RestoreGraphElementListSerializer.Serialize(nodesOrEdges);
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
        /// Re-creates the nodes and/or edges at each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            // First we need to restore the versioning.
            SetVersions(true, out List<RestoreGraphElement> elementList);

            GameElementDeleter.Restore(elementList, ToMap());
        }

        /// <summary>
        /// Undoes the RestoreAction locally if the server rejects it.
        /// </summary>
        public override void Undo()
        {
            UndoAction.Invoke();
            // First we need to restore the delete-versioning.
            SetVersions(false, out _);
            RollbackNotification();
        }

        /// <summary>
        /// Used to customize versioning locally.
        /// </summary>
        /// <param name="isRestored"><c>True</c> sets versions to 1<br></br>
        /// <c>False</c> sets versions to -1</param>
        public void SetVersions(bool isRestored, out List<RestoreGraphElement> elementList)
        {
            int version = isRestored ? 1 : -1;
            elementList = RestoreGraphElementListSerializer.Unserialize(NodesOrEdges);
            foreach (RestoreGraphElement elem in elementList)
            {
                Network.ActionNetworkInst.Value.SetObjectVersion(elem.ID, version);
            }
        }

        /// <summary>
        /// Creates a list for the concurrency check.
        /// </summary>
        /// <returns>List of GameObject-IDs.</returns>
        public override List<string> GetRegenerateList()
        {
            return RestoreGraphElementListSerializer
            .Unserialize(NodesOrEdges)
            .ConvertAll(elem => elem.ID);
        }
    }
}