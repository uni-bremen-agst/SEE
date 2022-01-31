using System;
using System.Collections.Generic;
using System.Linq;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// The type of a state-based action for mobile devices.
    /// </summary>
    public class MobileActionStateType : AbstractActionStateType<MobileActionStateType>
    {
        #region Static Types

        // Comment are just for orientation because the entries below will only be passed in AllTypes list
        // select button group on the right side
        public static MobileActionStateType Select { get; } =
            new MobileActionStateType(0, "Select", "Select objects",
                                      Color.white.Darker(), "Materials/Charts/MoveIcon", DeleteAction.CreateReversibleAction);

        public static MobileActionStateType Deselect { get; } =
            new MobileActionStateType(1, "Deselect", "Deselect object",
                                      Color.white.Darker(), "Materials/ModernUIPack/Cancel Bold", DeleteAction.CreateReversibleAction);

        // delete button group on the right side
        public static MobileActionStateType Delete { get; } =
            new MobileActionStateType(2, "Delete", "Delete a node on touch",
                                      Color.white.Darker(), "Materials/ModernUIPack/Trash", DeleteAction.CreateReversibleAction);

        // delete multi button group on the right side
        public static MobileActionStateType DeleteMulti { get; } =
            new MobileActionStateType(3, "Delete Multi", "Delete multiple nodes",
                                      Color.white.Darker(), "Materials/ModernUIPack/Minus", DeleteAction.CreateReversibleAction);

        public static MobileActionStateType CancelDeletion { get; } =
            new MobileActionStateType(4, "Cancel Deletion", "Cancel the deletion of the selected objects",
                                      Color.white.Darker(), "Materials/ModernUIPack/Cancel Bold", DeleteAction.CreateReversibleAction);

        public static MobileActionStateType AcceptDeletion { get; } =
            new MobileActionStateType(5, "Accept Deletion", "Accept the deletion of the selected objects",
                                      Color.white.Darker(), "Materials/ModernUIPack/Check Bold", DeleteAction.CreateReversibleAction);

        // rotate button group on the right side
        public static MobileActionStateType Rotate { get; } =
            new MobileActionStateType(6, "Rotate", "Rotation Mode",
                                      Color.white.Darker(), "Materials/ModernUIPack/Refresh", DeleteAction.CreateReversibleAction);

        public static MobileActionStateType RotateCity { get; } =
            new MobileActionStateType(7, "Rotate City", "Rotate the City",
                                      Color.white.Darker(), "n", DeleteAction.CreateReversibleAction);

        public static MobileActionStateType RotateObject { get; } =
            new MobileActionStateType(8, "Rotate Object", "Rotate an Object",
                                      Color.white.Darker(), "1", DeleteAction.CreateReversibleAction);

        public static MobileActionStateType LockedRotate { get; } =
            new MobileActionStateType(9, "Locked Rotation Mode", "Locked Rotation Mode",
                                      Color.white.Darker(), "Materials/GoogleIcons/ic_rotate_90_degrees_ccw_white_48dp", DeleteAction.CreateReversibleAction);

        public static MobileActionStateType LockedCenter { get; } =
            new MobileActionStateType(10, "Locked Around Center Mode", "Locked Around Center Mode",
                                      Color.white.Darker(), "Materials/GoogleIcons/ic_settings_backup_restore_white_48dp", DeleteAction.CreateReversibleAction);

        // move button group on the right side
        public static MobileActionStateType Move { get; } =
            new MobileActionStateType(11, "Move", "Move Mode",
                                      Color.white.Darker(), "Materials/ModernUIPack/Horizontal Selector", DeleteAction.CreateReversibleAction);

        public static MobileActionStateType MoveCity { get; } =
            new MobileActionStateType(12, "Move City", "Move the whole City",
                                      Color.white.Darker(), "n", DeleteAction.CreateReversibleAction);

        public static MobileActionStateType MoveObject { get; } =
            new MobileActionStateType(13, "Move Object", "Move Object Mode",
                                      Color.white.Darker(), "8", DeleteAction.CreateReversibleAction);

        public static MobileActionStateType EightDirections { get; } =
            new MobileActionStateType(14, "8-Directions Mode", "8-Directions Mode",
                                      Color.white.Darker(), "Materials/ModernUIPack/Plus", DeleteAction.CreateReversibleAction);

        // Quick Menu group on the left side
        public static MobileActionStateType Redo { get; } =
            new MobileActionStateType(15, "Redo Action", "Redo Action",
                                      Color.white.Darker(), "Materials/GoogleIcons/ic_redo_white_48dp", DeleteAction.CreateReversibleAction);

        public static MobileActionStateType Undo { get; } =
            new MobileActionStateType(16, "Undo", "Undo Action",
                                      Color.white.Darker(), "Materials/GoogleIcons/ic_undo_white_48dp", DeleteAction.CreateReversibleAction);

        public static MobileActionStateType CameraLock { get; } =
            new MobileActionStateType(17, "Camera Lock Mode", "Camera Lock Mode",
                                      Color.white.Darker(), "Materials/ModernUIPack/Lock Open", DeleteAction.CreateReversibleAction);

        public static MobileActionStateType Rerotate { get; } =
            new MobileActionStateType(18, "Rerotate", "Set rotation back to standard",
                                      Color.white.Darker(), "Materials/GoogleIcons/ic_history_white_48dp", DeleteAction.CreateReversibleAction);

        public static MobileActionStateType Recenter { get; } =
            new MobileActionStateType(19, "Recenter", "Recenter the City",
                                      Color.white.Darker(), "Materials/GoogleIcons/ic_open_with_white_48dp", DeleteAction.CreateReversibleAction);

        public static MobileActionStateType Collapse { get; } =
            new MobileActionStateType(20, "Collapse", "Collapse the Quick Menu",
                                      Color.white.Darker(), "Materials/ModernUIPack/Arrow Bold", DeleteAction.CreateReversibleAction);

        #endregion

        /// <summary>
        /// Constructor allowing to set <see cref="CreateReversible"/>.
        /// 
        /// This constructor is needed for the test cases which implement
        /// their own variants of <see cref="ReversibleAction"/> and 
        /// which need to provide an <see cref="MobileActionStateType"/> of
        /// their own.
        /// </summary>
        /// <param name="createReversible">value for <see cref="CreateReversible"/></param>
        protected MobileActionStateType(CreateReversibleAction createReversible) : base(createReversible)
        {
        }

        /// <summary>
        /// Constructor for <see cref="MobileActionStateType"/>.
        /// Because this class replaces an enum, values of this class may only be created inside of it,
        /// hence the visibility modifier is set to private.
        /// </summary>
        /// <param name="value">The ID of this <see cref="MobileActionStateType"/>.
        /// Must increase by one for each new instantiation.</param>
        /// <param name="name">The Name of this <see cref="MobileActionStateType"/>. Must be unique.</param>
        /// <param name="description">Description for this <see cref="MobileActionStateType"/>.</param>
        /// <param name="color">Color for this <see cref="MobileActionStateType"/>.</param>
        /// <param name="iconPath">Path to the material of the icon for this <see cref="MobileActionStateType"/>.</param>
        /// <exception cref="ArgumentException">When the given <paramref name="name"/> or <paramref name="value"/>
        /// is not unique, or when the <paramref name="value"/> doesn't fulfill the "must increase by one" criterion.
        /// </exception>
        private MobileActionStateType(int value, string name, string description, Color color, string iconPath,
                                      CreateReversibleAction createReversible)
            : base(value, name, description, color, iconPath, createReversible)
        {
        }
    }
}