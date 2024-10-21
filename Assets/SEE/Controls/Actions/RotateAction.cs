using RTG;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils.History;
using SEE.XR;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// An action to rotate nodes.
    /// </summary>
    internal class RotateAction : NodeManipulationAction<Quaternion>
    {
        #region Constructors

        /// <summary>
        /// This constructor will be used when this kind of action is to be
        /// continued with a new instance where <paramref name="gameNodeToBeContinuedWith"/>
        /// was selected already.
        /// </summary>
        /// <param name="gameNodeToBeContinuedWith">the game node selected already</param>
        private RotateAction(GameObject gameNodeToBeContinuedWith) : base()
        {
            Initialize();
            StartAction(gameNodeToBeContinuedWith);
        }

        /// <summary>
        /// This constructor will be used if no game node was selected in a previous
        /// instance of this type of action.
        /// </summary>
        private RotateAction() : base()
        {
            Initialize();
        }

        /// <summary>
        /// Initializes the newly created instance by setting <see cref="currentState"/>
        /// and <see cref="gizmo"/>.
        /// </summary>
        private void Initialize()
        {
            CurrentState = IReversibleAction.Progress.NoEffect;
            if (SceneSettings.InputType == PlayerInputType.DesktopPlayer)
            {
                UsedGizmo = new RotateGizmo();
            }
        }

        #endregion Constructors

        #region ReversibleAction Overrides

        /// <summary>
        /// Returns a new instance of <see cref="RotateAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new RotateAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="RotateAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override IReversibleAction NewInstance()
        {
            if (GameNodeToBeContinuedInNextAction)
            {
                return new RotateAction(GameNodeToBeContinuedInNextAction);
            }
            else
            {
                return CreateReversibleAction();
            }
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Rotate"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Rotate;
        }

        #endregion ReversibleAction Overrides

        #region Memento

        /// <summary>
        /// A memento of the rotation of <see cref="gameNodeSelected"/> before or after,
        /// respectively, it was rotated.
        /// </summary>
        private class RotateMemento : Memento<Quaternion>
        {
            /// <summary>
            /// Constructor taking a snapshot of the world-space rotation of <paramref name="gameObject"/>.
            /// </summary>
            /// <param name="gameObject">object whose rotation is to be captured</param>
            public RotateMemento(GameObject gameObject) : base(gameObject)
            {
                InitialState = gameObject.transform.rotation;
            }

            /// <summary>
            /// Should never be used.
            /// </summary>
            private RotateMemento() : base(null)
            {
                throw new System.NotImplementedException();
            }

            /// <summary>
            /// Rotates the game object by the given world-space <paramref name="rotation"/>.
            /// </summary>
            /// <param name="rotation">world-space rotation</param>
            protected override void Transform(Quaternion rotation)
            {
                base.Transform(rotation);
                NodeOperator.RotateTo(rotation);
            }

            /// <summary>
            /// Broadcasts the <paramref name="rotation"/> to all clients.
            /// </summary>
            /// <param name="rotation">rotation to be broadcast</param>
            protected override void BroadcastState(Quaternion rotation)
            {
                new RotateNodeNetAction(NodeOperator.name, rotation).Execute();
            }
        }

        #endregion Memento

        #region NodeManipulationAction Overrides

        protected override void FinalizeAction()
        {
            base.FinalizeAction();
            XRSEEActions.RotateObject = null;
            XRSEEActions.Selected = false;
            XRSEEActions.SelectedFlag = false;
            GameNodeMemento.Finalize(GameNodeSelected.transform.rotation);
        }

        /// <summary>
        /// Yields true if the object to be manipulated has had a change.
        /// Precondition: the object to be manipulated is not null.
        /// </summary>
        /// <returns>true if the object to be manipulated has had a change</returns>
        protected override bool HasChanges()
        {
            return GameNodeSelected.transform.rotation != GameNodeMemento.InitialState;
        }

        protected override Memento<Quaternion> CreateMemento(GameObject gameNode)
        {
            return new RotateMemento(gameNode);
        }

        #endregion NodeManipulationAction Overrides

        #region Gizmo

        /// <summary>
        /// Manages the gizmo to rotate the selected game node.
        /// </summary>
        private class RotateGizmo : Gizmo
        {
            /// <summary>
            /// Constructor setting up <see cref="objectTransformGizmo"/>.
            /// </summary>
            public RotateGizmo()
            {
                ObjectTransformationGizmo = RTGizmosEngine.Get.CreateObjectRotationGizmo();
            }
        }
        #endregion
    }
}
