using SEE.Game.Avatars;
using SEE.GO;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Toggles pointing of a player's avatar on all remote clients.
    /// </summary>
    public class TogglePointingAction : AbstractNetAction
    {
        /// <summary>
        /// The network object ID of the spawned avatar. Not to be confused
        /// with a network client ID.
        /// </summary>
        public ulong NetworkObjectID;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="networkObjectID">network object ID of the spawned avatar game object</param>
        public TogglePointingAction(ulong networkObjectID)
        {
            NetworkObjectID = networkObjectID;
        }

        /// <summary>
        /// If executed by the initiating client, nothing happens. Otherwise the pointing
        /// mode of the avatar identified by <see cref="NetworkObjectID"/> will be toggled.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                NetworkManager networkManager = NetworkManager.Singleton;
                if (networkManager != null)
                {
                    NetworkSpawnManager networkSpawnManager = networkManager.SpawnManager;
                    if (networkSpawnManager.SpawnedObjects.TryGetValue(NetworkObjectID, out NetworkObject networkObject))
                    {
                        if (networkObject.gameObject.TryGetComponentOrLog(out AvatarAimingSystem aimingSystem))
                        {
                            aimingSystem.TogglePointing();
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

        /// <summary>
        /// Nothing will happen.
        /// </summary>
        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }
    }
}
