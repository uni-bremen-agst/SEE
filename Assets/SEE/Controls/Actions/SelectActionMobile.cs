using SEE.Controls.Actions;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Basically an empty Action to ensure the player can only select objects.
    /// Select interactions are provided by <see cref="SelectAction"/>
    /// </summary>
    public class SelectActionMobile : AbstractPlayerAction
    {
        /// <summary>
        /// Returns a new instance of <see cref="SelectActionMobile"/>
        /// </summary>
        /// <returns><see cref="ActionStateType.Select"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.Select;
        }

        /// <summary>
        /// Returns all IDs of gameObjects manipulated by this action.
        /// </summary>
        /// <returns>all IDs of gameObjects manipulated by this action</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>();
        }

        /// <summary>
        /// Returns a new instance of <see cref="SelectActionMobile"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// See <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed but there is no action to be completed yet</returns>
        public override bool Update()
        {
            return false;
        }

        /// <summary>
        /// Returns a new instance of <see cref="SelectActionMobile"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new SelectActionMobile();
        }
    }
}
