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
    }
}
