using System;
using System.Collections.Generic;
using System.Linq;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// The abstract type of a state-based action.
    /// Implemented using the "Enumeration" (not enum) or "type safe enum" pattern.
    /// The following two pages have been used for reference:
    /// <ul>
    /// <li>https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/enumeration-classes-over-enum-types#implement-an-enumeration-base-class</li>
    /// <li>https://ardalis.com/enum-alternatives-in-c/</li>
    /// </ul>
    /// The <i>Curiously Recurring Template Pattern</i> (CRTP) has also been used, which allows
    /// inheritors to re-use e.g. <see cref="AllTypes"/>.
    /// </summary>
    public abstract class AbstractActionStateType<T> where T : AbstractActionStateType<T>
    {
        /// <summary>
        /// The name of this action.
        /// Must be unique across all types.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Description for this action.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Color for this action.
        /// Will be used in the menu and indicator.
        /// </summary>
        public Color Color { get; }

        /// <summary>
        /// Path to the material of the icon for this action.
        /// The icon itself should be a visual representation of the action.
        /// Will be used in the menu.
        /// </summary>
        public string IconPath { get; }

        /// <summary>
        /// Numeric value of this action.
        /// Must be unique across all types.
        /// Must increase by one for each new instantiation of a type.
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Delegate to be called to create a new instance of this kind of action.
        /// May be null if none needs to be created (in which case this delegate will not be called).
        /// </summary>
        public CreateReversibleAction CreateReversible { get; }

        /// <summary>
        /// A list of all available types.
        /// </summary>
        public static List<T> AllTypes { get; } = new List<T>();

        /// <summary>
        /// Constructor for <see cref="AbstractActionStateType{T}"/>.
        /// Because this class replaces an enum, values of this class may only be created inside of it,
        /// hence the visibility modifier is set to protected.
        /// Children of this class must use this constructor if they provide their own.
        ///
        /// </summary>
        /// <param name="value">The ID of this type. Must increase by one for each new instantiation.</param>
        /// <param name="name">The Name of this type. Must be unique.</param>
        /// <param name="description">Description for this type.</param>
        /// <param name="color">Color for this type.</param>
        /// <param name="iconPath">Path to the material of the icon for this type.</param>
        /// <exception cref="ArgumentException">When the given <paramref name="name"/> or <paramref name="value"/>
        /// is not unique, or when the <paramref name="value"/> doesn't fulfill the "must increase by one" criterion.
        /// </exception>
        protected AbstractActionStateType(int value, string name, string description, Color color, string iconPath, CreateReversibleAction createReversible)
        {
            Value = value;
            Name = name;
            Description = description;
            Color = color;
            IconPath = iconPath;
            CreateReversible = createReversible;

            // Check for duplicates
            if (AllTypes.Any(x => x.Value == value || x.Name == name))
            {
                throw new ArgumentException($"Duplicate {typeof(T)}s may not exist!\n");
            }

            // Check whether the ID is always increased by 1. For this, it suffices to check
            // the most recently added element, as all added elements go through this check.
            if (value != AllTypes.Select(x => x.Value + 1).DefaultIfEmpty(0).Last())
            {
                throw new ArgumentException($"{typeof(T)} IDs must be increasing by one!\n");
            }

            // Add new value to list of all types
            AllTypes.Add((T)this); // cast will always work due to CRTP
        }

        /// <summary>
        /// Constructor allowing to set <see cref="CreateReversible"/>.
        ///
        /// This constructor is needed for the test cases which implement
        /// their own variants of <see cref="ReversibleAction"/> and
        /// which need to provide an <see cref="ActionStateType"/> of
        /// their own.
        /// </summary>
        /// <param name="createReversible">value for <see cref="CreateReversible"/></param>
        protected AbstractActionStateType(CreateReversibleAction createReversible)
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

            return obj.GetType() == GetType() && ((T)obj).Value == Value;
        }

        public override int GetHashCode()
        {
            return Value;
        }

        #endregion
    }
}