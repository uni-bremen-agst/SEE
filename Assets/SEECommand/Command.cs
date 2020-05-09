using SEE.Net.Internal;
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Command
{

    public class State
    {
        public IPEndPoint stateOwner;
        public GameObject[] predecessors;
        public GameObject[] gameObjects;

        public State(IPEndPoint stateOwner, GameObject[] predecessors, GameObject[] gameObjects)
        {
            this.stateOwner = stateOwner;
            this.predecessors = predecessors;
            this.gameObjects = gameObjects;
        }

        public void Destroy()
        {
            foreach (GameObject go in gameObjects)
            {
                UnityEngine.Object.Destroy(go);
            }
        }
    }

    public static class CommandHistory
    {
        private static int currentState = -1;
        private static List<State> history = new List<State>(); // TODO: circular array, so we can limit the history memory

        internal static void OnExecute(IPEndPoint stateOwner, GameObject[] originalGameObjects, GameObject[] copiedAndModifiedGameObjects)
        {
            Assert.IsNotNull(originalGameObjects);
            Assert.IsNotNull(copiedAndModifiedGameObjects);
            Assert.IsTrue(originalGameObjects.Length == copiedAndModifiedGameObjects.Length);

            State state = new State(stateOwner, originalGameObjects, copiedAndModifiedGameObjects);

            if (currentState + 1 < history.Count)
            {
                for (int i = currentState + 1; i < history.Count; i++)
                {
                    history[i].Destroy();
                }
                history.RemoveRange(currentState + 1, history.Count - (currentState + 1));
            }

            if (currentState != -1)
            {
                for (int i = 0; i < originalGameObjects.Length; i++)
                {
                    originalGameObjects[i]?.SetActive(false);
                }
            }

            currentState++;
            history.Add(state);
        }

        public static void Undo()
        {
            bool canUndo = currentState != -1 && (Client.LocalEndPoint == null || Client.LocalEndPoint.Equals(history[currentState].stateOwner));
            if (canUndo)
            {
                Net.Network.UndoCommand();
            }
        }

        internal static void UndoOnClient()
        {
            if (currentState != -1)
            {
                for (int i = 0; i < history[currentState].gameObjects.Length; i++)
                {
                    history[currentState].gameObjects[i]?.SetActive(false);
                    history[currentState].predecessors[i]?.SetActive(true);
                }
                currentState--;
            }
        }

        public static void Redo()
        {
            bool canRedo = currentState + 1 < history.Count && (Client.LocalEndPoint == null || Client.LocalEndPoint.Equals(history[currentState + 1].stateOwner));
            if (canRedo)
            {
                Net.Network.RedoCommand();
            }
        }

        internal static void RedoOnClient()
        {
            if (currentState + 1 < history.Count)
            {
                currentState++;
                for (int i = 0; i < history[currentState].gameObjects.Length; i++)
                {
                    history[currentState].predecessors[i]?.SetActive(false);
                    history[currentState].gameObjects[i]?.SetActive(true);
                }
            }
        }
    }

    public abstract class AbstractCommand
    {
        public string requesterIPAddress;
        public int requesterPort;
        public bool buffer;

        // has NOT is used, so deserialization always deserializes this to false, as it is a private field
        private bool hasNotBeenExecutedYet = true;

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
        }

        public void Execute()
        {
            if (hasNotBeenExecutedYet)
            {
#if UNITY_EDITOR
                try
                {
                    JsonUtility.ToJson(this);
                }
                catch (Exception)
                {
                    Debug.LogError("This command can not be serialized into json! This class probably contains GameObjects or other components. Consider removing some members of '" + GetType().ToString() + "'!");
                }
#endif
                hasNotBeenExecutedYet = false;
                Net.Network.ExecuteCommand(this);
            }
        }

        internal void ExecuteOnServerBase()
        {
            ExecuteOnServer();
        }

        internal void ExecuteOnClientBase()
        {
            KeyValuePair<GameObject[], GameObject[]> gameObjects = ExecuteOnClient();
            if (buffer)
            {
                IPEndPoint stateOwner = Client.LocalEndPoint == null ? null : new IPEndPoint(IPAddress.Parse(requesterIPAddress), requesterPort);
                CommandHistory.OnExecute(stateOwner, gameObjects.Key, gameObjects.Value);
            }
        }

        internal abstract void ExecuteOnServer();

        internal abstract KeyValuePair<GameObject[], GameObject[]> ExecuteOnClient();
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
            AbstractCommand command = (AbstractCommand)JsonUtility.FromJson(tokens[1], Type.GetType(tokens[0]));
            return command;
        }
    }

}
