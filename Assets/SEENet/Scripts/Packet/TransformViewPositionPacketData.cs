using System;
using UnityEngine;

namespace SEE.Net.Internal
{

    public class TransformViewPositionPacketData : PacketData
    {
        public static readonly string PACKET_NAME = "TransformViewPosition";

        public TransformView transformView;
        public Vector3 position;
        public DateTime updateTime; // TODO: will this be needed ever? also in TransformViewRotationPacketData and TransformViewScalePacketData

        public TransformViewPositionPacketData(TransformView transformView, Vector3 position, DateTime updateTime)
        {
            this.transformView = transformView;
            this.position = position;
            this.updateTime = updateTime;
        }

        public override string Serialize()
        {
            return Serialize(new object[]
            {
                transformView.viewContainer.id,
                transformView.viewContainer.GetIndexOf(transformView),
                position,
                updateTime
            });
        }
        public static TransformViewPositionPacketData Deserialize(string data)
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
            return new TransformViewPositionPacketData(
                transformView,
                DeserializeVector3(d, out d),
                DeserializeDateTime(d, out d)
            );
        }
    }

}
