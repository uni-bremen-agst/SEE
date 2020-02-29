using System;
using UnityEngine;

namespace SEE.Net.Internal
{

    public class TransformViewScalePacket : Packet
    {
        public static readonly string PACKET_TYPE = "TransformViewScale";

        public TransformView transformView;
        public Vector3 scale;
        public DateTime updateTime;

        public TransformViewScalePacket(TransformView transformView, Vector3 scale, DateTime updateTime) : base(PACKET_TYPE)
        {
            this.transformView = transformView;
            this.scale = scale;
            this.updateTime = updateTime;
        }

        public override string Serialize()
        {
            return Serialize(new object[]
            {
                transformView.viewContainer.id,
                transformView.viewContainer.GetIndexOf(transformView),
                scale,
                updateTime
            });
        }
        public static TransformViewScalePacket Deserialize(string data)
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
