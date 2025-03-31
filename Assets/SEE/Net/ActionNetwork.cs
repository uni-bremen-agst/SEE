using Cysharp.Threading.Tasks;
using SEE.Game.Drawable;
using SEE.Net.Actions;
using SEE.Net.Actions.City;
using SEE.Net.Util;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Net
{
    /// <summary>
    /// Gateway to the server.
    /// </summary>
    public class ActionNetwork : NetworkBehaviour
    {
        /// <summary>
        /// The server id to verify sender for client RPCs.
        /// </summary>
        public static ulong ServerClientId = NetworkManager.ServerClientId;

        /// Collect and preserve the fragments of packages.
        public Dictionary<string, List<Fragment>> fragmentsGatherer = new();

        /// <summary>
        /// Fetches the multiplayer city files from the backend on the server or host.
        /// </summary>
        private void Start()
        {
            if (!IsServer && !IsHost)
            {
                Debug.Log("Starting client action network!\n");
                RequestSynchronizationServerRpc();
                return;
            }
            Debug.Log("Starting server action network!\n");
            BackendSyncUtil.InitializeCitiesAsync().Forget();
        }

        /// <summary>
        /// Sends an action to all clients in the recipients list, or to all connected clients (except the sender) if <c>recipients</c> is <c>null</c>.
        /// </summary>
        [Rpc(SendTo.Server)]
        public void BroadcastActionServerRpc(string serializedAction, ulong[] recipientIds = null, RpcParams rpcParams = default)
        {
            if (!IsServer && !IsHost)
            {
                return;
            }
            if (recipientIds != null && recipientIds.Length == 0)
            {
                return;
            }

            AbstractNetAction deserializedAction = ActionSerializer.Deserialize(serializedAction);
            if (deserializedAction.ShouldBeSentToNewClient)
            {
                Network.NetworkActionList.Add(serializedAction);
            }
            deserializedAction.ExecuteOnServer();

            if (recipientIds == null) {
                ulong senderId = rpcParams.Receive.SenderClientId;
                ExecuteActionClientRpc(serializedAction, RpcTarget.Not(senderId, RpcTargetUse.Temp));
            }
            else
            {
                using NativeArray<ulong> targetClientIds = new(recipientIds, Allocator.Temp);
                ExecuteActionClientRpc(serializedAction, RpcTarget.Group(targetClientIds, RpcTargetUse.Temp));
            }
        }

        /// <summary>
        /// Receives the fragments of a packet and performs the broadcast when all fragments of the packet are present.
        /// </summary>
        /// <param name="id">The packet id.</param>
        /// <param name="packetSize">The size of fragments of the packet.</param>
        /// <param name="currentFragment">The current fragment.</param>
        /// <param name="data">The data of the fragment</param>
        /// <param name="recipients">The recipients of the call.</param>
        /// <param name="rpcParams">Used to identify the sender.</param>
        [Rpc(SendTo.Server)]
        public void BroadcastActionServerRpc(string id, int packetSize, int currentFragment, string data, ulong[] recipients, RpcParams rpcParams = default)
        {
            Fragment fragment = new(id, packetSize, currentFragment, data);
            if (fragmentsGatherer.TryGetValue(fragment.PacketID, out List<Fragment> fragments))
            {
                fragments.Add(fragment);
            }
            else
            {
                List<Fragment> frags = new() { fragment };
                fragmentsGatherer.Add(fragment.PacketID, frags);
            }
            if (fragmentsGatherer.TryGetValue(fragment.PacketID, out List<Fragment> f)
                && Fragment.CombineFragments(f) != "")
            {
                BroadcastFragmentedActionServerRpc(fragment.PacketID, recipients, rpcParams);
            }
        }

        /// <summary>
        /// Performs the broadcast. First, the serialized string is assembled.
        /// </summary>
        /// <param name="key">The packet id.</param>
        /// <param name="recipientIds">The recipients of the call.</param>
        /// <param name="rpcParams">Used to identify the sender.</param>
        [Rpc(SendTo.Server)]
        private void BroadcastFragmentedActionServerRpc(string key, ulong[] recipientIds, RpcParams rpcParams = default)
        {
            if (!IsServer && !IsHost)
            {
                return;
            }
            if (recipientIds != null && recipientIds.Length == 0)
            {
                return;
            }
            if (fragmentsGatherer.TryGetValue(key, out List<Fragment> fragments))
            {
                string serializedAction = Fragment.CombineFragments(fragments);
                AbstractNetAction deserializedAction = ActionSerializer.Deserialize(serializedAction);
                if (deserializedAction.ShouldBeSentToNewClient)
                {
                    Network.NetworkActionList.Add(serializedAction);
                }
                deserializedAction.ExecuteOnServer();

                if (recipientIds == null)
                {
                    ulong senderId = rpcParams.Receive.SenderClientId;
                    foreach (Fragment fragment in fragments)
                    {
                        ReceiveFragmentActionClientRpc(fragment.PacketID, fragment.PacketSize,
                            fragment.CurrentFragment, fragment.Data, RpcTarget.Not(senderId, RpcTargetUse.Temp));
                    }
                }
                else
                {
                    using NativeArray<ulong> targetClientIds = new NativeArray<ulong>(recipientIds, Allocator.Temp);
                    foreach (Fragment fragment in fragments)
                    {
                        ReceiveFragmentActionClientRpc(fragment.PacketID, fragment.PacketSize,
                            fragment.CurrentFragment, fragment.Data, RpcTarget.Group(targetClientIds, RpcTargetUse.Temp));
                    }
                }
                fragmentsGatherer.Remove(key);
            }
        }

        /// <summary>
        /// Requests client synchronization.
        /// This RPC is called by the client to initiate the synchronization process.
        /// </summary>
        [Rpc(SendTo.Server)]
        public void RequestSynchronizationServerRpc(RpcParams rpcParams = default)
        {
            if (!IsServer && !IsHost)
            {
                return;
            }

            ulong senderId = rpcParams.Receive.SenderClientId;
            SyncFilesClientRpc(Network.ServerId, Network.BackendDomain, RpcTarget.Single(senderId, RpcTargetUse.Temp));
        }

        /// <summary>
        /// Syncs the current state of the server with the connecting client.
        /// </summary>
        [Rpc(SendTo.Server)]
        internal void SyncClientServerRpc(ulong clientId)
        {
            StartCoroutine(SyncActionsAndWaitSomeSecondsForCityCreation(clientId));
            return;
            IEnumerator SyncActionsAndWaitSomeSecondsForCityCreation(ulong clientId)
            {
                foreach (string serializedAction in Network.NetworkActionList.ToList())
                {
                    AbstractNetAction action = ActionSerializer.Deserialize(serializedAction);
                    ExecuteActionUnsafeClientRpc(serializedAction, RpcTarget.Single(clientId, RpcTargetUse.Temp));
                    if (action is AddCityNetAction)
                    {
                        yield return new WaitForSeconds(2f);
                    }
                }
                DrawableSynchronizer.Synchronize(clientId);
            }
        }

        /// <summary>
        /// Executes an action, even if the sender and this client are the same. This is used
        /// for synchronizing server state.
        /// </summary>
        [Rpc(SendTo.NotServer, AllowTargetOverride = true)]
        private void ExecuteActionUnsafeClientRpc(string serializedAction, RpcParams rpcParams = default)
        {
            if (IsHost || IsServer)
            {
                return;
            }

            if (rpcParams.Receive.SenderClientId != ServerClientId)
            {
                Debug.LogWarning($"Received a ExecuteActionUnsafeClientRpc from client ID {rpcParams.Receive.SenderClientId}!\n");
                return;
            }

            AbstractNetAction action = ActionSerializer.Deserialize(serializedAction);
            action.ExecuteOnClient();
        }

        /// <summary>
        /// Executes an action on the client.
        /// </summary>
        [Rpc(SendTo.NotServer, AllowTargetOverride = true)]
        private void ExecuteActionClientRpc(string serializedAction, RpcParams rpcParams = default)
        {
            if (IsHost || IsServer)
            {
                return;
            }

            if (rpcParams.Receive.SenderClientId != ServerClientId)
            {
                Debug.LogWarning($"Received a ExecuteActionClientRpc from client ID {rpcParams.Receive.SenderClientId}!\n");
                return;
            }

            AbstractNetAction action = ActionSerializer.Deserialize(serializedAction);
            if (action.Requester != NetworkManager.Singleton.LocalClientId)
            {
                action.ExecuteOnClient();
            }
        }

        /// <summary>
        /// Receives the fragments of a packet and performs the broadcast when all fragments of the packet are present.
        /// </summary>
        /// <param name="id">The packet id.</param>
        /// <param name="packetSize">The size of fragments of the packet.</param>
        /// <param name="currentFragment">The current fragment.</param>
        /// <param name="data">The data of the fragment</param>
        /// <param name="rpcParams">Used to define recipients.</param>
        [Rpc(SendTo.NotServer, AllowTargetOverride = true)]
        public void ReceiveFragmentActionClientRpc(string id, int packetSize, int currentFragment, string data, RpcParams rpcParams = default)
        {
            if (IsHost || IsServer)
            {
                return;
            }
            if (rpcParams.Receive.SenderClientId != ServerClientId)
            {
                Debug.LogWarning($"Received a ExecuteActionClientRpc from client ID {rpcParams.Receive.SenderClientId}!\n");
                return;
            }
            Fragment fragment = new(id, packetSize, currentFragment, data);
            if (fragmentsGatherer.TryGetValue(fragment.PacketID, out List<Fragment> fragments))
            {
                fragments.Add(fragment);
            }
            else
            {
                List<Fragment> frags = new() { fragment };
                fragmentsGatherer.Add(fragment.PacketID, frags);
            }
            if (fragmentsGatherer.TryGetValue(fragment.PacketID, out List<Fragment> f)
                && Fragment.CombineFragments(f) != "")
            {
                ExecuteFragmentAction(fragment.PacketID);
            }
        }

        /// <summary>
        /// Performs the broadcast. First, the serialized string is assembled.
        /// </summary>
        /// <param name="key">The packet id.</param>
        private void ExecuteFragmentAction(string key)
        {
            if (fragmentsGatherer.TryGetValue(key, out List<Fragment> fragments))
            {
                string serializedAction = Fragment.CombineFragments(fragments);
                AbstractNetAction action = ActionSerializer.Deserialize(serializedAction);
                if (action.Requester != NetworkManager.Singleton.LocalClientId)
                {
                    action.ExecuteOnClient();
                }
                fragmentsGatherer.Remove(key);
            }
        }

        /// <summary>
        /// Initiates the synchronization process with the backend and game server.
        /// This RPC is called by the game server after the client has registered itself.
        /// </summary>
        [Rpc(SendTo.SpecifiedInParams)]
        private void SyncFilesClientRpc(string backendServerId, string backendDomain, RpcParams rpcParams = default)
        {
            if (IsHost || IsServer)
            {
                return;
            }

            if (rpcParams.Receive.SenderClientId != ServerClientId)
            {
                Debug.LogWarning($"Received a SyncFilesClientRpc from client ID {rpcParams.Receive.SenderClientId}!\n");
                return;
            }

            Network.ServerId = backendServerId;
            Network.BackendDomain = backendDomain;

            BackendSyncUtil.InitializeClientAsync().Forget();
        }
    }
}
