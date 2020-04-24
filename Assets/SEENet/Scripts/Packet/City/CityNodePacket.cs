using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net.Internal
{

    internal class CityNodePacket : Packet
    {
        internal static readonly string PACKET_TYPE = "CityNode";

        internal int id;
        internal Vector3 position;
        internal Quaternion rotation;
        internal Vector3 scale;
        internal Color color;

        internal CityNodePacket(GameObject node) : base(PACKET_TYPE)
        {
            Assert.IsNotNull(node);
            Assert.IsNotNull(node.GetComponent<MeshRenderer>());

            id = node.GetInstanceID();
            position = node.transform.position;
            rotation = node.transform.rotation;
            scale = node.transform.lossyScale;
            color = node.GetComponent<MeshRenderer>().material.color;
        }

        private CityNodePacket() : base(PACKET_TYPE) { }

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

        internal static CityNodePacket Deserialize(string data)
        {
            return new CityNodePacket()
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
