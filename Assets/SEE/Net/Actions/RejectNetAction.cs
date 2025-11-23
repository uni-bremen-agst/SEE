
namespace SEE.Net.Actions
{
    /// <summary>
    /// Is used to reject a clients NetAction and force him to rollback locally.
    /// </summary>
    public class RejectNetAction : AbstractNetAction
    {
        /// <summary>
        /// The new ID of an Attributable Element.
        /// </summary>
        public int? ObjectVersion;

        /// <summary>
        /// The unique name of the gameObject affected by the Action.
        /// </summary>
        public string GameObjectID;

        /// <summary>
        /// The current NetworkVersion at the moment of the NetAction.
        /// </summary>
        public int NetworkVersion;

        /// <summary>
        /// Should not be sent to newly connecting clients
        /// </summary>
        public override bool ShouldBeSentToNewClient { get => false; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="oldV">Version of the Object before the action, may be <c>null</c></param>
        /// <param name="newV">Version of the Object after the action, may be <c>null</c></param>
        /// <param name="netV">Network Version before the Action</param>
        /// <param name="gid">the unique ObjectID</param>
        public RejectNetAction(string gid, int networkVersion, int? objectId, ulong serverId) : base()
        {
            GameObjectID = gid;
            NetworkVersion = networkVersion;
            ObjectVersion = objectId;
            Requester = serverId;
        }

        /// <summary>
        /// Movement in all clients except the requesting client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            Network.ActionNetworkInst.Value.RemoveRejectedAction(this);
        }

        /// <summary>
        /// Does not do anything.
        /// </summary>
        public override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

    }
}
