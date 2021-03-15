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
        /// </summary>RotateAction
        /// <returns>new instance of <see cref="MoveAction"/></returns>
        internal static ReversibleAction CreateReversibleAction()
        {
            return new RotateAction();
        }
    }
}
