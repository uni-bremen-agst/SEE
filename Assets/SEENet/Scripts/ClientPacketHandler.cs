using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using System;
using UnityEngine;

namespace SEE.Net.Internal
{

    public class ClientPacketHandler : PacketHandler
    {
        private static DateTime lastTransformViewPositionPacketDateTime = DateTime.MinValue;
        private static DateTime lastTransformViewRotationPacketDateTime = DateTime.MinValue;
        private static DateTime lastTransformViewScalePacketDateTime = DateTime.MinValue;

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
            if (packet == null || packet.updateTime < lastTransformViewPositionPacketDateTime)
            {
                return false;
            }
            lastTransformViewPositionPacketDateTime = packet.updateTime;
            packet.transformView.SetNextPosition(packet.position);
            return true;
        }
        protected override bool HandleTransformViewRotationPacketData(PacketHeader packetHeader, Connection connection, string data)
        {
            TransformViewRotationPacketData packet = TransformViewRotationPacketData.Deserialize(data);
            if (packet == null || packet.updateTime < lastTransformViewRotationPacketDateTime)
            {
                return false;
            }
            lastTransformViewRotationPacketDateTime = packet.updateTime;
            packet.transformView.SetNextRotation(packet.rotation);
            return true;
        }
        protected override bool HandleTransformViewScalePacketData(PacketHeader packetHeader, Connection connection, string data)
        {
            TransformViewScalePacketData packet = TransformViewScalePacketData.Deserialize(data);
            if (packet == null || packet.updateTime < lastTransformViewScalePacketDateTime)
            {
                return false;
            }
            lastTransformViewScalePacketDateTime = packet.updateTime;
            packet.transformView.SetNextScale(packet.scale);
            return true;
        }
    }

}
