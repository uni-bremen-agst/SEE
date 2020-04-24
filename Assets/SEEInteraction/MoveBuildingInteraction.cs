using SEE.DataModel;
using SEE.GO;
using UnityEngine;

namespace SEE.Interact
{

    public class MoveBuildingInteraction : AbstractInteraction
    {
        public readonly GameObject building;
        public readonly Vector3 position;

        public MoveBuildingInteraction(GameObject building, Vector3 position)
        {
            this.building = building;
            this.position = position;
        }

        internal override void ExecuteLocally()
        {
            building.transform.position = position;
        }
    }

    internal static class MoveBuildingInteractionSerializer
    {
        private struct SerializedObject
        {
            public string id;
            public Vector3 position;
        }

        internal static string Serialize(MoveBuildingInteraction interaction)
        {
            NodeRef nodeRef = interaction.building.GetComponent<NodeRef>();
            SerializedObject serializedObject = new SerializedObject() { id = nodeRef.node.LinkName, position = interaction.position };
            string result = JsonUtility.ToJson(serializedObject);
            return result;
        }

        internal static MoveBuildingInteraction Deserialize(string interaction)
        {
            SerializedObject serializedObject = JsonUtility.FromJson<SerializedObject>(interaction);
            GameObject[] buildings = GameObject.FindGameObjectsWithTag(Tags.Building);
            MoveBuildingInteraction result = null;
            foreach (GameObject building in buildings)
            {
                NodeRef nodeRef = building.GetComponent<NodeRef>();
                if (nodeRef != null && nodeRef.node != null && nodeRef.node.LinkName == serializedObject.id)
                {
                    result = new MoveBuildingInteraction(building, serializedObject.position);
                    break;
                }
            }
            return result;
        }
    }

}