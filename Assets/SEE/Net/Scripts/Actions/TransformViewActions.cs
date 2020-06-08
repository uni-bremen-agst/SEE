using UnityEngine;

namespace SEE.Net
{

    internal static class TransformViewActionHelper
    {
        internal static TransformView AcquireTransformView(uint id, int index)
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

    public class TransformViewPositionCommand : AbstractAction
    {
        public uint viewContainerID;
        public int viewIndex;
        public Vector3 position;



        public TransformViewPositionCommand(TransformView transformView, Vector3 position) : base(false)
        {
            viewContainerID = transformView.viewContainer.id;
            viewIndex = transformView.viewContainer.GetIndexOf(transformView);
            this.position = position;
        }



        protected override bool ExecuteOnServer()
        {
            return true;
        }

        protected override bool ExecuteOnClient()
        {
            TransformView transformView = TransformViewActionHelper.AcquireTransformView(viewContainerID, viewIndex);
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

    public class TransformViewRotationCommand : AbstractAction
    {
        public uint viewContainerID;
        public int viewIndex;
        public Quaternion rotation;



        public TransformViewRotationCommand(TransformView transformView, Quaternion rotation) : base(false)
        {
            viewContainerID = transformView.viewContainer.id;
            viewIndex = transformView.viewContainer.GetIndexOf(transformView);
            this.rotation = rotation;
        }



        protected override bool ExecuteOnServer()
        {
            return true;
        }

        protected override bool ExecuteOnClient()
        {
            TransformView transformView = TransformViewActionHelper.AcquireTransformView(viewContainerID, viewIndex);
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

    public class TransformViewScaleCommand : AbstractAction
    {
        public uint viewContainerID;
        public int viewIndex;
        public Vector3 scale;



        public TransformViewScaleCommand(TransformView transformView, Vector3 scale) : base(false)
        {
            viewContainerID = transformView.viewContainer.id;
            viewIndex = transformView.viewContainer.GetIndexOf(transformView);
            this.scale = scale;
        }



        protected override bool ExecuteOnServer()
        {
            return true;
        }

        protected override bool ExecuteOnClient()
        {
            TransformView transformView = TransformViewActionHelper.AcquireTransformView(viewContainerID, viewIndex);
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
