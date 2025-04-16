using SEE.UI.Menu;
using SEE.Utils.History;
using System.Collections.Generic;

namespace SEE.Controls.Actions.Table
{
    /// <summary>
    ///
    /// </summary>
    public class ModifyTableAction : AbstractPlayerAction
    {
        /// <summary>
        /// The menu for this action.
        /// </summary>
        private ModifyTableMenu menu;

        /// <summary>
        /// Enables the table menu.
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            menu = new();
        }

        /// <summary>
        /// Destroys the table menu.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            menu.Destroy();
        }

        public override bool Update()
        {
            return false;
        }

        /// <summary>
        /// Reverts this action, i.e., TODO.
        /// </summary>
        public override void Undo()
        {
            base.Undo();

        }

        /// <summary>
        /// Repeats this action, i.e., TODO.
        /// </summary>
        public override void Redo()
        {
            base.Redo();

        }

        /// <summary>
        /// A new instance of <see cref="ModifyTableAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="ModifyTableAction"/></returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new ModifyTableAction();
        }

        /// <summary>
        /// A new instance of <see cref="ModifyTableAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="ModifyTableAction"/></returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.ModifyTable"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.ModifyTable;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// </summary>
        /// <returns>The object id of the changed object.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new() {  };
        }

    }
}
