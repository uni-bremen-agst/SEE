using System;
using UnityEngine;

namespace SEE.Net.Internal
{

    internal class TransformViewRotationPacket : AbstractPacket
    {
        internal static readonly string PACKET_TYPE = "TransformViewRotation";

        internal TransformView transformView;
        internal Quaternion rotation;
        internal DateTime updateTime;

        internal TransformViewRotationPacket(TransformView transformView, Quaternion rotation, DateTime updateTime) : base(PACKET_TYPE)
        {
            this.transformView = transformView;
            this.rotation = rotation;
            this.updateTime = updateTime;
        }

        internal override string Serialize()
        {
            return Serialize(new object[]
            {
                transformView.viewContainer.id,
                transformView.viewContainer.GetIndexOf(transformView),
                rotation,
                updateTime
            });
        }

        internal static TransformViewRotationPacket Deserialize(string data)
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

            return new TransformViewRotationPacket(
                transformView,
                DeserializeQuaternion(d, out d),
                DeserializeDateTime(d, out d)
            );
        }
    }

}
