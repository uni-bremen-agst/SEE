using System;
using System.Collections.Generic;
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
                Debug.LogError("This command can not be serialized into json! This class probably contains GameObjects or other Components. Reconsider members of '" + GetType().ToString() + "'!");
            }
#endif
            Net.Network.ExecuteCommand(this);
        }

        internal abstract void ExecuteOnClient();
        internal abstract void ExecuteOnServer();
    }

    internal static class CommandSerializer
    {
        internal class NonSerializableException : Exception
        {
            public NonSerializableException() : base("Command can not be serialized! Command will only be executed locally!") { }
            public NonSerializableException(Type type) : base("Command of type '" + type.ToString() + "' can not be serialized! Command will only be executed locally!") { }
        }
        internal class NonDeserializableException : Exception
        {
            public NonDeserializableException() : base("Command can not be deserialized! Command will not be executed locally!") { }
            public NonDeserializableException(string data) : base("Command with data '" + data + "' can not be deserialized! Command will not be executed locally!") { }
        }

        private static readonly Dictionary<Type, Func<string, AbstractCommand>> deserializationDict = new Dictionary<Type, Func<string, AbstractCommand>>()
        {
            { typeof(LoadCityCommand), (s) => JsonUtility.FromJson<LoadCityCommand>(s) },
            { typeof(InstantiateCommand), (s) => JsonUtility.FromJson<InstantiateCommand>(s) }
        };

        internal static string Serialize(AbstractCommand command)
        {
            string result = command.GetType().ToString() + ';' + JsonUtility.ToJson(command);
            return result;
        }

        internal static AbstractCommand Deserialize(string data)
        {
            string[] tokens = data.Split(new char[] { ';' }, 2, StringSplitOptions.None);
            Type type = Type.GetType(tokens[0]);

            AbstractCommand result = null;

            if (deserializationDict.ContainsKey(type))
            {
                result = deserializationDict[type](tokens[1]);
            }

            return result;
        }
    }

}
