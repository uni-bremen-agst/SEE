using SEE.Net.Internal;
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace SEE.Command
{

    internal static class CommandHistory
    {
        internal static List<AbstractCommand> commands = new List<AbstractCommand>();
        internal static List<GameObject> commandHistoryElements = new List<GameObject>();

        internal static void OnExecute(AbstractCommand command)
        {
            GameObject prefab = Resources.Load<GameObject>("CommandHistoryElement");
            prefab.GetComponentInChildren<UnityEngine.UI.Text>().text = command.GetType().Name;
            Transform parent = UnityEngine.Object.FindObjectOfType<UnityEngine.UI.VerticalLayoutGroup>().transform;
            GameObject commandHistoryElement = UnityEngine.Object.Instantiate(prefab, parent);
            commandHistoryElement.GetComponent<CommandHistoryElement>().index = commands.Count;
            
            commands.Add(command);
            commandHistoryElements.Add(commandHistoryElement);
        }

        internal static void Undo(int index)
        {
            commands[index].Undo();
        }

        internal static void Redo(int index)
        {
            commands[index].Redo();
        }
    }

    public abstract class AbstractCommand
    {
        private static int nextIndex = 0;

        public int index = -1;
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
                if (Net.Network.UseInOfflineMode)
                {
                    ExecuteOnServerBase();
                    ExecuteOnClientBase();
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
                if (Net.Network.UseInOfflineMode)
                {
                    UndoOnServerBase();
                    UndoOnClientBase();
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
                if (Net.Network.UseInOfflineMode)
                {
                    RedoOnServerBase();
                    RedoOnClientBase();
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
                        CommandHistory.OnExecute(this);
                    }
                    executed = true;
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
                if (result)
                {
                    executed = false;
                }
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
                if (result)
                {
                    executed = true;
                }
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
