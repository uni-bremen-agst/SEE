using SEE.Layout;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net.Internal
{

    public class CityEdgePacket : Packet
    {
        public static readonly string PACKET_TYPE = "CityEdge";
        public static readonly int FIELD_COUNT = 6;

        public int id;
        public Vector3[] positions;
        public float startWidth;
        public float endWidth;
        public Color startColor;
        public Color endColor;

        public CityEdgePacket(GameObject edge) : base(PACKET_TYPE)
        {
            Assert.IsNotNull(edge);
            LineRenderer lineRenderer = edge.GetComponent<LineRenderer>();
            Assert.IsNotNull(lineRenderer);
            Material lineRendererMaterial = lineRenderer.material;
            Assert.IsNotNull(lineRendererMaterial);

            id = edge.GetInstanceID();
            positions = new Vector3[lineRenderer.positionCount];
            lineRenderer.GetPositions(positions);
            startWidth = lineRenderer.startWidth;
            endWidth = lineRenderer.endWidth;
            startColor = lineRenderer.startColor;
            endColor = lineRenderer.endColor;
        }
        private CityEdgePacket() : base(PACKET_TYPE)
        {
        }

        public override string Serialize()
        {
            int objectCount = (FIELD_COUNT - 1) + (1 + positions.Length);
            object[] objects = new object[objectCount];

            int offset = 0;
            objects[offset++] = id;
            objects[offset++] = positions.Length;
            for (int i = 0; i < positions.Length; i++)
            {
                objects[offset++] = positions[i];
            }
            objects[offset++] = startWidth;
            objects[offset++] = endWidth;
            objects[offset++] = startColor;
            objects[offset++] = endColor;

            return Serialize(objects);
        }
        public static CityEdgePacket Deserialize(string data)
        {
            CityEdgePacket packet = new CityEdgePacket();

            packet.id = DeserializeInt(data, out string d);
            int positionCount = DeserializeInt(d, out d);
            packet.positions = new Vector3[positionCount];
            for (int i = 0; i < positionCount; i++)
            {
                packet.positions[i] = DeserializeVector3(d, out d);
            }
            packet.startWidth = DeserializeFloat(d, out d);
            packet.endWidth = DeserializeFloat(d, out d);
            packet.startColor = DeserializeColor(d, out d);
            packet.endColor = DeserializeColor(d, out d);

            return packet;
        }
    }

}
