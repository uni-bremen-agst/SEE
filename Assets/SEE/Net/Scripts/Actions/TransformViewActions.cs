using UnityEngine;

namespace SEE.Net
{

    /// <summary>
    /// Supplies utility functionality for transform view commands.
    /// </summary>
    internal static class TransformViewActionHelper
    {
        /// <summary>
        /// Finds the transform view of given index in view container of given ID.
        /// </summary>
        /// <param name="id">The ID of the view container.</param>
        /// <param name="index">The index of the transform view in the view container.
        /// </param>
        /// <returns>The acquired transform view or <code>null</code>, if non was found.
        /// </returns>
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

    /// <summary>
    /// Synchronizes positions between Clients.
    /// </summary>
    public class TransformViewPositionCommand : AbstractAction
    {
        /// <summary>
        /// The unique ID of the view container containing the transform view.
        /// </summary>
        public uint viewContainerID;

        /// <summary>
        /// The index of the view inside of the view container.
        /// </summary>
        public int viewIndex;

        /// <summary>
        /// The new position to be synchronized.
        /// </summary>
        public Vector3 position;



        /// <summary>
        /// Constructs a command for given transform view and position.
        /// </summary>
        /// <param name="transformView">The transform view to synchronize.</param>
        /// <param name="position">The new position.</param>
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

        /// <summary>
        /// Sets the <see cref="position"/> in transform view of <see cref="viewIndex"/>
        /// in view container with <see cref="viewContainerID"/>.
        /// </summary>
        /// <returns><code>true</code> if transform view exists and position could be updated,
        /// <code>false</code> otherwise.</returns>
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

    /// <summary>
    /// Synchronizes rotations between Clients.
    /// </summary>
    public class TransformViewRotationCommand : AbstractAction
    {
        /// <summary>
        /// The unique ID of the view container containing the transform view.
        /// </summary>
        public uint viewContainerID;

        /// <summary>
        /// The index of the view inside of the view container.
        /// </summary>
        public int viewIndex;

        /// <summary>
        /// The rotation to be synchronized.
        /// </summary>
        public Quaternion rotation;



        /// <summary>
        /// Constructs a command for given transform view and rotation.
        /// </summary>
        /// <param name="transformView">The transform view to synchronize.</param>
        /// <param name="rotation">The new rotation.</param>
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

        /// <summary>
        /// Sets the <see cref="rotation"/> in transform view of <see cref="viewIndex"/>
        /// in view container with <see cref="viewContainerID"/>.
        /// </summary>
        /// <returns><code>true</code> if transform view exists and rotation could be updated,
        /// <code>false</code> otherwise.</returns>
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

    /// <summary>
    /// Synchronizes cale between Clients.
    /// </summary>
    public class TransformViewScaleCommand : AbstractAction
    {
        /// <summary>
        /// The unique ID of the view container containing the transform view.
        /// </summary>
        public uint viewContainerID;

        /// <summary>
        /// The index of the view inside of the view container.
        /// </summary>
        public int viewIndex;

        /// <summary>
        /// The scale to be synchronized.
        /// </summary>
        public Vector3 scale;



        /// <summary>
        /// Constructs a command for given transform view and scale.
        /// </summary>
        /// <param name="transformView">The transform view to synchronize.</param>
        /// <param name="scale">The new scale.</param>
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

        /// <summary>
        /// Sets the <see cref="scale"/> in transform view of <see cref="viewIndex"/>
        /// in view container with <see cref="viewContainerID"/>.
        /// </summary>
        /// <returns><code>true</code> if transform view exists and scale could be updated,
        /// <code>false</code> otherwise.</returns>
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
