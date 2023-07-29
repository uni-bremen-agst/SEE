using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// The type of a state-based action.
    /// Implemented using the "Enumeration" (not enum) or "type safe enum" pattern.
    /// The following two pages have been used for reference:
    /// <ul>
    /// <li>https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/enumeration-classes-over-enum-types#implement-an-enumeration-base-class</li>
    /// <li>https://ardalis.com/enum-alternatives-in-c/</li>
    /// </ul>
    /// </summary>
    {
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
        /// <param name="color">Color for this ActionStateType.</param>
        /// <param name="iconPath">Path to the material of the icon for this ActionStateType.</param>
        {
            CreateReversible = createReversible;
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

        }

        public override int GetHashCode()
        {
        }

        #endregion
    }
}
