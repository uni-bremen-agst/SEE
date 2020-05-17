using SEE.Net.Internal;
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace SEE.Command
{

    internal static class CommandHistory
    {
        private static List<AbstractCommand> commands = new List<AbstractCommand>();

        internal static void OnExecute(AbstractCommand command)
        {
            commands.Add(command);
        }
    }

    public abstract class AbstractCommand
    {
        public string requesterIPAddress;
        public int requesterPort;
        public bool buffer;
        public bool executed;



        public AbstractCommand(bool buffer)
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



        public void Execute()
        {
            if (!executed)
            {
                executed = true;
                if (Net.Network.UseInOfflineMode)
                {
                    ExecuteOnServerBase();
                    ExecuteOnClientBase();
                    CommandHistory.OnExecute(this);
                }
                else
                {
#if UNITY_EDITOR
                    DebugAssertCanBeSerialized();
#endif
                    ExecuteCommandPacket packet = new ExecuteCommandPacket(this);
                    Net.Network.SubmitPacket(Client.Connection, packet);
                }
            }
        }

        public void Undo()
        {
            if (executed)
            {
                executed = false;
                if (Net.Network.UseInOfflineMode)
                {
                    UndoOnServer();
                    UndoOnClient();
                }
                else
                {
#if UNITY_EDITOR
                    DebugAssertCanBeSerialized();
#endif
                    UndoCommandPacket packet = new UndoCommandPacket(this);
                    Net.Network.SubmitPacket(Client.Connection, packet);
                }
            }
        }

        public void Redo()
        {
            if (!executed)
            {
                executed = true;
                if (Net.Network.UseInOfflineMode)
                {
                    RedoOnServer();
                    RedoOnClient();
                }
                else
                {
#if UNITY_EDITOR
                    DebugAssertCanBeSerialized();
#endif
                    RedoCommandPacket packet = new RedoCommandPacket(this);
                    Net.Network.SubmitPacket(Client.Connection, packet);
                }
            }
        }



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

        internal void ExecuteOnClientBase()
        {
            try
            {
                ExecuteOnClient();
                if (buffer)
                {
                    CommandHistory.OnExecute(this);
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
                UndoOnClient();
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
                RedoOnClient();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }



        protected abstract void ExecuteOnServer();

        protected abstract void ExecuteOnClient();

        protected abstract void UndoOnServer();

        protected abstract void UndoOnClient();

        protected abstract void RedoOnServer();

        protected abstract void RedoOnClient();



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
                Debug.LogError("This command can not be serialized into json! This class probably contains GameObjects or other components. Consider removing some members of '" + GetType().ToString() + "'!");
                throw e;
            }
        }
#endif
    }



    internal static class CommandSerializer
    {
        internal static string Serialize(AbstractCommand command)
        {
            string result = command.GetType().ToString() + ';' + JsonUtility.ToJson(command);
            return result;
        }

        internal static AbstractCommand Deserialize(string data)
        {
            string[] tokens = data.Split(new char[] { ';' }, 2, StringSplitOptions.None);
            AbstractCommand result = (AbstractCommand)JsonUtility.FromJson(tokens[1], Type.GetType(tokens[0]));
            return result;
        }
    }

}
