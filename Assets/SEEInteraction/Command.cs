using System;
using UnityEngine;

namespace SEE.Command
{

    public abstract class AbstractCommand
    {
        public bool buffer;

        public AbstractCommand(bool buffer)
        {
            this.buffer = buffer;
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
            Net.Network.ExecuteCommand(this);
        }

        internal abstract void ExecuteOnClient();
        internal abstract void ExecuteOnServer();
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
