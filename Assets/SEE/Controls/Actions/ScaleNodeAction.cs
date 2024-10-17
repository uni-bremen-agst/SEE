using SEE.Net.Actions;
using SEE.Game.Operator;
using RTG;
using UnityEngine;
using SEE.Utils.History;
using SEE.GO;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to scale a node.
    /// </summary>
    internal class ScaleNodeAction : NodeManipulationAction<Vector3>
    {
        #region Constructors

        /// <summary>
        /// This constructor will be used when this kind of action is to be
        /// continued with a new instance where <paramref name="gameNodeToBeContinuedWith"/>
        /// was selected already.
        /// </summary>
        /// <param name="gameNodeToBeContinuedWith">the game node selected already</param>
        private ScaleNodeAction(GameObject gameNodeToBeContinuedWith) : base()
        {
            Initialize();
            StartAction(gameNodeToBeContinuedWith);
        }

        /// <summary>
        /// This constructor will be used if no game node was selected in a previous
        /// instance of this type of action.
        /// </summary>
        private ScaleNodeAction() : base()
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
            UsedGizmo = new ScaleGizmo();
        }

        #endregion Constructors

        #region ReversibleAction Overrides

        /// <summary>
        /// Returns a new instance of <see cref="ScaleNodeAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new ScaleNodeAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="ScaleNodeAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override IReversibleAction NewInstance()
        {
            if (GameNodeToBeContinuedInNextAction)
            {
                return new ScaleNodeAction(GameNodeToBeContinuedInNextAction);
            }
            else
            {
                return CreateReversibleAction();
            }
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.ScaleNode"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.ScaleNode;
        }

        #endregion ReversibleAction Overrides

        #region Memento

        /// <summary>
        /// A memento of the scale of <see cref="gameNodeSelected"/> before or after, respectively,
        /// it was scaled.
        /// </summary>
        private class ScaleMemento : Memento<Vector3>
        {
            /// <summary>
            /// Constructor taking a snapshot of the local scale of <paramref name="gameObject"/>.
            /// </summary>
            /// <param name="gameObject">object whose local scale is to be captured</param>
            public ScaleMemento(GameObject gameObject) : base(gameObject)
            {
                InitialState = gameObject.transform.localScale;
            }

            /// <summary>
            /// Should never be used.
            /// </summary>
            private ScaleMemento() : base(null)
            {
                throw new System.NotImplementedException();
            }

            protected override void Transform(Vector3 localScale)
            {
                base.Transform(localScale);
                NodeOperator.ScaleTo(localScale);
            }

            /// <summary>
            /// Broadcasts the <paramref name="localScale"/> to all clients.
            /// </summary>
            /// <param name="localScale">local scale to be broadcast</param>
            protected override void BroadcastState(Vector3 localScale)
            {
                new ScaleNodeNetAction(NodeOperator.name, localScale).Execute();
            }
        }

        #endregion Memento

        #region NodeManipulationAction Overrides

        protected override void FinalizeAction()
        {
            base.FinalizeAction();
            GameNodeMemento.Finalize(GameNodeSelected.transform.localScale);
        }

        /// <summary>
        /// Yields true if the object to be manipulated has had a change.
        /// Precondition: the object to be manipulated is not null.
        /// </summary>
        /// <returns>true if the object to be manipulated has had a change</returns>
        protected override bool HasChanges()
        {
            return GameNodeSelected.transform.localScale != GameNodeMemento.InitialState;
        }

        protected override Memento<Vector3> CreateMemento(GameObject gameNode)
        {
            return new ScaleMemento(gameNode);
        }

        #endregion NodeManipulationAction Overrides

        #region Gizmo

        /// <summary>
        /// Manages the gizmo to scale the selected game node.
        /// </summary>
        private class ScaleGizmo : Gizmo
        {
            /// <summary>
            /// Constructor setting up <see cref="objectTransformGizmo"/>.
            /// </summary>
            public ScaleGizmo()
            {
                if (SceneSettings.InputType == PlayerInputType.DesktopPlayer)
                {
                    ObjectTransformationGizmo = RTGizmosEngine.Get.CreateObjectScaleGizmo();
                }
            }
        }
        #endregion
    }
}
