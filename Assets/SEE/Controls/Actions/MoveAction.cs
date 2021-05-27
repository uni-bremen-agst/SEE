using SEE.Utils;
using System.Collections.Generic;

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

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Move"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.Move;
        }

        /// <summary>
        /// Returns the list of IDs of all game objects changed by this action.
        /// <see cref="ReversibleAction.GetChangedObjects"/>
        /// </summary>
        /// <returns>empty list because this action does not change anything</returns>
        public override List<string> GetChangedObjects()
        {
            return new List<string>();
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
