using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using SEE.Net.Actions;
using SEE.Game.Operator;
using RTG;

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
            currentState = ReversibleAction.Progress.NoEffect;
            gizmo = new ScaleGizmo();
        }

        #endregion Constructors

        #region ReversibleAction Overrides

        /// <summary>
        /// Returns a new instance of <see cref="ScaleNodeAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new ScaleNodeAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="ScaleNodeAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance()
        {
            if (gameNodeToBeContinuedInNextAction)
            {
                return new ScaleNodeAction(gameNodeToBeContinuedInNextAction);
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

        /// <summary>
        /// Returns all IDs of gameObjects manipulated by this action.
        /// </summary>
        /// <returns>all IDs of gameObjects manipulated by this action</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return gameNodeSelected == null ? new HashSet<string>() : new HashSet<string>()
            {
                gameNodeSelected.name
            };
        }

        #endregion ReversibleAction Overrides

        #region Memento

        /// <summary>
        /// A memento of the position and scale of <see cref="gameNodeSelected"/> before
        /// or after, respectively, it was scaled.
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
                // FIXME: If a duration > 0 is used, the node operator does not scale the object.
                nodeOperator.ScaleTo(localScale, 0);
            }

            /// <summary>
            /// Broadcasts the <paramref name="localScale"/> to call clients.
            /// </summary>
            /// <param name="localScale">local scale to be broadcast</param>
            protected override void BroadcastState(Vector3 localScale)
            {
                new ScaleNodeNetAction(nodeOperator.name, localScale, AbstractOperator.DefaultAnimationDuration).Execute();
            }
        }

        #endregion Memento

        #region NodeManipulationAction Overrides

        protected override void FinalizeAction()
        {
            base.FinalizeAction();
            memento.Finalize(gameNodeSelected.transform.localScale);
        }

        /// <summary>
        /// Yields true if the object to be manipulated has had a change.
        /// Precondition: the object to be manipulated is not null.
        /// </summary>
        /// <returns>true if the object to be manipulated has had a change</returns>
        protected override bool HasChanges()
        {
            return gameNodeSelected.transform.localScale != memento.InitialState;
        }

        protected override Memento<Vector3> CreateMemento(GameObject gameNode)
        {
            return new ScaleMemento(gameNode);
        }

        #endregion NodeManipulationAction Overrides

        #region Gizmo

        /// <summary>
        /// Manages the gizmo to manipulate the selected game node.
        /// </summary>
        private class ScaleGizmo : Gizmo
        {
            public ScaleGizmo()
            {
                objectTransformationGizmo = RTGizmosEngine.Get.CreateObjectScaleGizmo();
            }
        }
        #endregion
    }
}
