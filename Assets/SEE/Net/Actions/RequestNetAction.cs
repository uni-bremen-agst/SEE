using SEE.Game;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Propagates <see cref="Highlighter.SetHighlight"/> through the network.
    /// </summary>
    internal class RequestNetAction : AbstractNetAction
    {
        /// <summary>
        /// Should not be sent to newly connecting clients
        /// </summary>
        public override bool ShouldBeSentToNewClient { get => false; }

        /// <summary>
        /// List of the missing NetActions.
        /// </summary>
        public string MissingActions;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="gameObjectID">the unique game-object name of the child to
        /// be put and fit onto the <paramref name="newParentID"/>;
        /// must be known to <see cref="GraphElementIDMap"/></param>
        /// <param name="highlight">If true, the game object identified by <see cref="GameObjectID"/>
        /// will be highlighted; otherwise its highlighting will be turned off.</param>
        public RequestNetAction(List<int> missingList)
        {
            MissingActions = StringListSerializer.Serialize(missingList.ConvertAll(n => n.ToString()));
        }

        /// <summary>
        /// This message is not meant to be executed on a client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            // this does nothing
        }

        /// <summary>
        /// Initiates the look up of missing actions.
        /// </summary>
        public override void ExecuteOnServer()
        {
            List<string> missingActions = StringListSerializer.Unserialize(MissingActions);
            if (missingActions == null || missingActions.Count == 0)
            {
                // if this happens there must be something terribly wrong
                Debug.LogWarning($"Received an empty RequestMissingActions Message by client ID {Requester}!\n");
                return;
            }

            HashSet<int> missing = new HashSet<int>(
                missingActions.ConvertAll(int.Parse)
            );

            Network.ActionNetworkInst.Value.SendRequestedMissingActionsServer(missing, Requester);
        }

    }
}
