using System;
using System.Collections.Generic;
using System.Linq;
using SEE.Utils;
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
    public class ActionStateType
    {
        /// <summary>
        /// A list of all available ActionStateTypes.
        /// </summary>
        public static List<ActionStateType> AllTypes { get; } = new List<ActionStateType>();

        #region Static Types
        public static ActionStateType Move { get; } =
            new ActionStateType(0, "Move", "Move a node within a graph",
                                Color.red.Darker(), "Materials/Charts/MoveIcon",
                                MoveAction.CreateReversibleAction);
        public static ActionStateType Rotate { get; } =
            new ActionStateType(1, "Rotate", "Rotate everything around the selected node within a graph",
                                Color.blue.Darker(), "Materials/ModernUIPack/Refresh",
                                RotateAction.CreateReversibleAction);
        public static ActionStateType Hide { get; } =
            new ActionStateType(2, "Hide", "Hides nodes or edges",
                       Color.yellow.Darker(), "Materials/ModernUIPack/Eye", HideAction.CreateReversibleAction);

        public static ActionStateType NewEdge { get; } =
            new ActionStateType(3, "New Edge", "Draw a new edge between two nodes",
                                Color.green.Darker(), "Materials/ModernUIPack/Minus",
                                AddEdgeAction.CreateReversibleAction);
        public static ActionStateType NewNode { get; } =
            new ActionStateType(4, "New Node", "Create a new node",
                                Color.green.Darker(), "Materials/ModernUIPack/Plus",
                                AddNodeAction.CreateReversibleAction);
        public static ActionStateType EditNode { get; } =
            new ActionStateType(5, "Edit Node", "Edit a node",
                                Color.green.Darker(), "Materials/ModernUIPack/Settings",
                                EditNodeAction.CreateReversibleAction);
        public static ActionStateType ScaleNode { get; } =
            new ActionStateType(6, "Scale Node", "Scale a node",
                                Color.green.Darker(), "Materials/ModernUIPack/Crop",
                                ScaleNodeAction.CreateReversibleAction);
        public static ActionStateType Delete { get; } =
            new ActionStateType(7, "Delete", "Delete a node or an edge",
                                Color.yellow.Darker(), "Materials/ModernUIPack/Trash",
                                DeleteAction.CreateReversibleAction);
        public static ActionStateType ShowCode { get; } =
            new ActionStateType(8, "Show Code", "Display the source code of a node.",
                                Color.black, "Materials/ModernUIPack/Document", ShowCodeAction.CreateReversibleAction);
        public static ActionStateType Draw { get; } =
            new ActionStateType(9, "Draw", "Draw a line",
                        Color.magenta.Darker(), "Materials/ModernUIPack/Pencil",
                        DrawAction.CreateReversibleAction);
        
        public static ActionStateType Mark { get; } =
            new ActionStateType(10, "Mark Node", "Marks ",
                Color.white, "Materials/ModernUIPack/Plus",
                MarkAction.CreateReversibleAction);
        #endregion

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
        /// Will be used in the <see cref="DesktopMenu"/> and <see cref="ActionStateIndicator"/>.
        /// </summary>
        public Color Color { get; }

        /// <summary>
        /// Path to the material of the icon for this action.
        /// The icon itself should be a visual representation of the action.
        /// Will be used in the <see cref="DesktopMenu"/>.
        /// </summary>
        public string IconPath { get; }

        /// <summary>
        /// Numeric value of this action.
        /// Must be unique across all types.
        /// Must increase by one for each new instantiation of an <see cref="ActionStateType"/>.
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Delegate to be called to create a new instance of this kind of action.
        /// May be null if none needs to be created (in which case this delegate will not be called).
        /// </summary>
        public CreateReversibleAction CreateReversible { get; }

        /// <summary>
        /// Constructor allowing to set <see cref="CreateReversible"/>.
        ///
        /// This constructor is needed for the test cases which implement
        /// their own variants of <see cref="ReversibleAction"/> and
        /// which need to provide an <see cref="ActionStateType"/> of
        /// their own.
        /// </summary>
        /// <param name="createReversible">value for <see cref="CreateReversible"/></param>
        protected ActionStateType(CreateReversibleAction createReversible)
        {
            CreateReversible = createReversible;
        }

        /// <summary>
        /// Constructor for ActionStateType.
        /// Because this class replaces an enum, values of this class may only be created inside of it,
        /// hence the visibility modifier is set to private.
        /// </summary>
        /// <param name="value">The ID of this ActionStateType. Must increase by one for each new instantiation.</param>
        /// <param name="name">The Name of this ActionStateType. Must be unique.</param>
        /// <param name="description">Description for this ActionStateType.</param>
        /// <param name="color">Color for this ActionStateType.</param>
        /// <param name="iconPath">Path to the material of the icon for this ActionStateType.</param>
        /// <exception cref="ArgumentException">When the given <paramref name="name"/> or <paramref name="value"/>
        /// is not unique, or when the <paramref name="value"/> doesn't fulfill the "must increase by one" criterion.
        /// </exception>
        private ActionStateType(int value, string name, string description, Color color, string iconPath, CreateReversibleAction createReversible)
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
                throw new ArgumentException("Duplicate ActionStateTypes may not exist!\n");
            }

            // Check whether the ID is always increased by 1. For this, it suffices to check
            // the most recently added element, as all added elements go through this check.
            if (value != AllTypes.Select(x => x.Value + 1).DefaultIfEmpty(0).Last())
            {
                throw new ArgumentException("ActionStateType IDs must be increasing by one!\n");
            }

            // Add new value to list of all types
            AllTypes.Add(this);
        }

        /// <summary>
        /// Returns the ActionStateType whose ID matches the given parameter.
        /// </summary>
        /// <param name="ID">The ID of the ActionStateType which shall be returned</param>
        /// <returns>the ActionStateType whose ID matches the given parameter</returns>
        /// <exception cref="InvalidOperationException">If no such ActionStateType exists.</exception>
        public static ActionStateType FromID(int ID)
        {
            return AllTypes.Single(x => x.Value == ID);
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

            return obj.GetType() == GetType() && ((ActionStateType)obj).Value == Value;
        }

        public override int GetHashCode()
        {
            return Value;
        }

        #endregion
    }
}
