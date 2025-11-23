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
using System;
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
        /// Indicator of whether synchronization was blocked to wait for a response from the client.
        /// Only needed for late joining.
        /// </summary>
        private bool blockedForSynchronization = false;

        /// <summary>
        /// Stores all <see cref="ConcurrentNetAction"/>s that are to be accepted or rejected by the server.
        /// </summary>
        private List<ConcurrentNetAction> PendingActions;

        /// <summary>
        /// Stores ConcurrentNetActions if necessary.
        /// </summary>
        private Queue<ConcurrentNetAction> IncomingActions;

        /// <summary>
        /// Async coroutine to check for empty PendingActions and perform IncomingActions.
        /// </summary>
        private Coroutine ClientExecuteInOrder;

        /// <summary>
        /// Dictionary to organize the individual object versions of GameElements.
        /// </summary>
        private Dictionary<string, int> ObjectVersion = new();

        /// <summary>
        /// Boolean value for improved readability.
        /// Masks the definition of the baseclass <see cref="NetworkBehaviour"/>.
        /// </summary>
        private new bool IsClient;

        /// <summary>
        ///  ID of the user whose last change was accepted.
        ///  Is used von ConcurrentNetActions.
        /// </summary>
        private ulong LastChangeBy;

        /// <summary>
        /// Time to reset recent rejection mode.
        /// We need realtime and set the cooldown to 2-5x the worst ping.
        /// Approximately 60ms times five. (Unity recommends 200-500ms).
        /// </summary>
        private const float COOLDOWN_TIME = 0.3f;

        /// <summary>
        /// Time to waited for checking whether own NetActions can be executed.
        /// FIXME: THIS SHOULD BE PING TO SERVER x 1.5 (maybe even dynamically)
        /// </summary>
        private const float PING_TO_SERVER = 0.09f;

        /// <summary>
        /// Unity Time since last rejection.
        /// </summary>
        private float LastRejection = 0f;

        /// <summary>
        /// Is used to be cautious in concurrency checking.
        /// </summary>
        private bool RecentRejection
        {
            get => (Time.realtimeSinceStartup - LastRejection < COOLDOWN_TIME);

            set
            {
                if (value)
                {
                    LastRejection = Time.realtimeSinceStartup;
                }
            }
        }

        /// <summary>
        /// Stores the version number of the network.
        /// Managed solely by the server and used to detect conflicts.
        /// </summary>
        private int NetworkVersion = 0;

        /// <summary>
        /// Used to keep track of missing NetActions in versioning.
        /// </summary>
        private HashSet<int> MissingNetworkVersions;

        /// <summary>
        /// Fetches the multiplayer city files from the backend on the server or host.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            IsClient = (!IsServer && !IsHost);
            if (IsClient)
            {
                Debug.Log("Starting client action network!\n");
                RequestSynchronizationServerRpc();
                PendingActions = new();
                IncomingActions = new();
                MissingNetworkVersions = new();
            }
            else
            {
                Debug.Log("Starting server action network!\n");
                BackendSyncUtil.InitializeCitiesAsync().Forget();
            }
        }

        /// <summary>
        /// Sends an action to all clients in the recipients list, or to all connected clients
        /// (except the sender) if <c>recipients</c> is <c>null</c>.
        /// </summary>
        /// <param name="serializedAction">The serialized action to be broadcasted.</param>
        /// <param name="recipientIds">The list of recipients of the action; if null, all
        /// connected clients will be notified</param>
        /// <param name="rpcParams">The additional RPC parameters</param>
        [Rpc(SendTo.Server)]
        public void BroadcastActionServerRpc(string serializedAction, ulong[] recipientIds = null, RpcParams rpcParams = default)
        {
            if (IsClient)
            {
                return;
            }
            if (recipientIds != null && recipientIds.Length == 0)
            {
                return;
            }

            AbstractNetAction deserializedAction = ActionSerializer.Deserialize(serializedAction);
            if (deserializedAction is ConcurrentNetAction conAction)
            {
                if (ConcurrencyCheck(conAction)) { return; }
            }

            if (deserializedAction.ShouldBeSentToNewClient)
            {
                Network.NetworkActionList.Add(serializedAction);
            }

            deserializedAction.ExecuteOnServer();
            if (recipientIds == null)
            {
                // ulong senderId = rpcParams.Receive.SenderClientId;
                // Alle erhalten die NetAction, der Sender nutzt sie als ACK
                ExecuteActionClientRpc(serializedAction, RpcTarget.Everyone);
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
            if (IsClient)
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

                if (deserializedAction is ConcurrentNetAction conAction)
                {
                    if (ConcurrencyCheck(conAction)) { return; }
                }

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
        /// <param name="rpcParams">The additional RPC parameters</param>
        [Rpc(SendTo.Server)]
        public void RequestSynchronizationServerRpc(RpcParams rpcParams = default)
        {
            if (IsClient)
            {
                return;
            }

            ulong senderId = rpcParams.Receive.SenderClientId;
            SyncFilesClientRpc(Network.ServerId, UserSettings.BackendDomain, RpcTarget.Single(senderId, RpcTargetUse.Temp));
        }

        /// <summary>
        /// Syncs the current state of the server with the connecting client.
        /// </summary>
        /// <param name="clientId">The ID of the receiving client</param>
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
        /// <param name="rpcParams">The additional RPC parameters (not actually used)</param>
        [Rpc(SendTo.Server)]
        private void ClientResponseActionExecutionToServerRpc(RpcParams rpcParams = default)
        {
            if (IsClient)
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
        /// <param name="rpcParams">The RPC parameters</param>
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
        /// <param name="rpcParams">The additional RPC parameters</param>
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
        /// <c>SERVER</c><br/>
        /// Tries to send the requesting client the missing actions from the ActionHistory.
        /// Otherwise a ReSync-Message.
        /// </summary>
        /// <param name="missingActions"> List of missing NetActions by the Client.</param>
        public void SendRequestedMissingActionsServer(HashSet<int> missingActions, ulong requestingClient)
        {
            int oldActionCount = Network.NetworkActionList.Count - 1;
            if (oldActionCount <= 0)
            {
                Debug.LogWarning($"Received a RequestMissingActions Message by client ID {requestingClient} while ActionHistory is empty!\n");
                // Send ReSync NetAction to client.
                return;
            }
            int limit = oldActionCount < 7 ? oldActionCount : 7;
            AbstractNetAction tempAction;
            for (int i = oldActionCount; i > oldActionCount - limit; i--)
            {
                tempAction = ActionSerializer.Deserialize(Network.NetworkActionList[i]);
                if (tempAction is ConcurrentNetAction tempConAction &&
                    missingActions.Contains(tempConAction.NetworkVersion))
                {
                    ExecuteActionUnsafeClientRpc(Network.NetworkActionList[i], RpcTarget.Single(requestingClient, RpcTargetUse.Temp));
                }
            }
            if (missingActions.Count > 0)
            {
                // Send ReSync NetAction to client.
            }
        }

        /// <summary>
        /// Executes an action, even if the sender and this client are the same. This is used
        /// for synchronizing server state.
        /// </summary>
        /// <param name="serializedAction">The serialized action to be broadcasted.</param>
        /// <param name="rpcParams">The additional RPC parameters</param>
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
        /// <param name="rpcParams">The additional RPC parameters</param>
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
                ExecuteNetAction(action);
            }
            else
            {
                // Remove from pending list.
                RemovePendingAction((ConcurrentNetAction)action);
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
                    ExecuteNetAction(action);
                }
                else
                {
                    // Remove from pending list.
                    RemovePendingAction((ConcurrentNetAction)action);
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

        /// <summary>
        /// <c>CLIENT</c><br/>
        /// Adds NetAction to the pending list, can be reversed later.
        /// </summary>
        /// <param name="netAction">NetAction to wait for acception/rejection.</param>
        public void AddPendingAction(ConcurrentNetAction netAction)
        {
            PendingActions.Add(netAction);
        }

        /// <summary>
        /// <c>CLIENT</c><br/>
        /// Removes NetAction if it got accepted.
        /// </summary>
        /// <param name="netAction">Accepted NetAction to remove from pending list.</param>
        private void RemovePendingAction(ConcurrentNetAction netAction)
        {
            for (int i = 0; i < PendingActions.Count; i++)
            {
                if (PendingActions[i].Equals(netAction))
                {
                    if (netAction.UsesVersioning)
                    {
                        ObjectVersion[netAction.GameObjectID] = (int)netAction.NewVersion;
                    }
                    if (netAction is DeleteNetAction deleteAction)
                    {
                        deleteAction.UpdateVersioning();
                    }
                    SetNetworkVersion(netAction.NewNetworkVersion);
                    PendingActions.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// <c>CLIENT</c><br/>
        /// Removes NetAction if it got rejected.
        /// </summary>
        /// <param name="netAction">Rejected NetAction to remove from pending list.</param>
        public void RemoveRejectedAction(RejectNetAction netAction)
        {
            for (int i = 0; i < PendingActions.Count; i++)
            {
                if (PendingActions[i].Equals(netAction))
                {
                    PendingActions[i].Undo();
                    PendingActions.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// If there are PendingAction the execution of incoming ConcurrentNetAction
        /// has to wait its turn.
        /// </summary>
        /// <param name="action"></param>
        private void ExecuteNetAction(AbstractNetAction action)
        {
            if (action is ConcurrentNetAction conAction)
            {
                if (conAction.NewNetworkVersion != NetworkVersion + 1 || PendingActions.Count > 0)
                {
                    IncomingActions.Enqueue(conAction);
                    ClientExecuteInOrder ??= StartCoroutine(ExecuteInOrder());
                }
                else
                {
                    conAction.ExecuteOnClient();
                    SetNetworkVersion(conAction.NewNetworkVersion);
                }
            }
            else
            {
                action.ExecuteOnClient();
            }
        }

        /// <summary>
        /// <c>CLIENT</c><br/>
        /// To keep the order of execution we keep new NetActions here
        /// until PendingActions are all executed.
        /// </summary>
        /// <returns><c>Yields</c> for not-blocking exection.</returns>
        private IEnumerator ExecuteInOrder()
        {
            float lastMissingRequest = 0f;
            while (IncomingActions.Count > 0 || MissingNetworkVersions.Count > 0)
            {
                if (IncomingActions.Count > 0)
                {
                    ConcurrentNetAction nextAction = IncomingActions.Peek();

                    if (PendingActions.Count == 0)
                    {
                        IncomingActions.Dequeue().ExecuteOnClient();
                        SetNetworkVersion(nextAction.NewNetworkVersion);
                    }
                    else if (nextAction.UsesVersioning &&
                             nextAction.OldVersion == GetObjectVersion(nextAction.GameObjectID))
                    {
                        IncomingActions.Dequeue().ExecuteOnClient();
                        SetNetworkVersion(nextAction.NewNetworkVersion);
                    }
                    else
                    {
                        yield return new WaitForSeconds(PING_TO_SERVER);
                    }
                }
                else if (MissingNetworkVersions.Count > 0 && IncomingActions.Count == 0)
                {
                    int smallestMissingVersion = MissingNetworkVersions.Min();
                    if (NetworkVersion - smallestMissingVersion > 5) // FIXME: Magic Number
                    {
                        //ReSync();
                        throw new NotImplementedException();
                    }
                    else
                    {
                        if (Time.realtimeSinceStartup - lastMissingRequest > 1.5f)
                        {
                            var missingList = MissingNetworkVersions.OrderBy(v => v).ToList();
                            new RequestNetAction(missingList).Execute();
                            lastMissingRequest = Time.realtimeSinceStartup;
                        }
                    }
                    yield return new WaitForSeconds(PING_TO_SERVER);
                }
            }
            // IMPORTANT! Destroy CoRoutine.
            ClientExecuteInOrder = null;
        }

        /// <summary>
        /// <c>SERVER</c><br/>
        /// Determines whether the received NetAction can be performed safely.
        /// 
        /// </summary>
        /// <param name="netAction">The NetAction in question.</param>
        /// <returns><c>True</c> if it needs to be rejected,<br/>
        /// <c>False</c> if the NetAction can be performed. </returns>
        private bool ConcurrencyCheck(ConcurrentNetAction netAction)
        {
            int networkDifference = NetworkVersion - netAction.NetworkVersion;
            // We have the matching network version and no recent rejection
            // We can assume that the NetAction was made on the current state
            if (networkDifference == 0 && !RecentRejection)
            {
                AcceptConcurrentAction(netAction);
                return false;
            }
            // mismatch of network version or recent rejection -> maybe tied client action
            // first we try to incorporate the change
            else
            {
                if (netAction.UsesVersioning)   // Is an object versioning used?
                {
                    // We need a version match and do not accept self-incremented
                    if (GetServerObjectVersion(netAction.GameObjectID) == netAction.OldVersion &&
                        LastChangeBy == netAction.Requester)
                    {
                        AcceptConcurrentAction(netAction);
                        return false;
                    }
                    // here we try to find out whether we can apply the change either way
                    // this is complex, so we want to avoid this
                    // First we want to know, if the object of our desire has been deleted
                    else if (GetServerObjectVersion(netAction.GameObjectID) != -1 && IsSafeAction(netAction))
                    {
                        // the action seems to be safe to perform
                        AcceptConcurrentAction(netAction);
                        return false;
                    }
                    else // the change must not be accepted
                    {
                        RejectAction(netAction);
                        return true;
                    }
                }
                else // no versioned object -> deletion or delete-reversal or set-select
                {
                    if (netAction is SetSelectNetAction)
                    {
                        if (ObjectVersion.TryGetValue(netAction.GameObjectID, out int version) &&
                            version != -1) // do we need to check whether the object version matches?
                        {
                            // the action seems to be safe to perform
                            AcceptConcurrentAction(netAction);
                            return false;
                        }
                    }
                    else if (netAction is DeleteNetAction deleteAction &&
                        deleteAction.GetVersionedObjects() is Dictionary<string, int> deleteObjectVersions)
                    {
                        // use LINQ to check whether all affected versions match
                        bool actionAcceptable = deleteObjectVersions
                                                .All(kv => GetServerObjectVersion(kv.Key) == kv.Value);

                        // on a mismatch we have elements left and reject the DeleteNetAction
                        if (actionAcceptable)
                        {
                            AcceptConcurrentAction(netAction);
                            return false;
                        }
                    }
                    else if (netAction is RegenerateNetAction regenerateAction)
                    {
                        // use LINQ to check whether there is ANY not deleted element
                        bool actionAcceptable = regenerateAction
                                                .GetRegenerateList()
                                                .All(id => GetServerObjectVersion(id) == -1);

                        if (actionAcceptable && NoRecentStructuralChanges(regenerateAction.Requester))
                        {
                            AcceptConcurrentAction(netAction);
                            return false;
                        }
                    }
                }
            }
            RejectAction(netAction);
            return true;
        }

        /// <summary>
        /// <c>SERVER</c><br/>
        /// Routine to update network version and distribute new object version.
        /// </summary>
        /// <param name="netAction"> accepted NetAction.</param>
        private void AcceptConcurrentAction(ConcurrentNetAction netAction)
        {
            NetworkVersion++;
            netAction.NetworkVersion = NetworkVersion;
            if (netAction.UsesVersioning)
            {
                netAction.NewVersion = IncrementObjectVersion(netAction.GameObjectID);
                LastChangeBy = netAction.Requester;
            }
        }

        /// <summary>
        /// Use this before <see cref="RegenerateNetAction"/>,
        /// there must be no structural changes after deletion.
        /// CAUTION: This does not account for History-undone SetParents or Deletions!
        /// Delete -> SetParent -> Undo SetParent ->X Regenerate: Does NOT work!
        /// Delete #1 -> Delete #2 ->  Regenerate #2 ->X Regerate #1: Does NOT work!
        /// </summary>
        private bool NoRecentStructuralChanges(ulong requester)
        {
            AbstractNetAction tempAction;
            for (int i = Network.NetworkActionList.Count - 1; i > 0; i--)
            {
                tempAction = ActionSerializer.Deserialize(Network.NetworkActionList[i]);

                if (tempAction is not ConcurrentNetAction)
                { continue; }

                if (tempAction is DeleteNetAction && tempAction.Requester == requester)
                { return true; }

                if (tempAction is DeleteNetAction || tempAction is SetParentNetAction)
                { return false; }

            }
            return false;
        }

        /// <summary>
        /// <c>SERVER</c><br/>
        /// Routine to send rejection.
        /// </summary>
        /// <param name="netAction"> accepted NetAction.</param>
        private void RejectAction(ConcurrentNetAction netAction)
        {
            string serializedRejection = ActionSerializer.Serialize(netAction.GetRejection(ServerClientId));
            RecentRejection = true;
            // Must use "unsafe" because the server requests this
            ExecuteActionUnsafeClientRpc(serializedRejection, RpcTarget.Single(netAction.Requester, RpcTargetUse.Temp));
        }

        /// <summary>
        /// <c>SERVER</c><br/>
        /// Checks whether the action is safe to perform despite the same object version.
        /// To avoid masking, we do not allow to of the same actions.
        /// THIS SEEMS TO BE EXPENSIVE
        /// </summary>
        /// <param name="action">The action whose executability is to be checked.</param>
        /// <returns><c>True</c> if we can perform the action, <br/><c>False</c> otherwise.</returns>
        private bool IsSafeAction(ConcurrentNetAction action)
        {
            int length = Network.NetworkActionList.Count - 1;
            if (length > 2 && ConcurrentNetRules.IsSafeAction.Contains(action.GetType()))
            {
                AbstractNetAction tempAction;
                for (int i = length; i > (length - 3); i--)
                {
                    tempAction = ActionSerializer.Deserialize(Network.NetworkActionList[i]);
                    if (tempAction is ConcurrentNetAction tempConAction &&      // is it a ConcurrentAction?
                        tempConAction.GameObjectID == action.GameObjectID &&    // are we looking at the same object?
                        tempConAction.GetType() != action.GetType())            // would these actions mask each other?
                    {
                        return true;
                    }
                }

            }
            return false;
        }


        /// <summary>
        ///  Calculates the network version for new NetActions.
        /// </summary>
        /// <returns>Usable network version as <c>int</c>.</returns>
        public int GetCurrentClientNetworkVersion()
        {
            return NetworkVersion + PendingActions.Count;
        }

        /// <summary>
        ///  Returns the actual network version without pending actions.
        /// </summary>
        /// <returns>Actual network version as <c>uint</c>.</returns>
        public int GetActualNetworkVersion()
        {
            return NetworkVersion;
        }

        /// <summary>
        /// Sets the received NetAction and 
        /// </summary>
        /// <param name="version"></param>
        private void SetNetworkVersion(int newVersion)
        {
            if (newVersion < NetworkVersion)
            {
                MissingNetworkVersions.Remove(newVersion);
                return;
            }

            if (newVersion > NetworkVersion + 1)
            {
                for (int i = NetworkVersion + 1; i < newVersion; i++)
                {
                    MissingNetworkVersions.Add(i);
                }
            }

            NetworkVersion = newVersion;
        }

        /// <summary>
        /// <c>SERVER</c><br/>
        /// Returns the server version of an object.<br/>
        /// To use <see cref="ObjectVersion"/> in a lazy way.
        /// </summary>
        /// <param name="objectId">The ID of the versioned object.</param>
        /// <returns><c>0</c> if there is no change<br/><c>-1</c> if the object was deleted<br/>
        /// <c>int</c> of the version otherwise</returns>
        public int GetServerObjectVersion(string objectId)
        {
            if (ObjectVersion.TryGetValue(objectId, out int serverVersion))
            {
                return serverVersion;
            }
            return 0;
        }

        /// <summary>
        /// <c>CLIENT</c><br/>
        /// Returns the current version of versioned objects.
        /// </summary>
        /// <param name="objectId"> The ID of the derserved versioned object.</param>
        /// <returns>Current object version.</returns>
        public int GetObjectVersion(string objectId)
        {
            int version = -2;
            // First: Check whether we already have pending changes, these must be higher than other versions.
            for (int i = 0; i < PendingActions.Count; i++)
            {
                if (PendingActions[i].GameObjectID == objectId && (PendingActions[i].NewVersion ?? 0) > version)
                {
                    version = (int)PendingActions[i].NewVersion;
                }
            }
            // If there are no pending actions we try to find if this objects was modified since the beginning.
            if (version < -1 && ObjectVersion.TryGetValue(objectId, out int objectVersion))
            {
                return objectVersion;
            }

            return version == -2 ? 0 : version;
        }

        /// <summary>
        /// <c>CLIENT</c><br/>
        /// The client uses this to set the version according to the NetAction
        /// received from the server.
        /// </summary>
        /// <param name="objectId"> of the versioned object.</param>
        /// <param name="version"> new version to set.</param>
        public void SetObjectVersion(string objectId, int version)
        {
            ObjectVersion[objectId] = version;
        }

        /// <summary>
        /// <c>SERVER</c><br/>
        /// The object version needs to be incremented for conflict detection.
        /// Increment iff the NetAction provides an object version.
        /// </summary>
        /// <param name="objectId"> The ID of the derserved versioned object.</param>
        /// <returns>Value of 1 if it is newly versioned, the version otherwise.</returns>
        private int IncrementObjectVersion(string objectId)
        {
            if (ObjectVersion.TryGetValue(objectId, out int objectVersion) && objectVersion > 0)
            {
                ObjectVersion[objectId]++;
                return (objectVersion + 1); // no additional dictionary access
            }
            ObjectVersion[objectId] = 1;
            return 1;
        }
    }
}