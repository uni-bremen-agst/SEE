using SEE.DataModel;
using SEE.GO;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE
{

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

        private static readonly Dictionary<Type, Func<Interaction, string>> serializationDict = new Dictionary<Type, Func<Interaction, string>>()
        {
            { typeof(DeleteBuildingInteraction), (i) => DeleteBuildingInteractionSerializer.Serialize((DeleteBuildingInteraction) i) }
        };
        
        private static readonly Dictionary<Type, Func<string, Interaction>> deserializationDict = new Dictionary<Type, Func<string, Interaction>>()
        {
            { typeof(DeleteBuildingInteraction), (s) => DeleteBuildingInteractionSerializer.Deserialize(s) }
        };

        internal static string Serialize(Interaction interaction)
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

        internal static Interaction Deserialize(string interaction)
        {
            Interaction result = null;

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

    internal static class DeleteBuildingInteractionSerializer
    {
        private struct SerializedObject
        {
            public string id;
        }

        internal static string Serialize(DeleteBuildingInteraction interaction)
        {
            NodeRef nodeRef = interaction.building.GetComponent<NodeRef>();
            SerializedObject serializedObject = new SerializedObject() { id = nodeRef.node.LinkName };
            string result = JsonUtility.ToJson(serializedObject);
            return result;
        }

        internal static DeleteBuildingInteraction Deserialize(string interaction)
        {
            SerializedObject serializedObject = JsonUtility.FromJson<SerializedObject>(interaction);
            GameObject[] buildings = GameObject.FindGameObjectsWithTag(Tags.Building);
            DeleteBuildingInteraction result = null;
            foreach (GameObject building in buildings)
            {
                NodeRef nodeRef = building.GetComponent<NodeRef>();
                if (nodeRef != null && nodeRef.node != null && nodeRef.node.LinkName == serializedObject.id)
                {
                    result = new DeleteBuildingInteraction(building);
                    break;
                }
            }
            return result;
        }
    }
}
