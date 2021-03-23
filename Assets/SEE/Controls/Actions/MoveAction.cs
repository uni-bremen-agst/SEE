using SEE.Utils;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// An action to move nodes. 
    /// 
    /// Note: This is currently only a placeholder.
    /// FIXME: This class needs to implemented. It must somehow be integrated with NavigationAction.
    /// </summary>
    class MoveAction : AbstractPlayerAction
    {
        /// <summary>
        /// Returns a new instance of <see cref="MoveAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="MoveAction"/></returns>
        internal static ReversibleAction CreateReversibleAction()
        {
            return new MoveAction();
        }

        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.Move;
        }

        /// <summary>
        /// Returns a new instance of <see cref="MoveAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>always false</returns>
        public override bool Update()
        {
            // may continue endlessly
            return false;
        }
    }
}
