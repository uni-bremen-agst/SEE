using UnityEngine;

namespace SEE.Net
{
    /// <summary>
    /// Supplies utility functionality for transform view actions.
    /// </summary>
    internal static class TransformViewActionHelper
    {
        /// <summary>
        /// Finds the transform view of given index in view container of given ID.
        /// </summary>
        /// <param name="id">The ID of the view container.</param>
        /// <param name="index">The index of the transform view in the view container.
        /// </param>
        /// <returns>The acquired transform view or <code>null</code>, if none was found.
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
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractAction"/> before modifying this class!
    ///
    /// Synchronizes positions between Clients.
    /// </summary>
    public class TransformViewPositionAction : AbstractAction
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
        /// Constructs an action for given transform view and position.
        /// </summary>
        /// <param name="transformView">The transform view to synchronize.</param>
        /// <param name="position">The new position.</param>
        public TransformViewPositionAction(TransformView transformView, Vector3 position)
        {
            viewContainerID = transformView.viewContainer.id;
            viewIndex = transformView.viewContainer.GetIndexOf(transformView);
            this.position = position;
        }

        protected override void ExecuteOnServer()
        {
            // intentionally left blank
        }

        /// <summary>
        /// Sets the <see cref="position"/> in transform view of <see cref="viewIndex"/>
        /// in view container with <see cref="viewContainerID"/>.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            TransformViewActionHelper.AcquireTransformView(viewContainerID, viewIndex)?.SetNextPosition(position);
        }
    }

    /// <summary>
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractAction"/> before modifying this class!
    ///
    /// Synchronizes rotations between Clients.
    /// </summary>
    public class TransformViewRotationAction : AbstractAction
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
        /// Constructs an action for given transform view and rotation.
        /// </summary>
        /// <param name="transformView">The transform view to synchronize.</param>
        /// <param name="rotation">The new rotation.</param>
        public TransformViewRotationAction(TransformView transformView, Quaternion rotation)
        {
            viewContainerID = transformView.viewContainer.id;
            viewIndex = transformView.viewContainer.GetIndexOf(transformView);
            this.rotation = rotation;
        }

        protected override void ExecuteOnServer()
        {
            // intentionally left blank
        }

        /// <summary>
        /// Sets the <see cref="rotation"/> in transform view of <see cref="viewIndex"/>
        /// in view container with <see cref="viewContainerID"/>.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            TransformViewActionHelper.AcquireTransformView(viewContainerID, viewIndex)?.SetNextRotation(rotation);
        }
    }

    /// <summary>
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractAction"/> before modifying this class!
    ///
    /// Synchronizes cale between Clients.
    /// </summary>
    public class TransformViewScaleAction : AbstractAction
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
        /// Constructs an action for given transform view and scale.
        /// </summary>
        /// <param name="transformView">The transform view to synchronize.</param>
        /// <param name="scale">The new scale.</param>
        public TransformViewScaleAction(TransformView transformView, Vector3 scale)
        {
            viewContainerID = transformView.viewContainer.id;
            viewIndex = transformView.viewContainer.GetIndexOf(transformView);
            this.scale = scale;
        }

        protected override void ExecuteOnServer()
        {
            // intentionally left blank
        }

        /// <summary>
        /// Sets the <see cref="scale"/> in transform view of <see cref="viewIndex"/>
        /// in view container with <see cref="viewContainerID"/>.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            TransformViewActionHelper.AcquireTransformView(viewContainerID, viewIndex)?.SetNextScale(scale);
        }
    }
}
