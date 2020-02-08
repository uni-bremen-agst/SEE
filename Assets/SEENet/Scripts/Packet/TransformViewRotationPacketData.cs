using System;
using UnityEngine;

namespace SEE.Net.Internal
{

    public class TransformViewRotationPacketData : PacketData
    {
        public static readonly string PACKET_NAME = "TransformViewRotation";

        public TransformView transformView;
        public Quaternion rotation;
        public DateTime updateTime;

        public TransformViewRotationPacketData(TransformView transformView, Quaternion rotation, DateTime updateTime)
        {
            this.transformView = transformView;
            this.rotation = rotation;
            this.updateTime = updateTime;
        }

        public override string Serialize()
        {
            return Serialize(new object[]
            {
                transformView.viewContainer.id,
                transformView.viewContainer.GetIndexOf(transformView),
                rotation,
                updateTime
            });
        }
        public static TransformViewRotationPacketData Deserialize(string data)
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
            return new TransformViewRotationPacketData(
                transformView,
                DeserializeQuaternion(d, out d),
                DeserializeDateTime(d, out d)
            );
        }
    }

}
