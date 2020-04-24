using SEE.DataModel;
using SEE.GO;
using UnityEngine;

namespace SEE.Interact
{

    public class DeleteBuildingInteraction : AbstractInteraction
    {
        public readonly GameObject building;

        public DeleteBuildingInteraction(GameObject building)
        {
            this.building = building;
        }

        internal override void ExecuteLocally()
        {
            Object.Destroy(building);
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
