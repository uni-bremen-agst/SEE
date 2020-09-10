using System;
using System.Net;
using UnityEngine;

namespace SEE.Net
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
    /// 
    /// 
    /// An abstract networked action. Actions can be completely arbitrary and can be
    /// executed on the server and/or client.
    /// </summary>
    public abstract class AbstractAction
    {
        /// <summary>
        /// The next unique ID of an action.
        /// </summary>
        private static int nextIndex = 0;

        /// <summary>
        /// The unique ID of the action.
        /// </summary>
        public int index = -1;

        /// <summary>
        /// The IP-address of the requester of this action.
        /// </summary>
        public string requesterIPAddress;

        /// <summary>
        /// The port of the requester of this action.
        /// </summary>
        public int requesterPort;

        /// <summary>
        /// Whether the action should be buffered, so that new clients in the future will
        /// receive it.
        /// </summary>
        public bool buffer;



        /// <summary>
        /// Constructs an abstract action.
        /// </summary>
        /// <param name="buffer">Whether the action should be buffered, so that new
        /// clients in the future will receive it.</param>
        public AbstractAction(bool buffer)
        {
            IPEndPoint requester = Client.LocalEndPoint;
            if (requester != null)
            {
                requesterIPAddress = requester.Address.ToString();
                requesterPort = requester.Port;
            }
            else
            {
                requesterIPAddress = null;
                requesterPort = -1;
            }
            this.buffer = buffer;
        }



        /// <summary>
        /// Checks, if the executing client is the one that requested this action.
        /// </summary>
        /// <returns><code>true</code> if this client requested this action, <code>false</code>
        /// otherwise.</returns>
        protected bool IsRequester()
        {
            if (Network.UseInOfflineMode)
            {
                return true;
            }

            IPEndPoint requesterEndPoint = new IPEndPoint(IPAddress.Parse(requesterIPAddress), requesterPort);
            bool result = Client.LocalEndPoint.Equals(requesterEndPoint);
            return result;
        }



        /// <summary>
        /// Executes this action for the server and every client.
        /// 
        /// The action will be sent to the server and from there broadcasted to every
        /// client. The Server executes <see cref="ExecuteOnServer"/> and each Client
        /// executes <see cref="ExecuteOnClient"/> locally.
        /// 
        /// If <see cref="Network.UseInOfflineMode"/> is <code>true</code>, this will be
        /// simulated locally without sending networked packets.
        /// </summary>
        public void Execute()
        {
            if (Network.UseInOfflineMode)
            {
                ExecuteOnServerBase();
                ExecuteOnClientBase();
            }
            else
            {
#if UNITY_EDITOR
                DebugAssertCanBeSerialized();
#endif
                ExecuteActionPacket packet = new ExecuteActionPacket(this);
                Network.SubmitPacket(Client.Connection, packet);
            }
        }

        /// <summary>
        /// Undos this action for the server and every client. The procedure is analogous
        /// to <see cref="Execute"/>.
        /// 
        /// Order of execution:
        /// 1. <see cref="UndoOnServer"/>
        /// 2. <see cref="UndoOnClient"/>
        /// </summary>
        public void Undo()
        {
            if (Network.UseInOfflineMode)
            {
                UndoOnServerBase();
                UndoOnClientBase();
            }
            else
            {
#if UNITY_EDITOR
                DebugAssertCanBeSerialized();
#endif
                UndoActionPacket packet = new UndoActionPacket(this);
                Network.SubmitPacket(Client.Connection, packet);
            }
        }

        /// <summary>
        /// Redos this action for the server and every client. The procedure is analogous
        /// to <see cref="Execute"/>.
        /// 
        /// Order of execution:
        /// 1. <see cref="RedoOnServer"/>
        /// 2. <see cref="RedoOnClient"/>
        /// </summary>
        public void Redo()
        {
            if (Network.UseInOfflineMode)
            {
                RedoOnServerBase();
                RedoOnClientBase();
            }
            else
            {
#if UNITY_EDITOR
                DebugAssertCanBeSerialized();
#endif
                RedoActionPacket packet = new RedoActionPacket(this);
                Network.SubmitPacket(Client.Connection, packet);
            }
        }



        /// Executes the action on the server locally. This function is only called by
        /// <see cref="ExecuteActionPacket"/> or by <see cref="Execute"/> directly in
        /// offline mode directly. It must not be called otherwise!
        internal void ExecuteOnServerBase()
        {
            try
            {
                if (buffer)
                {
                    index = nextIndex++;
                }
                else
                {
                    index = -1;
                }
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
                bool result = ExecuteOnClient();
                if (result)
                {
                    if (buffer)
                    {
                        ActionHistory.DebugOnExecute(this);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Undos the action on the server locally. This function is only called by
        /// <see cref="UndoActionPacket"/> or by <see cref="Undo"/> directly in
        /// offline mode directly. It must not be called otherwise!
        /// </summary>
        internal void UndoOnServerBase()
        {
            try
            {
                UndoOnServer();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Undos the action on the client locally. This function is only called by
        /// <see cref="UndoActionPacket"/> or by <see cref="Undo"/> directly in
        /// offline mode directly. It must not be called otherwise!
        /// </summary>
        internal void UndoOnClientBase()
        {
            try
            {
                bool result = UndoOnClient();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Redos the action on the server locally. This function is only called by
        /// <see cref="RedoActionPacket"/> or by <see cref="Redo"/> directly in
        /// offline mode directly. It must not be called otherwise!
        /// </summary>
        internal void RedoOnServerBase()
        {
            try
            {
                RedoOnServer();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Redos the action on the client locally. This function is only called by
        /// <see cref="RedoActionPacket"/> or by <see cref="Redo"/> directly in
        /// offline mode directly. It must not be called otherwise!
        /// </summary>
        internal void RedoOnClientBase()
        {
            try
            {
                bool result = RedoOnClient();
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
        /// <returns><code>true</code>, if the action could be executed, <code>false</code>
        /// otherwise.</returns>
        protected abstract bool ExecuteOnServer();

        /// <summary>
        /// The implementation of the action for the client. Returns whether the action
        /// could be executed successfully.
        /// 
        /// If the implementation throws an exception, it will be interpreted just like
        /// returning <code>false</code>.
        /// </summary>
        /// <returns><code>true</code>, if the action could be executed, <code>false</code>
        /// otherwise.</returns>
        protected abstract bool ExecuteOnClient();

        /// <summary>
        /// The implementation of undoing the action for the server. Returns whether the
        /// action could be undone successfully.
        /// 
        /// If the implementation throws an exception, it will be interpreted just like
        /// returning <code>false</code>.
        /// 
        /// If <see cref="buffer"/> is <code>false</code>, this function will never be called.
        /// </summary>
        /// <returns><code>true</code>, if the action could be undone, <code>false</code>
        /// otherwise.</returns>
        protected abstract bool UndoOnServer();

        /// <summary>
        /// The implementation of undoing the action for the client. Returns whether the
        /// action could be undone successfully.
        /// 
        /// If the implementation throws an exception, it will be interpreted just like
        /// returning <code>false</code>.
        /// 
        /// If <see cref="buffer"/> is <code>false</code>, this function will never be called.
        /// </summary>
        /// <returns><code>true</code>, if the action could be undone, <code>false</code>
        /// otherwise.</returns>
        protected abstract bool UndoOnClient();

        /// <summary>
        /// The implementation of redoing the action for the server. Returns whether the
        /// action could be redone successfully.
        /// 
        /// If the implementation throws an exception, it will be interpreted just like
        /// returning <code>false</code>.
        /// 
        /// If <see cref="buffer"/> is <code>false</code>, this function will never be called.
        /// </summary>
        /// <returns><code>true</code>, if the action could be redone, <code>false</code>
        /// otherwise.</returns>
        protected abstract bool RedoOnServer();

        /// <summary>
        /// The implementation of redoing the action for the client. Returns whether the
        /// action could be redone successfully.
        /// 
        /// If the implementation throws an exception, it will be interpreted just like
        /// returning <code>false</code>.
        /// 
        /// If <see cref="buffer"/> is <code>false</code>, this function will never be called.
        /// </summary>
        /// <returns><code>true</code>, if the action could be redone, <code>false</code>
        /// otherwise.</returns>
        protected abstract bool RedoOnClient();



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
            catch (Exception e)
            {
                Debug.LogError("This action can not be serialized into json! This class probably contains GameObjects or other components. Consider removing some members of '" + GetType().ToString() + "'!");
                throw e;
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
        internal static string Serialize(AbstractAction action)
        {
            string result = action.GetType().ToString() + ';' + JsonUtility.ToJson(action);
            return result;
        }

        /// <summary>
        /// Deserializes the given string to an action.
        /// </summary>
        /// <param name="data">The serialized action as a string.</param>
        /// <returns>The deserialized action.</returns>
        internal static AbstractAction Deserialize(string data)
        {
            string[] tokens = data.Split(new char[] { ';' }, 2, StringSplitOptions.None);
            AbstractAction result = (AbstractAction)JsonUtility.FromJson(tokens[1], Type.GetType(tokens[0]));
            return result;
        }
    }

}
