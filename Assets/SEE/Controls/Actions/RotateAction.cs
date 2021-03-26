using SEE.Utils;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// An action to rotate nodes. 
    /// 
    /// Note: This is currently only a placeholder.
    /// FIXME: This class needs to implemented. It must somehow be integrated with NavigationAction.
    /// </summary>
    class RotateAction : AbstractPlayerAction
    {
        /// <summary>
        /// Returns a new instance of <see cref="RotateAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="RotateAction"/></returns>
        internal static ReversibleAction CreateReversibleAction()
        {
            return new RotateAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Rotate"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.Rotate;
        }

        /// <summary>
        /// Returns a new instance of <see cref="RotateAction"/>.
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
