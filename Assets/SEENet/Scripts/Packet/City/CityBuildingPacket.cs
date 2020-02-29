using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net.Internal
{

    public class CityBuildingPacket : Packet
    {
        public static readonly string PACKET_TYPE = "CityBuilding";

        public int id;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Color color;

        public CityBuildingPacket(GameObject building) : base(PACKET_TYPE)
        {
            Assert.IsNotNull(building);
            id = building.GetInstanceID();
            position = building.transform.position;
            rotation = building.transform.rotation;
            scale = building.transform.lossyScale;
            color = building.GetComponent<MeshRenderer>().material.color;
        }
        private CityBuildingPacket() : base(PACKET_TYPE)
        {
        }

        public override string Serialize()
        {
            return Serialize(new object[]
            {
                id,
                position,
                rotation,
                scale,
                color
            });
        }
        public static CityBuildingPacket Deserialize(string data)
        {
            return new CityBuildingPacket()
            {
                id = DeserializeInt(data, out string d),
                position = DeserializeVector3(d, out d),
                rotation = DeserializeQuaternion(d, out d),
                scale = DeserializeVector3(d, out d),
                color = DeserializeColor(d, out d)
            };
        }
    }

}
