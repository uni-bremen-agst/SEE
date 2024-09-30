using SEE.Game.Avatars;
using SEE.GO;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Toggles pointing of a player's avatar on all remote clients.
    /// </summary>
    public class TogglePointingNetAction : AbstractNetAction
    {
        /// <summary>
        /// The network object ID of the spawned avatar. Not to be confused
        /// with a network client ID.
        /// </summary>
        public ulong NetworkObjectID;

        /// <summary>
        /// Should not be sent to newly connecting clients
        /// </summary>
        public override bool ShouldBeSentToNewClient { get => false; }

        /// <summary>
        /// Whether pointing should be activated or deactivated.
        /// </summary>
        public bool Activate;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="networkObjectID">network object ID of the spawned avatar game object</param>
        public TogglePointingNetAction(ulong networkObjectID, bool activate)
        {
            NetworkObjectID = networkObjectID;
            Activate = activate;
        }

        /// <summary>
        /// The pointing mode of the avatar identified by <see cref="NetworkObjectID"/> will be toggled.
        /// </summary>
        /// <remarks>This method is not called for the requester.</remarks>
        public override void ExecuteOnClient()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager != null)
            {
                NetworkSpawnManager networkSpawnManager = networkManager.SpawnManager;
                if (networkSpawnManager.SpawnedObjects.TryGetValue(NetworkObjectID, out NetworkObject networkObject))
                {
                    if (networkObject.gameObject.TryGetComponentOrLog(out AvatarAimingSystem aimingSystem))
                    {
                        aimingSystem.SetPointing(Activate, true);
                    }
                }
                else
                {
                    Debug.LogError($"There is no network object with ID {NetworkObjectID}.\n");
                }
            }
            else
            {
                Debug.LogError($"There is no component {typeof(NetworkManager)} in the scene.\n");
            }
        }
    }
}
