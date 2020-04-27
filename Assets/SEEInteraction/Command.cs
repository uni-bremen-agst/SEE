using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Command
{

    public static class CommandHistory
    {
        internal static List<AbstractCommand> commands = new List<AbstractCommand>();
        private static int currentCommand = -1;
        private static int commandCount = 0;

        public static void Redo()
        {
            if (currentCommand + 1 < commandCount)
            {
                commands[++currentCommand].Redo();
            }
        }

        public static void Undo()
        {
            if (currentCommand >= 0)
            {
                commands[currentCommand--].Undo();
            }
        }

        internal static void Clear()
        {
            commands.Clear();
            currentCommand = -1;
            commandCount = 0;
        }

        internal static void OnExecute(AbstractCommand command)
        {
            if (currentCommand + 1 < commands.Count)
            {
                commands[++currentCommand] = command;
                commandCount = currentCommand + 1;
            }
            else
            {
                currentCommand++;
                commandCount++;
                commands.Add(command);
            }
        }
    }

    public abstract class AbstractCommand
    {
        public bool buffer;
        internal CommandAction action;

        public AbstractCommand(bool buffer)
        {
            this.buffer = buffer;
            action = CommandAction.None;
        }

        public void Execute()
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
            action = CommandAction.Execute;
            CommandHistory.OnExecute(this);
            Net.Network.SendCommand(this);
        }

        public void Redo()
        {
            action = CommandAction.Redo;
            Net.Network.SendCommand(this);
        }

        public void Undo()
        {
            action = CommandAction.Undo;
            Net.Network.SendCommand(this);
        }

        internal abstract void ExecuteOnClient();
        internal abstract void ExecuteOnServer();
        internal abstract void RedoOnClient();
        internal abstract void RedoOnServer();
        internal abstract void UndoOnClient();
        internal abstract void UndoOnServer();
    }

    internal enum CommandAction
    {
        None = 0,
        Execute,
        Redo,
        Undo
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
