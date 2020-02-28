using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net.Internal
{

    public class BuildingPacketData : PacketData
    {
        public static readonly string PACKET_NAME = "Building";

        public BuildingData buildingData;

        public BuildingPacketData(GameObject building)
        {
            Assert.IsNotNull(building);
            buildingData = new BuildingData
            {
                id = building.GetInstanceID(),
                position = building.transform.position,
                rotation = building.transform.rotation,
                scale = building.transform.lossyScale,
                color = building.GetComponent<MeshRenderer>().material.color
            };
        }
        private BuildingPacketData(BuildingData buildingData)
        {
            this.buildingData = buildingData;
        }

        public override string Serialize()
        {
            return Serialize(new object[]
            {
                buildingData.id,
                buildingData.position,
                buildingData.rotation,
                buildingData.scale,
                buildingData.color
            });
        }
        public static BuildingPacketData Deserialize(string data)
        {
            return new BuildingPacketData(new BuildingData
            {
                id = DeserializeInt(data, out string d),
                position = DeserializeVector3(d, out d),
                rotation = DeserializeQuaternion(d, out d),
                scale = DeserializeVector3(d, out d),
                color = DeserializeColor(d, out d)
            });
        }
    }

}
