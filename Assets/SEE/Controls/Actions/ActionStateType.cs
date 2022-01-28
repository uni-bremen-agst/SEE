using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// The abstract type of a state-based action.
    /// </summary>
    public class ActionStateType : AbstractActionStateType<ActionStateType>
    {
        #region Static Types

        public static ActionStateType Move { get; } =
            new ActionStateType(0, "Move", "Move a node within a graph",
                                Color.red.Darker(), "Materials/Charts/MoveIcon",
                                MoveAction.CreateReversibleAction);

        public static ActionStateType Rotate { get; } =
            new ActionStateType(1, "Rotate", "Rotate everything around the selected node within a graph",
                                Color.blue.Darker(), "Materials/ModernUIPack/Refresh",
                                RotateAction.CreateReversibleAction);

        public static ActionStateType Map { get; } =
            new ActionStateType(2, "Map", "Map a node from one graph to another graph",
                                Color.green.Darker(), "Materials/ModernUIPack/Map",
                                MappingAction.CreateReversibleAction);

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

        public static ActionStateType Hide { get; } =
            new ActionStateType(10, "Hide Node", "Hides a node",
                                Color.yellow.Darker(), "Materials/ModernUIPack/Eye", HideAction.CreateReversibleAction);

        #endregion

        /// <summary>
        /// Constructor for <see cref="ActionStateType"/>.
        /// Because this class replaces an enum, values of this class may only be created inside of it,
        /// hence the visibility modifier is set to private.
        /// </summary>
        /// <param name="value">The ID of this <see cref="ActionStateType"/>.
        /// Must increase by one for each new instantiation.</param>
        /// <param name="name">The Name of this <see cref="ActionStateType"/>. Must be unique.</param>
        /// <param name="description">Description for this <see cref="ActionStateType"/>.</param>
        /// <param name="color">Color for this <see cref="ActionStateType"/>.</param>
        /// <param name="iconPath">Path to the material of the icon for this <see cref="ActionStateType"/>.</param>
        /// <exception cref="ArgumentException">When the given <paramref name="name"/> or <paramref name="value"/>
        /// is not unique, or when the <paramref name="value"/> doesn't fulfill the "must increase by one" criterion.
        /// </exception>
        private ActionStateType(int value, string name, string description, Color color, string iconPath,
                                CreateReversibleAction createReversible)
            : base(value, name, description, color, iconPath, createReversible)
        {
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
        protected ActionStateType(CreateReversibleAction createReversible) : base(createReversible)
        {
        }
    }
}