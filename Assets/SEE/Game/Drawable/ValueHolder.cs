using SEE.Game;
using System;
using System.Collections.Generic;
using System.Xml;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.InputSystem.HID;

namespace Assets.SEE.Game.Drawable
{
    /// <summary>
    /// This class holds the current values and the constants for drawables.
    /// </summary>
    public static class ValueHolder
    {
        #region current values
        /// <summary>
        /// The current chosen primary color for drawing.
        /// </summary>
        public static Color currentPrimaryColor { get; set; }

        /// <summary>
        /// The current chosen secondary color for drawing.
        /// </summary>
        public static Color currentSecondaryColor { get; set; }

        /// <summary>
        /// The current chosen thickness for drawing.
        /// </summary>
        public static float currentThickness { get; set; }

        /// <summary>
        /// The current chosen thickness for text outline.
        /// </summary>
        public static float currentOutlineThickness { get; set; }

        /// <summary>
        /// The current chosen text font size.
        /// </summary>
        public static float currentFontSize { get; set; }

        /// <summary>
        /// The current chosen line kind for drawing.
        /// </summary>
        public static GameDrawer.LineKind currentLineKind { get; set; }

        /// <summary>
        /// The current chosen color kind for drawing.
        /// </summary>
        public static GameDrawer.ColorKind currentColorKind { get; set; }

        /// <summary>
        /// The current chosen tiling for drawing a dashed line kind.
        /// </summary>
        public static float currentTiling { get; set; }

        /// <summary>
        /// The current order in layer value.
        /// </summary>
        public static int currentOrderInLayer { get; set; }
        #endregion

        #region prefixes
        /// <summary>
        /// The prefix of a line object name.
        /// </summary>
        public const string LinePrefix = "Line";

        /// <summary>
        /// The prefix of a text object name.
        /// </summary>
        public const string TextPrefix = "Text";

        /// <summary>
        /// The prefix of a image object name.
        /// </summary>
        public const string ImagePrefix = "Image";

        /// <summary>
        /// The prefix of a mind map theme node
        /// </summary>
        public const string MindMapThemePrefix = "Theme";

        /// <summary>
        /// The prefix of a mind map node node
        /// </summary>
        public const string MindMapSubthemePrefix = "Subtheme";

        /// <summary>
        /// The prefix of a mind map leaf node.
        /// </summary>
        public const string MindMapLeafPrefix = "Leaf";

        /// <summary>
        /// The prefix of a mind map branch line.
        /// </summary>
        public const string MindMapBranchLine = "BranchLine";
 
        /// <summary>
        /// The prefix of a sticky notes.
        /// </summary>
        public const string StickyNotePrefix = "StickyNote";

        /// <summary>
        /// The prefix of a drawable holder object.
        /// </summary>
        public const string DrawableHolderPrefix = "DrawableHolder";

        /// <summary>
        /// The name of the attached objects object.
        /// </summary>
        public const string AttachedObject = "AttachedObjects";
        #endregion

        /// <summary>
        /// The distance to a drawable that is used by default to place objects.
        /// </summary>
        public static readonly Vector3 distanceToDrawable = new(0, 0, 0.0001f);

        /// <summary>
        /// The standard line thickness
        /// </summary>
        public const float standardLineThickness = 0.01f;

        /// <summary>
        /// The standard line tiling.
        /// </summary>
        public const float standardLineTiling = 1.0f;

        /// <summary>
        /// The standard text outline thickness.
        /// </summary>
        public const float standardTextOutlineThickness = 0.4f;

        /// <summary>
        /// The standard scale up factor.
        /// </summary>
        public const float scaleUp = 1.01f;

        /// <summary>
        /// The scale up factor for fast scaling.
        /// </summary>
        public const float scaleUpFast = 1.1f;

        /// <summary>
        /// The standard scale down factor.
        /// </summary>
        public const float scaleDown = 0.99f;

        /// <summary>
        /// The scale down factor for fast scaling.
        /// </summary>
        public const float scaleDownFast = 0.9f;

        /// <summary>
        /// The constant for rotate by mouse wheel.
        /// </summary>
        public const float rotate = 1;

        /// <summary>
        /// The constant for fast rotate by mouse wheel.
        /// </summary>
        public const float rotateFast = 10;

        /// <summary>
        /// The constant for moving speed by key.
        /// </summary>
        public const float move = 0.001f;

        /// <summary>
        /// The constant for fast moving speed by key.
        /// </summary>
        public const float moveFast = 0.01f;

        /// <summary>
        /// The path to the folder drawable folder of the saved files. This is saved in a field because multiple
        /// methods of this class and other classes use it.
        /// </summary>
        public static readonly string drawablePath = Application.persistentDataPath + "/Drawable/";

        /// <summary>
        /// The path to the drawable image folder. This is saved in a field because multiple
        /// methods of this class and other classes use it.
        /// </summary>
        public static readonly string imagePath = drawablePath + "Image/";

        /// <summary>
        /// The list of object names or <see cref="Tags"/> whose rotation is suitable for spawning/moving Sticky Notes.
        /// In the list, object names or <see cref="Tags"/> of suitable objects can be added.
        /// Since the name cannot be uniquely determined for objects added during runtime, the solution with <see cref="Tags"/> is provided.
        /// In addition to these objects, objects with the tag 'Drawable' are also suitable. 
        /// The respective Sticky Note should then adopt its rotation.
        /// </summary>
        public static readonly List<string> SuitableObjectsForStickyNotes = new() { 
            "Mirror",
        };

        /// <summary>
        /// Checks if the given game object is a suitable object for the sticky note rotation.
        /// </summary>
        /// <param name="gameObject">The object to check.</param>
        /// <returns>true, if the game object is suitable for the sticky note rotation.</returns>
        public static bool IsASuitableObjectForStickyNote(GameObject gameObject)
        {
            return SuitableObjectsForStickyNotes.Contains(gameObject.name)
                    || SuitableObjectsForStickyNotes.Contains(gameObject.tag);
        }

        /// <summary>
        /// The direction's for moving. Will needed for sticky note menu.
        /// </summary>
        public enum MoveDirection
        {
            Left,
            Right,
            Up,
            Down,
            Forward,
            Back
        }

        /// <summary>
        /// The constructor, it loads the default values for the current values.
        /// </summary>
        static ValueHolder()
        {
            currentPrimaryColor = UnityEngine.Random.ColorHSV();
            currentSecondaryColor = Color.clear;
            currentOutlineThickness = 0.4f;
            currentFontSize = 0.5f;
            currentThickness = 0.01f;
            currentLineKind = GameDrawer.LineKind.Solid;
            currentColorKind = GameDrawer.ColorKind.Monochrome;
            currentTiling = 1f;
            currentOrderInLayer = 1;
        }
    }
}