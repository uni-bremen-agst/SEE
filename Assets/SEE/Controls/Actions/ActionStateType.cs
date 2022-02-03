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
        public static List<ActionStateType> DesktopMenuTypes { get; } = new List<ActionStateType>();
        public static List<ActionStateType> MobileMenuTypes { get; } = new List<ActionStateType>();

        #region Static Types
        //Desktop Button Group:
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

        /// <summary>
        /// Marks the border to the desktop button group.
        /// </summary>
        private const int BorderDesktop = 11;

        // Mobile Buttons:
        // Select Button group on the right side
        public static ActionStateType Select { get; } =
            new ActionStateType(11, "Select", "Select objects",
                                      Color.white.Darker(), "Materials/Charts/MoveIcon", DeleteAction.CreateReversibleAction);

        public static ActionStateType Deselect { get; } =
            new ActionStateType(12, "Deselect", "Deselect object",
                                      Color.white.Darker(), "Materials/ModernUIPack/Cancel Bold", DeleteAction.CreateReversibleAction);

        // Delete button group on the right side
        public static ActionStateType DeleteMobile { get; } =
            new ActionStateType(13, "Delete Mobile", "Delete a node on touch",
                                      Color.white.Darker(), "Materials/ModernUIPack/Trash", DeleteAction.CreateReversibleAction);

        // Delete multi button group on the right side
        public static ActionStateType DeleteMulti { get; } =
            new ActionStateType(14, "Delete Multi", "Delete multiple nodes",
                                      Color.white.Darker(), "Materials/ModernUIPack/Minus", DeleteAction.CreateReversibleAction);

        public static ActionStateType CancelDeletion { get; } =
            new ActionStateType(15, "Cancel Deletion", "Cancel the deletion of the selected objects",
                                      Color.white.Darker(), "Materials/ModernUIPack/Cancel Bold", DeleteAction.CreateReversibleAction);

        public static ActionStateType AcceptDeletion { get; } =
            new ActionStateType(16, "Accept Deletion", "Accept the deletion of the selected objects",
                                      Color.white.Darker(), "Materials/ModernUIPack/Check Bold", DeleteAction.CreateReversibleAction);

        // Rotate button group on the right side
        public static ActionStateType RotateMobile { get; } =
            new ActionStateType(17, "Rotate Mobile", "Rotation Mode",
                                      Color.white.Darker(), "Materials/ModernUIPack/Refresh", DeleteAction.CreateReversibleAction);

        public static ActionStateType RotateCity { get; } =
            new ActionStateType(18, "Rotate City", "Rotate the City",
                                      Color.white.Darker(), "Icons/n", DeleteAction.CreateReversibleAction);

        public static ActionStateType RotateObject { get; } =
            new ActionStateType(19, "Rotate Object", "Rotate an Object",
                                      Color.white.Darker(), "Icons/1", DeleteAction.CreateReversibleAction);

        public static ActionStateType LockedRotate { get; } =
            new ActionStateType(20, "Locked Rotation Mode", "Locked Rotation Mode",
                                      Color.white.Darker(), "Materials/GoogleIcons/ic_rotate_90_degrees_ccw_white_48dp", DeleteAction.CreateReversibleAction);

        public static ActionStateType LockedCenter { get; } =
            new ActionStateType(21, "Locked Around Center Mode", "Locked Around Center Mode",
                                      Color.white.Darker(), "Materials/GoogleIcons/ic_settings_backup_restore_white_48dp", DeleteAction.CreateReversibleAction);

        // Move button group on the right side
        public static ActionStateType MoveMobile { get; } =
            new ActionStateType(22, "Move Mobile", "Move Mode",
                                      Color.white.Darker(), "Materials/ModernUIPack/Horizontal Selector", DeleteAction.CreateReversibleAction);

        public static ActionStateType MoveCity { get; } =
            new ActionStateType(23, "Move City", "Move the whole City",
                                      Color.white.Darker(), "Icons/n", DeleteAction.CreateReversibleAction);

        public static ActionStateType MoveObject { get; } =
            new ActionStateType(24, "Move Object", "Move Object Mode",
                                      Color.white.Darker(), "Icons/8", DeleteAction.CreateReversibleAction);

        public static ActionStateType EightDirections { get; } =
            new ActionStateType(25, "8-Directions Mode", "8-Directions Mode",
                                      Color.white.Darker(), "Materials/ModernUIPack/Plus", DeleteAction.CreateReversibleAction);

        // Quick Menu group on the left side
        public static ActionStateType Redo { get; } =
            new ActionStateType(26, "Redo Action", "Redo Action",
                                      Color.white.Darker(), "Materials/GoogleIcons/ic_redo_white_48dp", DeleteAction.CreateReversibleAction);

        public static ActionStateType Undo { get; } =
            new ActionStateType(27, "Undo", "Undo Action",
                                      Color.white.Darker(), "Materials/GoogleIcons/ic_undo_white_48dp", DeleteAction.CreateReversibleAction);

        public static ActionStateType CameraLock { get; } =
            new ActionStateType(28, "Camera Lock Mode", "Camera Lock Mode",
                                      Color.white.Darker(), "Materials/ModernUIPack/Lock Open", DeleteAction.CreateReversibleAction);

        public static ActionStateType Rerotate { get; } =
            new ActionStateType(29, "Rerotate", "Set rotation back to standard",
                                      Color.white.Darker(), "Materials/GoogleIcons/ic_history_white_48dp", DeleteAction.CreateReversibleAction);

        public static ActionStateType Recenter { get; } =
            new ActionStateType(30, "Recenter", "Recenter the City",
                                      Color.white.Darker(), "Materials/GoogleIcons/ic_open_with_white_48dp", DeleteAction.CreateReversibleAction);

        public static ActionStateType Collapse { get; } =
            new ActionStateType(31, "Collapse", "Collapse the Quick Menu",
                                      Color.white.Darker(), "Materials/ModernUIPack/Arrow Bold", DeleteAction.CreateReversibleAction);
        /// <summary>
        /// Marks the border to the mobile button group.
        /// </summary>
        private const int BorderMobileMenu = 32;

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

            // Create button group lists
           if (value < BorderDesktop)
           {
                DesktopMenuTypes.Add(this);
           }
           else if (value < BorderMobileMenu)
           {
                MobileMenuTypes.Add(this);
           }
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