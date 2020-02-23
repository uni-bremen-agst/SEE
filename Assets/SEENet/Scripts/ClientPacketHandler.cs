using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using UnityEngine;

namespace SEE.Net.Internal
{

    public class ClientPacketHandler : PacketHandler
    {
        public ClientPacketHandler(string packetTypePrefix) : base(packetTypePrefix)
        {
        }

        protected override bool HandleGXLPacketData(PacketHeader packetHeader, Connection connection, string data)
        {
            GXLPacketData packet = GXLPacketData.Deserialize(data);
            // TODO: build city
            return true;
        }
        protected override bool HandleInstantiatePacketData(PacketHeader packetHeader, Connection connection, string data)
        {
            InstantiatePacketData packet = InstantiatePacketData.Deserialize(data);
            if (packet == null)
            {
                return false;
            }
            GameObject prefab = Resources.Load<GameObject>(packet.prefabName);
            if (!prefab)
            {
                Debug.LogError("Prefab of name '" + packet.prefabName + "' could not be found!");
                return false;
            }
            GameObject obj = UnityEngine.Object.Instantiate(prefab, null, true);
            if (!obj)
            {
                Debug.Log("Object could not be instantiated with prefab '" + prefab + "'!");
                return false;
            }
            obj.GetComponent<ViewContainer>().Initialize(packet.viewID, packet.owner);
            obj.transform.position = packet.position;
            obj.transform.rotation = packet.rotation;
            obj.transform.localScale = packet.scale;
            return true;
        }
        protected override bool HandleTransformViewPositionPacketData(PacketHeader packetHeader, Connection connection, string data)
        {
            TransformViewPositionPacketData packet = TransformViewPositionPacketData.Deserialize(data);
            packet?.transformView?.SetNextPosition(packet.updateTime, packet.position);
            return true;
        }
        protected override bool HandleTransformViewRotationPacketData(PacketHeader packetHeader, Connection connection, string data)
        {
            TransformViewRotationPacketData packet = TransformViewRotationPacketData.Deserialize(data);
            packet?.transformView?.SetNextRotation(packet.updateTime, packet.rotation);
            return true;
        }
        protected override bool HandleTransformViewScalePacketData(PacketHeader packetHeader, Connection connection, string data)
        {
            TransformViewScalePacketData packet = TransformViewScalePacketData.Deserialize(data);
            packet?.transformView?.SetNextScale(packet.updateTime, packet.scale);
            return true;
        }
    }

}
