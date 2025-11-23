using SEE.Game;
using SEE.UI.Notification;
using System;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// !!! IMPORTANT !!!
    ///
    ///   Rules for every deriving class:
    ///
    ///     1. Every field to be serialized MUST be public!
    ///     2. Deriving classes MUST NOT have fields of the type GameObjects or
    ///        MonoBehaviours.
    ///     3. Fields used for reverse actions may be private as they don't need
    ///        to be serialized.
    ///
    ///   These rules are necessary, to allow (de)serialization of the classes for
    ///   networking.
    ///
    ///   See section Networking.Actions.Creation in
    ///   <see href="https://github.com/uni-bremen-agst/SEE/wiki/Networking">here</see>
    ///   for further details.
    ///
    ///
    ///
    /// An abstract concurrent networked action. Actions can be completely arbitrary and 
    /// can be executed on the server and/or client.
    /// </summary>
    public abstract class ConcurrentNetAction : AbstractNetAction
    {
        /// <summary>
        /// The old ID of an Attributable Element.
        /// </summary>
        public int? OldVersion;

        /// <summary>
        /// The new ID of an Attributable Element.
        /// </summary>
        public int? NewVersion;

        /// <summary>
        /// The unique name of the gameObject affected by the Action.
        /// </summary>
        public string GameObjectID;

        /// <summary>
        /// The current NetworkVersion at the moment of the NetAction.
        /// </summary>
        public int NetworkVersion;

        /// <summary>
        /// The current NetworkVersion at the moment of the NetAction.
        /// </summary>
        public bool UsesVersioning = false;

        /// <summary>
        /// The new NetworkVersion after the NetAction.
        /// </summary>
        public int NewNetworkVersion { get; set; }

        /// <summary>
        /// Whether it is used to remind Server in case of a package loss.
        /// </summary>
        public bool IsReminder { get; set; }

        /// <summary>
        /// Stores a delegate for an undo-action if necessary.
        /// </summary>
        [System.NonSerialized]
        private protected Action UndoAction;

        /// <summary>
        /// Constructor of an arbitrary Concurrent Net Action.
        /// </summary>
        /// <param name="gid">the unique ObjectID</param>
        public ConcurrentNetAction(string gid)
        {
            NetworkVersion = Network.ActionNetworkInst.Value.GetCurrentClientNetworkVersion();
            GameObjectID = gid;
            Network.ActionNetworkInst.Value.AddPendingAction(this);
        }

        /// <summary>
        /// Comaprison method for incoming ConcurrentActions. Is used to determine whether own Action got
        /// accepted or rejected.
        /// </summary>
        /// <param name="netAction">Received NetAction to compare to.</param>
        public bool Equals(ConcurrentNetAction netAction)
        {
            return  (NetworkVersion == netAction.NetworkVersion) &&
                    (GameObjectID == netAction.GameObjectID) &&
                    (OldVersion == netAction.OldVersion);
        }

        /// <summary>
        /// Creates an RejectNetAction object to send to corresponding client.
        /// </summary>
        public RejectNetAction GetRejection (ulong serverId)
        {
            return new RejectNetAction(GameObjectID, NetworkVersion, OldVersion, serverId);
        }

        /// <summary>
        /// The implementation of the reversal of an action for the client. 
        /// This method will be called if the action gets rejected by the server.
        /// Parameters for this function may be private.
        /// </summary>
        public abstract void Undo();

        /// <summary>
        /// Generates an UI-Notification for the Client on Rollback.
        /// </summary>
        public void RollbackNotification()
        {
            ConcurrentNetRules.RollbackNotification(this);
        }

        /// <summary>
        /// Whether the ConcurrentNetAction uses versioned objects.
        /// </summary>
        /// <param name="objectId">ID of the versioned object.</param>
        public void UseObjectVersion(string objectId)
        {
            int version = Network.ActionNetworkInst.Value.GetObjectVersion(objectId);
            if (version == -1) // the object has been deleted
            {
                OldVersion = -1;
                NewVersion = 1;
            } else {                // we can increment the version
                OldVersion = version;
                NewVersion = version + 1;
            }
            UsesVersioning = true;
        }

        /// <summary>
        /// This is used to set the versioning on client execution.
        /// </summary>
        public void SetVersion()
        {
            if(UsesVersioning && NewVersion != null)
            {
                Network.ActionNetworkInst.Value.SetObjectVersion(GameObjectID, (int)NewVersion);
            }
        }

        /// <summary>
        /// At this point we wanted to reverse an action that can't be undone.
        /// The client needs to resync his SEE instance with the server.
        /// </summary>
        public static void ReSync()
        {
            ShowNotification.Error("NETWORK ERROR", "Please leave the session and reconnect.");
            // FIXME: Needs to be implemented according to new persistence model.
            throw new NotImplementedException();
        }

    }
}
