using SEE.Net;
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

        protected override bool ExecuteOnServer()
        {
            return true;
        }

        protected override bool ExecuteOnClient()
        {
            TransformView transformView = TransformViewCommandHelper.AcquireTransformView(id, index);
            if (transformView)
            {
                transformView.SetNextPosition(position);
                return true;
            }
            return false;
        }

        protected override bool UndoOnServer()
        {
            return false;
        }

        protected override bool UndoOnClient()
        {
            return false;
        }

        protected override bool RedoOnServer()
        {
            return false;
        }

        protected override bool RedoOnClient()
        {
            return false;
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

        protected override bool ExecuteOnServer()
        {
            return true;
        }

        protected override bool ExecuteOnClient()
        {
            TransformView transformView = TransformViewCommandHelper.AcquireTransformView(id, index);
            if (transformView)
            {
                transformView.SetNextRotation(rotation);
                return true;
            }
            return false;
        }

        protected override bool UndoOnServer()
        {
            return false;
        }

        protected override bool UndoOnClient()
        {
            return false;
        }

        protected override bool RedoOnServer()
        {
            return false;
        }

        protected override bool RedoOnClient()
        {
            return false;
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

        protected override bool ExecuteOnServer()
        {
            return true;
        }

        protected override bool ExecuteOnClient()
        {
            TransformView transformView = TransformViewCommandHelper.AcquireTransformView(id, index);
            if (transformView)
            {
                transformView.SetNextScale(scale);
                return true;
            }
            return false;
        }

        protected override bool UndoOnServer()
        {
            return false;
        }

        protected override bool UndoOnClient()
        {
            return false;
        }

        protected override bool RedoOnServer()
        {
            return false;
        }

        protected override bool RedoOnClient()
        {
            return false;
        }
    }

}
