using System;
using System.Collections.Generic;
using System.Linq;
using SEE.Controls.Actions.HolisticMetrics;
using SEE.Game.UI.StateIndicator;
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
        /// The nesting groupings of the actions.
        /// </summary>
        public enum Grouping
        {
            /// <summary>
            /// The group of actions that are at the root of the nesting.
            /// </summary>
            Root,
            /// <summary>
            /// The nested group of actions dealing with the metric board.
            /// </summary>
            MetricBoard
        }

        /// <summary>
        /// A list of all available ActionStateTypes.
        /// </summary>
        public static List<ActionStateType> AllTypes { get; } = new();

        #region Static Types
        public static ActionStateType Move { get; } =
            new("Move", "Move a node within a graph", Grouping.Root,
                Color.red.Darker(), "Materials/Charts/MoveIcon",
                MoveAction.CreateReversibleAction);
        public static ActionStateType Rotate { get; } =
            new("Rotate", "Rotate the selected node and its children within a graph", Grouping.Root,
                Color.blue.Darker(), "Materials/ModernUIPack/Refresh",
                RotateAction.CreateReversibleAction);
        public static ActionStateType Hide { get; } =
            new("Hide", "Hides nodes or edges", Grouping.Root,
                Color.yellow.Darker(), "Materials/ModernUIPack/Eye", HideAction.CreateReversibleAction);

        public static ActionStateType NewEdge { get; } =
            new("New Edge", "Draw a new edge between two nodes", Grouping.Root,
                Color.green.Darker(), "Materials/ModernUIPack/Minus",
                AddEdgeAction.CreateReversibleAction);
        public static ActionStateType NewNode { get; } =
            new("New Node", "Create a new node", Grouping.Root,
                Color.green.Darker(), "Materials/ModernUIPack/Plus",
                AddNodeAction.CreateReversibleAction);
        public static ActionStateType EditNode { get; } =
            new("Edit Node", "Edit a node", Grouping.Root,
                Color.green.Darker(), "Materials/ModernUIPack/Settings",
                EditNodeAction.CreateReversibleAction);
        public static ActionStateType ScaleNode { get; } =
            new("Scale Node", "Scale a node", Grouping.Root,
                Color.green.Darker(), "Materials/ModernUIPack/Crop",
                ScaleNodeAction.CreateReversibleAction);
        public static ActionStateType Delete { get; } =
            new("Delete", "Delete a node or an edge", Grouping.Root,
                Color.yellow.Darker(), "Materials/ModernUIPack/Trash",
                DeleteAction.CreateReversibleAction);
        public static ActionStateType ShowCode { get; } =
            new("Show Code", "Display the source code of a node.", Grouping.Root,
                Color.black, "Materials/ModernUIPack/Document",
                ShowCodeAction.CreateReversibleAction);
        public static ActionStateType Draw { get; } =
            new ("Draw", "Draw a line", Grouping.Root,
                 Color.magenta.Darker(), "Materials/ModernUIPack/Pencil",
                 DrawAction.CreateReversibleAction);
        public static ActionStateType AddBoard { get; } =
            new ("Add Board", "Add a board", Grouping.MetricBoard,
                 Color.green.Darker(), "Materials/ModernUIPack/Plus",
                 AddBoardAction.CreateReversibleAction);
        public static ActionStateType AddWidget { get; } =
            new ("Add Widget", "Add a widget", Grouping.MetricBoard,
                Color.green.Darker(), "Materials/ModernUIPack/Plus",
                AddWidgetAction.CreateReversibleAction);
        public static ActionStateType MoveBoard { get; } =
            new ("Move Board", "Move a board", Grouping.MetricBoard,
                Color.yellow.Darker(), "Materials/Charts/MoveIcon",
                MoveBoardAction.CreateReversibleAction);
        public static ActionStateType MoveWidget { get; } =
            new ("Move Widget", "Move a widget", Grouping.MetricBoard,
                Color.yellow.Darker(), "Materials/Charts/MoveIcon",
                MoveWidgetAction.CreateReversibleAction);
        public static ActionStateType DeleteBoard { get; } =
            new ("Delete Board", "Delete a board", Grouping.MetricBoard,
                Color.red.Darker(), "Materials/ModernUIPack/Trash",
                DeleteBoardAction.CreateReversibleAction);
        public static ActionStateType DeleteWidget { get; } =
            new ("Delete Widget", "Delete a widget", Grouping.MetricBoard,
                Color.red.Darker(),  "Materials/ModernUIPack/Trash",
                DeleteWidgetAction.CreateReversibleAction);
        public static ActionStateType LoadBoard { get; } =
            new ("Load Board", "Load a board", Grouping.MetricBoard,
                Color.blue.Darker(), "Materials/ModernUIPack/Document",
                LoadBoardAction.CreateReversibleAction);
        public static ActionStateType SaveBoard { get; } =
            new ("Save Board", "Save a board", Grouping.MetricBoard,
                Color.blue.Darker(), "Materials/ModernUIPack/Document",
                SaveBoardAction.CreateReversibleAction);

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
        /// The group of actions this action belongs to.
        /// </summary>
        public Grouping Group { get;  }

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
        /// <param name="name">The Name of this ActionStateType. Must be unique.</param>
        /// <param name="description">Description for this ActionStateType.</param>
        /// <param name="group">The group of actions this action belongs to.</param>
        /// <param name="color">Color for this ActionStateType.</param>
        /// <param name="iconPath">Path to the material of the icon for this ActionStateType.</param>
        /// <param name="createReversible">Delegate to be called to create a new instance of this kind of action.
        /// Can be null, in which case no delegate will be called.</param>
        /// <exception cref="ArgumentException">When the given <paramref name="name"/> or <paramref name="value"/>
        /// is not unique, or when the <paramref name="value"/> doesn't fulfill the "must increase by one" criterion.
        /// </exception>
        private ActionStateType(string name, string description, Grouping group,
            Color color, string iconPath, CreateReversibleAction createReversible)
        {
            Name = name;
            Description = description;
            Group = group;
            Color = color;
            IconPath = iconPath;
            CreateReversible = createReversible;

            // Check for duplicates
            if (AllTypes.Any(x => x.Name == name))
            {
                throw new ArgumentException("Duplicate ActionStateTypes may not exist!\n");
            }

            // Add new value to list of all types
            AllTypes.Add(this);
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
