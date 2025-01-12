using SEE.Utils.History;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions
{

    /// <summary>
    /// The type of a state-based action.
    /// </summary>
    public class ActionStateType : AbstractActionStateType
    {
        // A static dictionary to hold all registered ActionStateType instances by name
        private static readonly Dictionary<string, ActionStateType> actionStateTypes = new Dictionary<string, ActionStateType>();

        /// <summary>
        /// Delegate to be called to create a new instance of this kind of action.
        /// May be null if none needs to be created (in which case this delegate will not be called).
        /// </summary>
        public CreateReversibleAction CreateReversible { get; }

        /// <summary>
        /// Constructor for ActionStateType.
        /// Because this class replaces an enum, values of this class may only be created inside of it,
        /// hence the visibility modifier is set to private.
        /// </summary>
        /// <param name="name">The Name of this ActionStateType. Must be unique.</param>
        /// <param name="description">Description for this ActionStateType.</param>
        /// <param name="parent">The parent of this action in the nesting hierarchy in the menu.</param>
        /// <param name="color">Color for this ActionStateType.</param>
        /// <param name="icon">Icon for this ActionStateType.</param>
        /// <param name="createReversible">Delegate to be called to create a new instance of this kind of action.
        /// Can be null, in which case no delegate will be called.</param>
        /// <param name="register">If true, this action state type will be registered in <see cref="ActionStateTypes"/>.</param>
        public ActionStateType(string name, string description,
            Color color, char icon, CreateReversibleAction createReversible, ActionStateTypeGroup parent = null, bool register = true)
            : base(name, description, color, icon, parent, register)
        {
            CreateReversible = createReversible;

            if (register && !actionStateTypes.ContainsKey(name))
            {
                actionStateTypes[name] = this;
            }
        }

        public static ActionStateType GetActionStateTypeByName(string name)
        {
            if (actionStateTypes.TryGetValue(name, out var actionStateType))
            {
                return actionStateType;
            }

            return null; // Return null if not found
        }

        #region Equality & Comparators

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() && ((ActionStateType)obj).CreateReversible == CreateReversible;
        }

        public override int GetHashCode()
        {
            return CreateReversible.GetHashCode();
        }

        #endregion
    }
}
