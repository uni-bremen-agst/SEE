using System;
using System.Collections.Generic;

namespace SEE.Interact
{

    public abstract class AbstractInteraction
    {
        public readonly bool buffer;

        public AbstractInteraction(bool buffer = true)
        {
            this.buffer = buffer;
        }

        public void Execute()
        {
            Net.Network.ExecuteInteraction(this);
        }

        internal abstract void ExecuteLocally();
    }

    internal static class InteractionSerializer
    {
        internal class NonSerializableException : Exception
        {
            public NonSerializableException() : base("Interaction can not be serialized! Interaction will only be executed locally!") { }
            public NonSerializableException(Type type) : base("Interaction of type '" + type.ToString() + "' can not be serialized! Interaction will only be executed locally!") { }
        }
        internal class NonDeserializableException : Exception
        {
            public NonDeserializableException() : base("Interaction can not be deserialized! Interaction will not be executed locally!") { }
            public NonDeserializableException(string data) : base("Interaction with data '" + data + "' can not be deserialized! Interaction will not be executed locally!") { }
        }

        private static readonly Dictionary<Type, Func<AbstractInteraction, string>> serializationDict = new Dictionary<Type, Func<AbstractInteraction, string>>()
        {
            { typeof(DeleteBuildingInteraction), (i) => DeleteBuildingInteractionSerializer.Serialize((DeleteBuildingInteraction) i) },
            { typeof(LoadCityInteraction), (i) => LoadCityInteractionSerializer.Serialize((LoadCityInteraction) i) },
            { typeof(MoveBuildingInteraction), (i) => MoveBuildingInteractionSerializer.Serialize((MoveBuildingInteraction) i) }
        };

        private static readonly Dictionary<Type, Func<string, AbstractInteraction>> deserializationDict = new Dictionary<Type, Func<string, AbstractInteraction>>()
        {
            { typeof(DeleteBuildingInteraction), (s) => DeleteBuildingInteractionSerializer.Deserialize(s) },
            { typeof(LoadCityInteraction), (s) => LoadCityInteractionSerializer.Deserialize(s) },
            { typeof(MoveBuildingInteraction), (s) => MoveBuildingInteractionSerializer.Deserialize(s) }
        };

        internal static string Serialize(AbstractInteraction interaction)
        {
            string result = interaction.GetType().ToString() + ';';

            if (serializationDict.ContainsKey(interaction.GetType()))
            {
                result += serializationDict[interaction.GetType()](interaction);
            }
            else
            {
                throw new NonSerializableException();
            }

            return result;
        }

        internal static AbstractInteraction Deserialize(string interaction)
        {
            AbstractInteraction result = null;

            string[] tokens = interaction.Split(new char[] { ';' }, 2, StringSplitOptions.None);
            Type type = Type.GetType(tokens[0]);

            if (serializationDict.ContainsKey(type))
            {
                result = deserializationDict[type](tokens[1]);
            }
            else
            {
                throw new NonDeserializableException();
            }

            return result;
        }
    }

}
