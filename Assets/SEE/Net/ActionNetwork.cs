using Cysharp.Threading.Tasks;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.Drawable;
using SEE.GameObjects;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Net.Actions.City;
using SEE.Net.Actions.Table;
using SEE.Net.Util;
using SEE.User;
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
        /// Indicator of whether synchronization was blocked to wait for a response from the client.
        /// Only needed for late joining.
        /// </summary>
        private bool blockedForSynchronization = false;

        /// <summary>
        /// Whether the a client has fetched the code cities from the server
        /// or a server has initialized
        /// </summary>
        private bool networkIsSetUp = false;

        private void Start()
        {
            FetchCities();
        }

        /// <summary>
        /// Fetches the multiplayer city files from the backend on the server or host.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            FetchCities();
        }

        /// <summary>
        /// Fetches the multiplayer city files from the backend on the server or host.
        /// </summary>
        private void FetchCities()
        {
            if (!networkIsSetUp)
            {
                if (IsServer || IsHost)
                {
                    // We are a server.
                    Debug.Log("Starting server action network!\n");
                    BackendSyncUtil.InitializeCitiesAsync().Forget();
                    networkIsSetUp = true;
                }
                if (IsClient)
                {
                    // We are a client.
                    Debug.Log("Starting client action network!\n");
                    RequestSynchronizationServerRpc();
                    networkIsSetUp = true;
                }
            }
        }


        /// <summary>
        /// Sends an action to all clients in the recipients list, or to all connected clients
        /// (except the sender) if <c>recipients</c> is <c>null</c>.
        /// </summary>
        /// <param name="serializedAction">The serialized action to be broadcasted.</param>
        /// <param name="recipientIds">The list of recipients of the action; if null, all
        /// connected clients will be notified.</param>
        /// <param name="rpcParams">The additional RPC parameters.</param>
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

            if (recipientIds == null)
            {
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
        /// <param name="data">The data of the fragment.</param>
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
                    using NativeArray<ulong> targetClientIds = new(recipientIds, Allocator.Temp);
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
        /// <param name="rpcParams">The additional RPC parameters.</param>
        [Rpc(SendTo.Server)]
        public void RequestSynchronizationServerRpc(RpcParams rpcParams = default)
        {
            if (!IsServer && !IsHost)
            {
                return;
            }

            ulong senderId = rpcParams.Receive.SenderClientId;
            SyncFilesClientRpc(Network.ServerId, UserSettings.Instance.Network.BackendServerAPI, RpcTarget.Single(senderId, RpcTargetUse.Temp));
        }

        /// <summary>
        /// Syncs the current state of the server with the connecting client.
        /// </summary>
        /// <param name="clientId">The ID of the receiving client.</param>
        [Rpc(SendTo.Server)]
        internal void SyncClientServerRpc(ulong clientId)
        {
            SyncActionsAsync(clientId).Forget();

            async UniTask SyncActionsAsync(ulong clientId)
            {
                foreach (string serializedAction in Network.NetworkActionList.ToList())
                {
                    AbstractNetAction action = ActionSerializer.Deserialize(serializedAction);
                    if (action is AddCityNetAction || action is SpawnTableNetAction)
                    {
                        blockedForSynchronization = true;
                        ExecuteCityOrTableCreationUnsafeWithResponseClientRpc
                            (serializedAction, RpcTarget.Single(clientId, RpcTargetUse.Temp));
                        await UniTask.WaitUntil(() => !blockedForSynchronization);
                    }
                    else
                    {
                        ExecuteActionUnsafeClientRpc(serializedAction, RpcTarget.Single(clientId, RpcTargetUse.Temp));
                    }
                }
                DrawableSynchronizer.Synchronize(clientId);
            }
        }

        /// <summary>
        /// Releases the synchronization lock on the server side.
        /// </summary>
        /// <param name="rpcParams">The additional RPC parameters (not actually used).</param>
        [Rpc(SendTo.Server)]
        private void ClientResponseActionExecutionToServerRpc(RpcParams rpcParams = default)
        {
            if (!IsServer && !IsHost)
            {
                return;
            }
            blockedForSynchronization = false;
        }

        /// <summary>
        /// Determines whether the unsafe action execution should be skipped.
        /// This is the case if the executor is the host or server,
        /// or if the sender is not the server.
        /// </summary>
        /// <param name="rpcParams">The RPC parameters.</param>
        /// <returns>True if the execution should be skipped; otherwise, false.</returns>
        private bool ShouldSkipUnsafeRpcExecution(RpcParams rpcParams)
        {
            if (IsHost || IsServer)
            {
                return true;
            }

            if (rpcParams.Receive.SenderClientId != ServerClientId)
            {
                Debug.LogWarning($"Received an ExecuteActionUnsafeClientRpc from client ID {rpcParams.Receive.SenderClientId}!\n");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Performs an unsafe creation of either a code city or table
        /// on the client side and then sends a response to the server.
        /// If <paramref name="cityCreation"/> is true, a city is assumed
        /// to be created; otherwise, a table is assumed to be created.
        ///
        /// Precondition: The action must be an <see cref="AddCityNetAction"/>
        /// or a <see cref="SpawnTableNetAction"/>.
        /// </summary>
        /// <param name="serializedAction">The serialized action to be broadcasted.</param>
        /// <param name="rpcParams">The additional RPC parameters.</param>
        [Rpc(SendTo.NotServer, AllowTargetOverride = true)]
        private void ExecuteCityOrTableCreationUnsafeWithResponseClientRpc
            (string serializedAction,
            RpcParams rpcParams = default)
        {
            if (ShouldSkipUnsafeRpcExecution(rpcParams))
            {
                return;
            }

            AbstractNetAction action = ActionSerializer.Deserialize(serializedAction);

            if (action is AddCityNetAction)
            {
                ExecuteAndWaitForCityCreation().Forget();
            }
            else if (action is SpawnTableNetAction)
            {
                ExecuteAndWaitForTableCreation().Forget();
            }
            else
            {
                throw new System.Exception($"The action must be an {nameof(AddCityNetAction)} or a {nameof(SpawnTableNetAction)}.");
            }

            return;

            async UniTask ExecuteAndWaitForCityCreation()
            {
                AddCityNetAction cityAction = (AddCityNetAction)action;
                cityAction.ExecuteOnClient();
                if (LocalPlayer.TryGetCitiesHolder(out CitiesHolder citiesHolder))
                {
                    GameObject city = citiesHolder.Find(cityAction.TableID);
                    await UniTask.WaitUntil(() => city.GetComponent<AbstractSEECity>() != null && city.IsCodeCityDrawnAndActive());
                    ClientResponseActionExecutionToServerRpc();
                }
                else
                {
                    // This case should not actually occur, but it serves as a backup.
                    await UniTask.Delay(2000);
                    ClientResponseActionExecutionToServerRpc();
                }
            }

            async UniTask ExecuteAndWaitForTableCreation()
            {
                SpawnTableNetAction spawnAction = (SpawnTableNetAction)action;
                spawnAction.ExecuteOnClient();
                await UniTask.Yield();
                ClientResponseActionExecutionToServerRpc();
            }
        }

        /// <summary>
        /// Executes an action, even if the sender and this client are the same. This is used
        /// for synchronizing server state.
        /// </summary>
        /// <param name="serializedAction">The serialized action to be broadcasted.</param>
        /// <param name="rpcParams">The additional RPC parameters.</param>
        [Rpc(SendTo.NotServer, AllowTargetOverride = true)]
        private void ExecuteActionUnsafeClientRpc(string serializedAction, RpcParams rpcParams = default)
        {
            if (ShouldSkipUnsafeRpcExecution(rpcParams))
            {
                return;
            }

            AbstractNetAction action = ActionSerializer.Deserialize(serializedAction);
            action.ExecuteOnClient();
        }

        /// <summary>
        /// Executes an action on the client.
        /// </summary>
        /// <param name="serializedAction">The serialized action to be broadcasted.</param>
        /// <param name="rpcParams">The additional RPC parameters.</param>
        [Rpc(SendTo.NotServer, AllowTargetOverride = true)]
        private void ExecuteActionClientRpc(string serializedAction, RpcParams rpcParams = default)
        {
            if (ShouldSkipUnsafeRpcExecution(rpcParams))
            {
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
        /// <param name="data">The data of the fragment.</param>
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
        /// <param name="backendServerId">The network id of the backend server.</param>
        /// <param name="backendDomain">The domain of the backend .</param>
        /// <param name="rpcParams">The additional RPC parameters.</param>
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
            UserSettings.Instance.Network.BackendServerAPI = backendDomain;

            BackendSyncUtil.InitializeClientAsync().Forget();
        }
    }
}
