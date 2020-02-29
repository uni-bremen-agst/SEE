using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net.Internal
{

    public class CityNodePacket : Packet
    {
        public static readonly string PACKET_TYPE = "CityNode";

        public int id;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Color color;

        public CityNodePacket(GameObject node) : base(PACKET_TYPE)
        {
            Assert.IsNotNull(node);
            id = node.GetInstanceID();
            position = node.transform.position;
            rotation = node.transform.rotation;
            scale = node.transform.lossyScale;
            color = node.GetComponent<MeshRenderer>().material.color;
        }
        private CityNodePacket() : base(PACKET_TYPE)
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
        public static CityNodePacket Deserialize(string data)
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
