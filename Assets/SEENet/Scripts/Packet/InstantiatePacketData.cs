using System;
using System.Net;
using UnityEngine;

namespace SEE.Net.Internal
{

    public class InstantiatePacket : Packet
    {
        public static readonly string PACKET_TYPE = "Instantiate";

        public string prefabName;
        public IPEndPoint owner;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public int viewID;

        public InstantiatePacket(string prefabName, IPEndPoint owner, int viewID = -1) : base(PACKET_TYPE)
        {
            Initialize(prefabName, owner, Vector3.zero, Quaternion.identity, Vector3.one, viewID);
        }
        public InstantiatePacket(string prefabName, IPEndPoint owner, Vector3 position, Quaternion rotation, Vector3 scale, int viewID = -1) : base(PACKET_TYPE)
        {
            Initialize(prefabName, owner, position, rotation, scale, viewID);
        }
        private void Initialize(string prefabName, IPEndPoint owner, Vector3 position, Quaternion rotation, Vector3 scale, int viewID)
        {
#if UNITY_EDITOR
            if (prefabName == null)
            {
                throw new ArgumentNullException("prefabName");
            }
            GameObject prefab = Resources.Load<GameObject>(prefabName);
            if (!prefab)
            {
                throw new ArgumentException("Prefab of name '" + prefabName + "' does not exist!");
            }
            ViewContainer viewContainer = prefab.GetComponent<ViewContainer>();
            if (!viewContainer)
            {
                throw new MissingComponentException("Prefab of name '" + prefabName + "' must contain a '" + typeof(ViewContainer).ToString() + "' component!");
            }
            if (owner == null || owner.Address == null)
            {
                throw new ArgumentException("Argument is invalid!", "owner");
            }
#endif
            this.prefabName = prefabName;
            this.owner = owner;
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
            this.viewID = viewID;
        }

        public override string Serialize()
        {
            return Serialize(new object[]
            {
                prefabName,
                owner.Address.ToString(),
                owner.Port,
                position,
                rotation,
                scale,
                viewID
            });
        }
        public static InstantiatePacket Deserialize(string data)
        {
            return new InstantiatePacket(
                DeserializeString(data, out string d),
                new IPEndPoint(IPAddress.Parse(DeserializeString(d, out d)), DeserializeInt(d, out d)),
                DeserializeVector3(d, out d),
                DeserializeQuaternion(d, out d),
                DeserializeVector3(d, out d),
                DeserializeInt(d, out d)
            );
        }
    }

}
