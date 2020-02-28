using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net.Internal
{

    public struct BuildingData
    {
        public int id;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Color color;
    }

    public class BuildingsPacketData : PacketData
    {
        public static readonly string PACKET_NAME = "Buildings";

        public BuildingData[] buildingData;

        public BuildingsPacketData(GameObject[] buildings)
        {
            Assert.IsNotNull(buildings);
            buildingData = new BuildingData[buildings.Length];
            for (int i = 0; i < buildings.Length; i++)
            {
                buildingData[i].id = buildings[i].GetInstanceID();
                buildingData[i].position = buildings[i].transform.position;
                buildingData[i].rotation = buildings[i].transform.rotation;
                buildingData[i].scale = buildings[i].transform.lossyScale;
                buildingData[i].color = buildings[i].GetComponent<MeshRenderer>().material.color;
            }
        }
        private BuildingsPacketData(BuildingData[] buildingData)
        {
            Assert.IsNotNull(buildingData);
            this.buildingData = buildingData;
        }

        public override string Serialize()
        {
            int buildingDataFieldCount = typeof(BuildingData).GetFields().Length;
            int objectCount = 1 + buildingDataFieldCount * buildingData.Length;
            object[] data = new object[objectCount];

            data[0] = buildingData.Length;

            for (int i = 0; i < buildingData.Length; i++)
            {
                int offset = 0;
                data[1 + buildingDataFieldCount * i + offset++] = buildingData[i].id;
                data[1 + buildingDataFieldCount * i + offset++] = buildingData[i].position;
                data[1 + buildingDataFieldCount * i + offset++] = buildingData[i].rotation;
                data[1 + buildingDataFieldCount * i + offset++] = buildingData[i].scale;
                data[1 + buildingDataFieldCount * i + offset++] = buildingData[i].color;
            }

            return Serialize(data);
        }
        public static BuildingsPacketData Deserialize(string data)
        {
            int buildingCount = DeserializeInt(data, out string d);
            BuildingData[] buildingData = new BuildingData[buildingCount];
            for (int i = 0; i < buildingCount; i++)
            {
                buildingData[i].id = DeserializeInt(d, out d);
                buildingData[i].position = DeserializeVector3(d, out d);
                buildingData[i].rotation = DeserializeQuaternion(d, out d);
                buildingData[i].scale = DeserializeVector3(d, out d);
                buildingData[i].color = DeserializeColor(d, out d);
            }
            return new BuildingsPacketData(buildingData);
        }
    }

}
