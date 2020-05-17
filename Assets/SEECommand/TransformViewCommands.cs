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

        protected override void ExecuteOnServer()
        {
        }

        protected override void ExecuteOnClient()
        {
            TransformView transformView = TransformViewCommandHelper.AcquireTransformView(id, index);
            if (transformView)
            {
                transformView.SetNextPosition(position);
            }
        }

        protected override void UndoOnServer()
        {
        }

        protected override void UndoOnClient()
        {
        }

        protected override void RedoOnServer()
        {
        }

        protected override void RedoOnClient()
        {
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

        protected override void ExecuteOnServer()
        {
        }

        protected override void ExecuteOnClient()
        {
            TransformView transformView = TransformViewCommandHelper.AcquireTransformView(id, index);
            if (transformView)
            {
                transformView.SetNextRotation(rotation);
            }
        }

        protected override void UndoOnServer()
        {
        }

        protected override void UndoOnClient()
        {
        }

        protected override void RedoOnServer()
        {
        }

        protected override void RedoOnClient()
        {
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

        protected override void ExecuteOnServer()
        {
        }

        protected override void ExecuteOnClient()
        {
            TransformView transformView = TransformViewCommandHelper.AcquireTransformView(id, index);
            if (transformView)
            {
                transformView.SetNextScale(scale);
            }
        }

        protected override void UndoOnServer()
        {
        }

        protected override void UndoOnClient()
        {
        }

        protected override void RedoOnServer()
        {
        }

        protected override void RedoOnClient()
        {
        }
    }

}
