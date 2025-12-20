using SEE.Game;
using System;
using Unity.Netcode;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// !!! IMPORTANT !!!
    ///
    ///   Rules for every deriving class:
    ///
    ///     1. Every field MUST be public!
    ///     2. Deriving classes MUST NOT have fields of the type GameObjects or
    ///        MonoBehaviours.
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
    /// An abstract networked action. Actions can be completely arbitrary and can be
    /// executed on the server and/or client.
    /// </summary>
    public abstract class AbstractNetAction
    {
        /// <summary>
        /// The Client ID of the requester.
        /// </summary>
        public ulong Requester;

        /// <summary>
        /// Should be sent to newly connecting clients
        /// </summary>
        public virtual bool ShouldBeSentToNewClient { get => true; }

        /// <summary>
        /// Constructs an abstract action.
        /// </summary>
        public AbstractNetAction()
        {
            Requester = NetworkManager.Singleton.LocalClientId;
        }

        /// <summary>
        /// Executes this action for the server and every client.
        ///
        /// The action will be sent to the server and from there broadcast to every
        /// client. The Server executes <see cref="ExecuteOnServer"/> and each Client
        /// executes <see cref="ExecuteOnClient"/> locally.
        /// </summary>
        /// <param name="recipients">The recipients of this action. If <c>null</c>
        /// or omitted, this actions will be executed on all clients.</param>
        public void Execute(ulong[] recipients = null)
        {
#if UNITY_EDITOR
            DebugAssertCanBeSerialized();
#endif
            Network.BroadcastAction(ActionSerializer.Serialize(this), recipients);
        }

        /// <summary>
        /// The implementation of the action for the server. This method will be called
        /// only for the server.
        /// </summary>
        public virtual void ExecuteOnServer()
        {
            // The default implementation does nothing.
        }

        /// <summary>
        /// The implementation of the action for the client. This method will be called
        /// for all connected clients excluding the requester.
        /// </summary>
        public abstract void ExecuteOnClient();

        /// <summary>
        /// Retrieves and returns the game object registered at <see cref="GraphElementIDMap"/>
        /// under the given <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The unique ID that is to be used to retrieve the game object.</param>
        /// <returns>The game object registered at <see cref="GraphElementIDMap"/>.</returns>
        /// <exception cref="Exception">Thrown if <see cref="GraphElementIDMap"/>
        /// has no game object registered by <paramref name="id"/>.</exception>
        protected static GameObject Find(string id)
        {
            GameObject result = GraphElementIDMap.Find(id);
            if (result == null)
            {
                throw new Exception($"There is no game object with the ID {id}.");
            }
            else
            {
                return result;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Checks whether the action is serializable in the current state. GameObjects
        /// and Components can not be part of an action, as the JsonUtility is unable to
        /// serialize them.
        /// </summary>
        /// <returns><code>true</code> if serialization is possible, <code>false</code> otherwise.
        /// </returns>
        private bool DebugAssertCanBeSerialized()
        {
            try
            {
                JsonUtility.ToJson(this);
                return true;
            }
            catch (Exception)
            {
                Debug.LogError("This action can not be serialized into json! This class probably contains GameObjects or other components. Consider removing some members of '" + GetType() + "'!");
                throw;
            }
        }
#endif
    }

    /// <summary>
    /// Responsible for serialization and deserialization of actions.
    /// </summary>
    internal static class ActionSerializer
    {
        /// <summary>
        /// Serializes the given action to a string.
        /// </summary>
        /// <param name="action">The action to be serialized.</param>
        /// <returns>The serialized action as a string.</returns>
        internal static string Serialize(AbstractNetAction action)
        {
            return action.GetType().ToString() + ';' + JsonUtility.ToJson(action);
        }

        /// <summary>
        /// Deserializes the given string to an action.
        /// </summary>
        /// <param name="data">The serialized action as a string.</param>
        /// <returns>The deserialized action.</returns>
        internal static AbstractNetAction Deserialize(string data)
        {
            string[] tokens = data.Split(new[] { ';' }, 2, StringSplitOptions.None);
            AbstractNetAction result = (AbstractNetAction)JsonUtility.FromJson(tokens[1], Type.GetType(tokens[0]));
            return result;
        }
    }
}
