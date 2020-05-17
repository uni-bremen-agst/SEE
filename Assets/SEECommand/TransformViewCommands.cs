using SEE.Net;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Command
{

    internal static class TransformViewCommandHelper
    {
        internal static TransformView AcquireTransformView(int id, int index)
        {
            TransformView result = null;

            ViewContainer viewContainer = ViewContainer.GetViewContainerByID(id);
            if (viewContainer && !viewContainer.IsOwner())
            {
                TransformView transformView = (TransformView)viewContainer.GetViewByIndex(index);
                if (transformView)
                {
                    result = transformView;
                }
            }

            return result;
        }
    }

    public class TransformViewPositionCommand : AbstractCommand
    {
        public int id;
        public int index;
        public Vector3 position;

        public TransformViewPositionCommand(TransformView transformView, Vector3 position) : base(false)
        {
            id = transformView.viewContainer.id;
            index = transformView.viewContainer.GetIndexOf(transformView);
            this.position = position;
        }

        internal override void ExecuteOnServer()
        {
        }

        internal override KeyValuePair<GameObject[], GameObject[]> ExecuteOnClient()
        {
            KeyValuePair<GameObject[], GameObject[]> result;

            TransformView transformView = TransformViewCommandHelper.AcquireTransformView(id, index);
            if (transformView)
            {
                result = new KeyValuePair<GameObject[], GameObject[]>(new GameObject[] {transformView.gameObject }, new GameObject[] { transformView.gameObject });
                transformView.SetNextPosition(position);
            }

            return result;
        }
    }

    public class TransformViewRotationCommand : AbstractCommand
    {
        public int id;
        public int index;
        public Quaternion rotation;

        public TransformViewRotationCommand(TransformView transformView, Quaternion rotation) : base(false)
        {
            id = transformView.viewContainer.id;
            index = transformView.viewContainer.GetIndexOf(transformView);
            this.rotation = rotation;
        }

        internal override void ExecuteOnServer()
        {
        }

        internal override KeyValuePair<GameObject[], GameObject[]> ExecuteOnClient()
        {
            KeyValuePair<GameObject[], GameObject[]> result;

            TransformView transformView = TransformViewCommandHelper.AcquireTransformView(id, index);
            if (transformView)
            {
                result = new KeyValuePair<GameObject[], GameObject[]>(new GameObject[] {transformView.gameObject }, new GameObject[] { transformView.gameObject });
                transformView.SetNextRotation(rotation);
            }

            return result;
        }
    }

    public class TransformViewScaleCommand : AbstractCommand
    {
        public int id;
        public int index;
        public Vector3 scale;

        public TransformViewScaleCommand(TransformView transformView, Vector3 scale) : base(false)
        {
            id = transformView.viewContainer.id;
            index = transformView.viewContainer.GetIndexOf(transformView);
            this.scale = scale;
        }

        internal override void ExecuteOnServer()
        {
        }

        internal override KeyValuePair<GameObject[], GameObject[]> ExecuteOnClient()
        {
            KeyValuePair<GameObject[], GameObject[]> result;

            TransformView transformView = TransformViewCommandHelper.AcquireTransformView(id, index);
            if (transformView)
            {
                result = new KeyValuePair<GameObject[], GameObject[]>(new GameObject[] {transformView.gameObject }, new GameObject[] { transformView.gameObject });
                transformView.SetNextScale(scale);
            }

            return result;
        }
    }

}
