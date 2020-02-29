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

        protected override bool HandleBuildingPacket(PacketHeader packetHeader, Connection connection, string data)
        {
            CityBuildingPacket buildingPacket = CityBuildingPacket.Deserialize(data);

            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.AddComponent<BuildingIdentifier>().id = buildingPacket.id;
            building.transform.position = buildingPacket.position;
            building.transform.rotation = buildingPacket.rotation;
            building.transform.localScale = buildingPacket.scale;
            building.GetComponent<MeshRenderer>().material.color = buildingPacket.color;

            return true;
        }
        protected override bool HandleGXLPacket(PacketHeader packetHeader, Connection connection, string data)
        {
            GXLPacket packet = GXLPacket.Deserialize(data);
            // TODO: build city
            return true;
        }
        protected override bool HandleInstantiatePacket(PacketHeader packetHeader, Connection connection, string data)
        {
            InstantiatePacket packet = InstantiatePacket.Deserialize(data);
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
        protected override bool HandleTransformViewPositionPacket(PacketHeader packetHeader, Connection connection, string data)
        {
            TransformViewPositionPacket packet = TransformViewPositionPacket.Deserialize(data);
            packet?.transformView?.SetNextPosition(packet.updateTime, packet.position);
            return true;
        }
        protected override bool HandleTransformViewRotationPacket(PacketHeader packetHeader, Connection connection, string data)
        {
            TransformViewRotationPacket packet = TransformViewRotationPacket.Deserialize(data);
            packet?.transformView?.SetNextRotation(packet.updateTime, packet.rotation);
            return true;
        }
        protected override bool HandleTransformViewScalePacket(PacketHeader packetHeader, Connection connection, string data)
        {
            TransformViewScalePacket packet = TransformViewScalePacket.Deserialize(data);
            packet?.transformView?.SetNextScale(packet.updateTime, packet.scale);
            return true;
        }
    }

}
