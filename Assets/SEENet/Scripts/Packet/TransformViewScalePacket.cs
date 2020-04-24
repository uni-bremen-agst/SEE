using System;
using UnityEngine;

namespace SEE.Net.Internal
{

    internal class TransformViewScalePacket : Packet
    {
        internal static readonly string PACKET_TYPE = "TransformViewScale";

        internal TransformView transformView;
        internal Vector3 scale;
        internal DateTime updateTime;

        internal TransformViewScalePacket(TransformView transformView, Vector3 scale, DateTime updateTime) : base(PACKET_TYPE)
        {
            this.transformView = transformView;
            this.scale = scale;
            this.updateTime = updateTime;
        }

        internal override string Serialize()
        {
            return Serialize(new object[]
            {
                transformView.viewContainer.id,
                transformView.viewContainer.GetIndexOf(transformView),
                scale,
                updateTime
            });
        }

        internal static TransformViewScalePacket Deserialize(string data)
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

            return new TransformViewScalePacket(
                transformView,
                DeserializeVector3(d, out d),
                DeserializeDateTime(d, out d)
            );
        }
    }

}
