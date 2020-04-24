using System;
using UnityEngine;

namespace SEE.Net.Internal
{

    internal class TransformViewPositionPacket : Packet
    {
        internal static readonly string PACKET_TYPE = "TransformViewPosition";

        internal TransformView transformView;
        internal Vector3 position;
        internal DateTime updateTime;

        internal TransformViewPositionPacket(TransformView transformView, Vector3 position, DateTime updateTime) : base(PACKET_TYPE)
        {
            this.transformView = transformView;
            this.position = position;
            this.updateTime = updateTime;
        }

        internal override string Serialize()
        {
            return Serialize(new object[]
            {
                transformView.viewContainer.id,
                transformView.viewContainer.GetIndexOf(transformView),
                position,
                updateTime
            });
        }

        internal static TransformViewPositionPacket Deserialize(string data)
        {
            ViewContainer viewContainer = ViewContainer.GetViewContainerByID(DeserializeInt(data, out string d));

            if (viewContainer == null)
            {
                return null;
            }

            TransformView transformView = (TransformView)viewContainer.GetViewByIndex(DeserializeInt(d, out d));

            if (transformView == null)
            {
                return null;
            }

            return new TransformViewPositionPacket(
                transformView,
                DeserializeVector3(d, out d),
                DeserializeDateTime(d, out d)
            );
        }
    }

}
