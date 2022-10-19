using System;
using System.Net;
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
        /// The IP-address of the requester of this action.
        /// </summary>
        public string RequesterIPAddress;

        /// <summary>
        /// The port of the requester of this action.
        /// </summary>
        public int RequesterPort;

        /// <summary>
        /// The IP-addresses of the recipients.
        /// </summary>
        public string[] RecipientsIPAddresses;

        /// <summary>
        /// The ports of the recipients.
        /// </summary>
        public int[] RecipientsPorts;

        /// <summary>
        /// Constructs an abstract action.
        /// </summary>
        public AbstractNetAction()
        {
            IPEndPoint requester = Client.LocalEndPoint;
            SetRequester(requester);
        }

        /// <summary>
        /// Sets the requester of this action to given end-point.
        /// </summary>
        /// <param name="requester">The requester.</param>
        public void SetRequester(IPEndPoint requester)
        {
            if (requester != null)
            {
                RequesterIPAddress = requester.Address.ToString();
                RequesterPort = requester.Port;
            }
            else
            {
                RequesterIPAddress = null;
                RequesterPort = -1;
            }
        }

        /// <summary>
        /// Returns the <see cref="IPEndPoint"/> of the client, that requested this
        /// action.
        /// </summary>
        /// <returns>The <see cref="IPEndPoint"/> of the client, that requested this
        /// action.</returns>
        protected IPEndPoint GetRequester()
        {
            return new IPEndPoint(IPAddress.Parse(RequesterIPAddress), RequesterPort);
        }

        /// <summary>
        /// Checks, if the executing client is the one that requested this action.
        /// </summary>
        /// <returns><code>true</code> if this client requested this action, <code>false</code>
        /// otherwise.</returns>
        protected bool IsRequester()
        {
            if (RequesterIPAddress == null || RequesterPort == -1)
            {
                return true;
            }

            IPEndPoint requesterEndPoint = GetRequester();
            return Client.LocalEndPoint.Equals(requesterEndPoint);
        }

        /// <summary>
        /// Returns all of the recipients of this action.
        /// </summary>
        /// <returns>All of the recipients of this action.</returns>
        public IPEndPoint[] GetRecipients()
        {
            IPEndPoint[] result = null;

            if (RecipientsIPAddresses != null && RecipientsPorts != null)
            {
                result = new IPEndPoint[RecipientsIPAddresses.Length];
                for (int i = 0; i < RecipientsIPAddresses.Length; i++)
                {
                    result[i] = new IPEndPoint(IPAddress.Parse(RecipientsIPAddresses[i]), RecipientsPorts[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// Executes this action for the server and every client.
        ///
        /// The action will be sent to the server and from there broadcast to every
        /// client. The Server executes <see cref="ExecuteOnServer"/> and each Client
        /// executes <see cref="ExecuteOnClient"/> locally.
        /// </summary>
        /// <param name="recipients">The recipients of this action. If <code>null</code>,
        /// this actions will be executed everywhere.</param>
        public void Execute(IPEndPoint[] recipients = null)
        {
            if (recipients == null)
            {
                RecipientsIPAddresses = null;
                RecipientsPorts = null;
            }
            else
            {
                RecipientsIPAddresses = new string[recipients.Length];
                RecipientsPorts = new int[recipients.Length];
                for (int i = 0; i < recipients.Length; i++)
                {
                    RecipientsIPAddresses[i] = recipients[i].Address.ToString();
                    RecipientsPorts[i] = recipients[i].Port;
                }
            }
#if UNITY_EDITOR
            DebugAssertCanBeSerialized();
#endif
            ExecuteActionPacket packet = new ExecuteActionPacket(this);
            Network.SubmitPacket(Client.Connection, packet);
        }

        /// Executes the action on the server locally. This function is only called by
        /// <see cref="ExecuteActionPacket"/> or by <see cref="Execute"/> directly in
        /// offline mode. It must not be called otherwise!
        internal void ExecuteOnServerBase()
        {
            try
            {
                ExecuteOnServer();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Executes the action on the client locally. This function is only called by
        /// <see cref="ExecuteActionPacket"/> or by <see cref="Execute"/> directly in
        /// offline mode directly. It must not be called otherwise!
        /// </summary>
        internal void ExecuteOnClientBase()
        {
            try
            {
                ExecuteOnClient();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// The implementation of the action for the server. Returns whether the action
        /// could be executed successfully.
        ///
        /// If the implementation throws an exception, it will be interpreted just like
        /// returning <code>false</code>.
        /// </summary>
        protected abstract void ExecuteOnServer();

        /// <summary>
        /// The implementation of the action for the client. Returns whether the action
        /// could be executed successfully.
        ///
        /// If the implementation throws an exception, it will be interpreted just like
        /// returning <code>false</code>.
        /// </summary>
        protected abstract void ExecuteOnClient();

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
            if (result.RecipientsIPAddresses.Length == 0)
            {
                result.RecipientsIPAddresses = null;
                result.RecipientsPorts = null;
            }
            return result;
        }
    }
}
