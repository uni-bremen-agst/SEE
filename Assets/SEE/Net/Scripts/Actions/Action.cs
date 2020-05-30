using System;
using System.Net;
using UnityEngine;

namespace SEE.Net
{

    public abstract class AbstractAction
    {
        private static int nextIndex = 0;

        public int index = -1;
        public string requesterIPAddress;
        public int requesterPort;
        public bool buffer;
        public bool executed;



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
            executed = false;
        }



        protected bool IsRequester()
        {
            if (Net.Network.UseInOfflineMode)
            {
                return true;
            }

            IPEndPoint requesterEndPoint = new IPEndPoint(IPAddress.Parse(requesterIPAddress), requesterPort);
            bool result = Client.LocalEndPoint.Equals(requesterEndPoint);
            return result;
        }



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

        internal void ExecuteOnClientBase()
        {
            try
            {
                bool result = ExecuteOnClient();
                if (result)
                {
                    if (buffer)
                    {
                        ActionHistory.OnExecute(this);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

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



        protected abstract bool ExecuteOnServer();

        protected abstract bool ExecuteOnClient();

        protected abstract bool UndoOnServer();

        protected abstract bool UndoOnClient();

        protected abstract bool RedoOnServer();

        protected abstract bool RedoOnClient();



#if UNITY_EDITOR
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



    internal static class ActionSerializer
    {
        internal static string Serialize(AbstractAction action)
        {
            string result = action.GetType().ToString() + ';' + JsonUtility.ToJson(action);
            return result;
        }

        internal static AbstractAction Deserialize(string data)
        {
            string[] tokens = data.Split(new char[] { ';' }, 2, StringSplitOptions.None);
            AbstractAction result = (AbstractAction)JsonUtility.FromJson(tokens[1], Type.GetType(tokens[0]));
            return result;
        }
    }

}
