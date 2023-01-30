using System;
using System.Collections.Generic;
using System.Linq;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    public enum Level
    {
        Root,
        Hide
    }

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
            new ActionStateType("Move", "Move a node within a graph",
                                Level.Root, Color.red.Darker(), "Materials/Charts/MoveIcon",
                                MoveAction.CreateReversibleAction);
        public static ActionStateType Rotate { get; } =
            new ActionStateType("Rotate", "Rotate everything around the selected node within a graph",
                                Level.Root, Color.blue.Darker(), "Materials/ModernUIPack/Refresh",
                                RotateAction.CreateReversibleAction);
        public static ActionStateType Hide { get; } =
            new ActionStateType("Hide", "Hides nodes or edges",
                                Level.Root, Color.yellow.Darker(), "Materials/ModernUIPack/Eye", 
                                HideAction.CreateReversibleAction);
        public static ActionStateType HideConnectedEdges { get; } =
            new ActionStateType("Hide Connected Edges", "Hides connected edges",
                                Level.Hide, Color.yellow.Darker(), "Materials/ModernUIPack/Eye",
                                HideConnectedEdgesAction.CreateReversibleAction);
        public static ActionStateType NewEdge { get; } =
            new ActionStateType("New Edge", "Draw a new edge between two nodes",
                                Level.Root, Color.green.Darker(), "Materials/ModernUIPack/Minus",
                                AddEdgeAction.CreateReversibleAction);
        public static ActionStateType NewNode { get; } =
            new ActionStateType("New Node", "Create a new node",
                                Level.Root, Color.green.Darker(), "Materials/ModernUIPack/Plus",
                                AddNodeAction.CreateReversibleAction);
        public static ActionStateType EditNode { get; } =
            new ActionStateType("Edit Node", "Edit a node",
                                Level.Root, Color.green.Darker(), "Materials/ModernUIPack/Settings",
                                EditNodeAction.CreateReversibleAction);
        public static ActionStateType ScaleNode { get; } =
            new ActionStateType("Scale Node", "Scale a node",
                                Level.Root, Color.green.Darker(), "Materials/ModernUIPack/Crop",
                                ScaleNodeAction.CreateReversibleAction);
        public static ActionStateType Delete { get; } =
            new ActionStateType("Delete", "Delete a node or an edge",
                                Level.Root, Color.yellow.Darker(), "Materials/ModernUIPack/Trash",
                                DeleteAction.CreateReversibleAction);
        public static ActionStateType ShowCode { get; } =
            new ActionStateType("Show Code", "Display the source code of a node.",
                                Level.Root, Color.black, "Materials/ModernUIPack/Document", 
                                ShowCodeAction.CreateReversibleAction);
        public static ActionStateType Draw { get; } =
            new ActionStateType("Draw", "Draw a line",
                                Level.Root, Color.magenta.Darker(), "Materials/ModernUIPack/Pencil",
                                DrawAction.CreateReversibleAction);
        public static ActionStateType MetricBoard { get; } =
            new ActionStateType("Metric Board", "Configure Metric Boards",
                                Level.Root, Color.cyan, "Materials/40+ Simple Icons - Free/Mixer_Simple_Icons_UI.png",
                                MetricBoardAction.CreateReversibleAction);
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

        public Level Level { get;  }

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
        /// <param name="level">The nesting level of the action in the menu.</param>
        /// <param name="color">Color for this ActionStateType.</param>
        /// <param name="iconPath">Path to the material of the icon for this ActionStateType.</param>
        /// <param name="createReversible">The delegate to be called when the action has finished
        /// and a new instance needs to be created to continue.</param>
        /// <exception cref="ArgumentException">When the given <paramref name="name"/> or <paramref name="value"/>
        /// is not unique, or when the <paramref name="value"/> doesn't fulfill the "must increase by one" criterion.
        /// </exception>
        private ActionStateType(string name, string description, Level level,
                                Color color, string iconPath, CreateReversibleAction createReversible)
        {
            Level = level;
            Name = name;
            Description = description;
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
