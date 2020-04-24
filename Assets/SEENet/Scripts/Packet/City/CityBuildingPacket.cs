using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net.Internal
{

    internal class CityBuildingPacket : Packet
    {
        internal static readonly string PACKET_TYPE = "CityBuilding";

        internal int id;
        internal Vector3 position;
        internal Quaternion rotation;
        internal Vector3 scale;
        internal Color color;

        internal CityBuildingPacket(GameObject building) : base(PACKET_TYPE)
        {
            Assert.IsNotNull(building);
            Assert.IsNotNull(building.GetComponent<MeshRenderer>());

            id = building.GetInstanceID();
            position = building.transform.position;
            rotation = building.transform.rotation;
            scale = building.transform.lossyScale;
            color = building.GetComponent<MeshRenderer>().material.color;
        }

        private CityBuildingPacket() : base(PACKET_TYPE) { }

        internal override string Serialize()
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

        internal static CityBuildingPacket Deserialize(string data)
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
