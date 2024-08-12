using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.Drawable
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
        public static Color CurrentPrimaryColor { get; set; }

        /// <summary>
        /// The current chosen secondary color for drawing.
        /// </summary>
        public static Color CurrentSecondaryColor { get; set; }

        /// <summary>
        /// The current chosen tertiary color.
        /// </summary>
        public static Color CurrentTertiaryColor { get; set; }

        /// <summary>
        /// The current chosen thickness for drawing.
        /// </summary>
        public static float CurrentThickness { get; set; }

        /// <summary>
        /// The current chosen thickness for text outline.
        /// </summary>
        public static float CurrentOutlineThickness { get; set; }

        /// <summary>
        /// The current chosen text font size.
        /// </summary>
        public static float CurrentFontSize { get; set; }

        /// <summary>
        /// The current chosen line kind for drawing.
        /// </summary>
        public static GameDrawer.LineKind CurrentLineKind { get; set; }

        /// <summary>
        /// The current chosen color kind for drawing.
        /// </summary>
        public static GameDrawer.ColorKind CurrentColorKind { get; set; }

        /// <summary>
        /// The current chosen tiling for drawing a dashed line kind.
        /// </summary>
        public static float CurrentTiling { get; set; }

        /// <summary>
        /// The current order in layer value.
        /// </summary>
        public static int MaxOrderInLayer { get; set; }

        /// <summary>
        /// The current fill out status.
        /// </summary>
        public static bool CurrentFillOutStatus { get; set; }
        #endregion

        #region prefixes and names
        /// <summary>
        /// The prefix of a line object name.
        /// </summary>
        public const string LinePrefix = "Line";

        /// <summary>
        /// The prefix of a text object name.
        /// </summary>
        public const string TextPrefix = "Text";

        /// <summary>
        /// The prefix of an image object name.
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
        /// The prefix of a sticky note.
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

        /// <summary>
        /// The name of the fill out game object for the lines.
        /// </summary>
        public const string FillOut = "FillOut";
        #endregion

        /// <summary>
        /// The distance to a drawable that is used by default to place objects.
        /// </summary>
        public static readonly Vector3 DistanceToDrawable = new(0, 0, 0.0001f);

        /// <summary>
        /// The standard line thickness.
        /// </summary>
        public const float StandardLineThickness = 0.01f;

        /// <summary>
        /// The standard line tiling.
        /// </summary>
        public const float StandardLineTiling = 1.0f;

        /// <summary>
        /// The standard text outline thickness.
        /// </summary>
        public const float StandardTextOutlineThickness = 0.4f;

        /// <summary>
        /// The standard scale up factor.
        /// </summary>
        public const float ScaleUp = 1.01f;

        /// <summary>
        /// The scale up factor for fast scaling.
        /// </summary>
        public const float ScaleUpFast = 1.1f;

        /// <summary>
        /// The standard scale down factor.
        /// </summary>
        public const float ScaleDown = 0.99f;

        /// <summary>
        /// The scale down factor for fast scaling.
        /// </summary>
        public const float ScaleDownFast = 0.9f;

        /// <summary>
        /// The constant to rotate by mouse wheel.
        /// </summary>
        public const float Rotate = 1;

        /// <summary>
        /// The constant for fast rotate by mouse wheel.
        /// </summary>
        public const float RotateFast = 10;

        /// <summary>
        /// The constant for moving speed by key.
        /// </summary>
        public const float Move = 0.001f;

        /// <summary>
        /// The constant for fast moving speed by key.
        /// </summary>
        public const float MoveFast = 0.01f;

        /// <summary>
        /// Specifies the radius for the line split marker.
        /// </summary>
        public const float LineSplitMarkerRadius = 0.022f;

        /// <summary>
        /// Specifies the number of edges for the polygon of the line split marker.
        /// </summary>
        public const int LineSplitMarkerVertices = 30;

        /// <summary>
        /// Specifies the time after which the Line Split marker should be deleted.
        /// </summary>
        public const float LineSplitTimer = 3.0f;

        /// <summary>
        /// The default color for colors that do not have a complementary color.
        /// </summary>
        public static readonly Color DefaultComplementary = Color.magenta;

        /// <summary>
        /// The initial scale for sticky notes.
        /// </summary>
        public static readonly Vector3 StickyNoteScale = new (0.5f, 0.5f, 0.5f);

        /// <summary>
        /// The path to the drawable folder of the saved files. This is saved in a field because multiple
        /// methods of this class and other classes use it.
        /// </summary>
        public static readonly string DrawablePath = Application.persistentDataPath + "/Drawable/";

        /// <summary>
        /// The path to the drawable image folder. This is saved in a field because multiple
        /// methods of this class and other classes use it.
        /// </summary>
        public static readonly string ImagePath = DrawablePath + "Image/";

        /// <summary>
        /// The list of object names or <see cref="Tags"/> whose rotation is suitable for spawning/moving Sticky Notes.
        /// In the list, object names or <see cref="Tags"/> of suitable objects can be added.
        /// Since the name cannot be uniquely determined for objects added during runtime, the solution with <see cref="Tags"/> is provided.
        /// In addition to these objects, objects with the tag <see cref="Tags.Drawable"/> are also suitable.
        /// The respective Sticky Note should then adopt its rotation.
        /// </summary>
        public static readonly IList<string> SuitableObjectsForStickyNotes = new List<string>() {
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
        /// The direction for moving. Will be needed for sticky note menu.
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
        /// This list manages all drawable surfaces in the scene.
        /// </summary>
        public static List<GameObject> DrawableSurfaces = new();

        /// <summary>
        /// The constructor. It sets the default values for the current values.
        /// </summary>
        static ValueHolder()
        {
            CurrentPrimaryColor = UnityEngine.Random.ColorHSV();
            CurrentSecondaryColor = Color.clear;
            CurrentTertiaryColor = Color.clear;
            CurrentOutlineThickness = 0.4f;
            CurrentFontSize = 0.5f;
            CurrentThickness = 0.01f;
            CurrentLineKind = GameDrawer.LineKind.Solid;
            CurrentColorKind = GameDrawer.ColorKind.Monochrome;
            CurrentTiling = 1f;
            MaxOrderInLayer = 1;
            CurrentFillOutStatus = false;
        }
    }
}
