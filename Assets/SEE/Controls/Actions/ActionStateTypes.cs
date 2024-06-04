using SEE.Utils;
using UnityEngine;
using SEE.Controls.Actions.HolisticMetrics;
using SEE.Controls.Actions.Drawable;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Provides all available <see cref="AbstractActionStateType"/>s.
    /// </summary>
    /// <remarks>These are used for <see cref="SEE.GO.Menu.PlayerMenu"/>.</remarks>
    public static class ActionStateTypes
    {
        /// <summary>
        /// All available ActionStateTypes as a <see cref="Forest{T}"/>.
        /// All elements in the forest can be derived at once via
        /// <see cref="AllRootTypes.Elements()"/>.
        /// </summary>
        public static Forest<AbstractActionStateType> AllRootTypes { get; }
            = new();

        /// <summary>
        /// Adds <paramref name="actionStateType"/> to the list of all action state
        /// types <see cref="AllRootTypes"/>.
        /// </summary>
        /// <param name="actionStateType">to be added</param>
        public static void Add(AbstractActionStateType actionStateType)
        {
            if (actionStateType.Parent == null)
            {
                AllRootTypes.AddRoot(actionStateType);
            }
            else
            {
                AllRootTypes.AddChild(actionStateType, actionStateType.Parent);
            }
        }

        /// <summary>
        /// Returns the default <see cref="ActionStateType"/> at top level (root)
        /// of <see cref="AllRootTypes"/>, i.e. one that does not have a parent.
        /// This is the one that should be executed initially if the user has not
        /// made any menu selections yet.
        /// </summary>
        /// <returns>default <see cref="ActionStateType"/> at top level</returns>
        /// <remarks><see cref="ActionStateType.Move"/> is the returned default</remarks>
        public static ActionStateType FirstActionStateType()
        {
            return Move;
        }

        #region Static Types

        /// <summary>
        /// Initializes all action state types and groups.
        ///
        /// C# uses lazy evaluation of initializers. That is why we initialize the action state
        /// types and groups ourselves in this static constructor. This will make sure that they
        /// actually have defined value (different from null) when used. That is particularly
        /// important for action state groups which are used in the initialization of all
        /// action state types and groups contained in them.
        /// </summary>
        static ActionStateTypes()
        {
            Move =
              new("Move", "Move a node within a graph",
                  Color.red.Darker(), "Materials/Charts/MoveIcon",
                  MoveAction.CreateReversibleAction);

            Rotate =
              new("Rotate", "Rotate the selected node and its children within a graph",
                  Color.blue.Darker(), "Materials/ModernUIPack/Refresh",
                  RotateAction.CreateReversibleAction);

            Hide =
              new("Hide", "Hides nodes or edges",
                  Color.yellow.Darker(), "Materials/ModernUIPack/Eye",
                  HideAction.CreateReversibleAction);

            NewEdge =
              new("New Edge", "Draw a new edge between two nodes",
                  Color.green.Darker(), "Materials/ModernUIPack/Minus",
                  AddEdgeAction.CreateReversibleAction);

            NewNode =
              new("New Node", "Create a new node",
                  Color.green.Darker(), "Materials/ModernUIPack/Plus",
                  AddNodeAction.CreateReversibleAction);

            EditNode =
              new("Edit Node", "Edit a node",
                  Color.green.Darker(), "Materials/ModernUIPack/Settings",
                  EditNodeAction.CreateReversibleAction);

            ScaleNode =
              new("Scale Node", "Scale a node",
                  Color.green.Darker(), "Materials/ModernUIPack/Crop",
                  ScaleNodeAction.CreateReversibleAction);

            Delete =
              new("Delete", "Delete a node or an edge",
                  Color.yellow.Darker(), "Materials/ModernUIPack/Trash",
                  DeleteAction.CreateReversibleAction);

            ShowCode =
              new("Show Code", "Display the source code of a node.",
                  Color.black, "Materials/ModernUIPack/Document",
                  ShowCodeAction.CreateReversibleAction);

            Draw =
              new("Draw", "Draw a line",
                  Color.magenta.Darker(), "Materials/ModernUIPack/Pencil",
                  DrawAction.CreateReversibleAction);

            AcceptDivergence =
              new("Accept Divergence", "Accept a diverging edge into the architecture",
                  Color.grey.Darker(), "Materials/ModernUIPack/Arrow Bold",
                  AcceptDivergenceAction.CreateReversibleAction);

            // Metric Board actions
            MetricBoard =
              new("Metric Board", "Manipulate a metric board",
                  Color.white.Darker(), "Materials/ModernUIPack/Pencil");

            AddBoard =
              new("Add Board", "Add a board",
                  Color.green.Darker(), "Materials/ModernUIPack/Plus",
                  AddBoardAction.CreateReversibleAction,
                  parent: MetricBoard);

            AddWidget =
              new("Add Widget", "Add a widget",
                  Color.green.Darker(), "Materials/ModernUIPack/Plus",
                  AddWidgetAction.CreateReversibleAction,
                  parent: MetricBoard);

            MoveBoard =
              new("Move Board", "Move a board",
                  Color.yellow.Darker(), "Materials/Charts/MoveIcon",
                  MoveBoardAction.CreateReversibleAction,
                  parent: MetricBoard);

            MoveWidget =
              new("Move Widget", "Move a widget",
                  Color.yellow.Darker(), "Materials/Charts/MoveIcon",
                  MoveWidgetAction.CreateReversibleAction,
                  parent: MetricBoard);

            DeleteBoard =
              new("Delete Board", "Delete a board",
                  Color.red.Darker(), "Materials/ModernUIPack/Trash",
                  DeleteBoardAction.CreateReversibleAction,
                  parent: MetricBoard);

            DeleteWidget =
              new("Delete Widget", "Delete a widget",
                  Color.red.Darker(), "Materials/ModernUIPack/Trash",
                  DeleteWidgetAction.CreateReversibleAction,
                  parent: MetricBoard);

            LoadBoard =
              new("Load Board", "Load a board",
                  Color.blue.Darker(), "Materials/ModernUIPack/Document",
                  LoadBoardAction.CreateReversibleAction,
                  parent: MetricBoard);

            SaveBoard =
              new("Save Board", "Save a board",
                  Color.blue.Darker(), "Materials/ModernUIPack/Document",
                  SaveBoardAction.CreateReversibleAction,
                  parent: MetricBoard);

            // Whiteboard actions
            Drawable =
              new("Drawable", "Please select the drawable mode you want to activate.",
                  Color.blue, "Materials/Drawable/Drawable");

            DrawFreehand =
                new("Draw Freehand", "Draws a line on a drawable.",
                    Color.magenta.Darker(), "Materials/Drawable/Brush",
                    DrawFreehandAction.CreateReversibleAction,
                    parent: Drawable);

            DrawShapes =
                new("Draw Shape", "Draws different shapes on a drawable.",
                    Color.magenta.Darker(), "Materials/ModernUIPack/Crop",
                    DrawShapesAction.CreateReversibleAction,
                    parent: Drawable);

            WriteText =
                new("Write Text", "Writes a text on a drawable.",
                    Color.magenta.Darker(), "Materials/Drawable/Text",
                    WriteTextAction.CreateReversibleAction,
                    parent: Drawable);

            AddImage =
                new("Add an Image", "Adds an image to a drawable.",
                    Color.magenta.Darker(), "Materials/Drawable/Image",
                    AddImageAction.CreateReversibleAction,
                    parent: Drawable);

            MindMap =
                new("Mind Map", "Adds and control mind-map components.",
                    Color.magenta.Darker(), "Materials/Charts/TreeIcon",
                    MindMapAction.CreateReversibleAction,
                    parent: Drawable);

            ColorPicker =
                new("Color Picker", "Picks a color.",
                    Color.yellow.Darker(), "Materials/Drawable/Eyedropper",
                    ColorPickerAction.CreateReversibleAction,
                    parent: Drawable);

            Edit =
                new("Edit", "Edits a drawable type.",
                    Color.green.Darker(), "Materials/Drawable/Edit",
                    EditAction.CreateReversibleAction,
                    parent: Drawable);

            MoveRotator =
                new("Move or Rotate", "Moves or rotates an object on a drawable.",
                    Color.green.Darker(), "Materials/Drawable/MoveRotator",
                    MoveRotateAction.CreateReversibleAction,
                    parent: Drawable);

            Scale =
                new("Scale", "Scales a drawable type. Mouse wheel up to scale up, mouse wheel down to scale down.",
                    Color.green.Darker(), "Materials/Drawable/Scale",
                    ScaleAction.CreateReversibleAction,
                    parent: Drawable);

            LayerChanger =
                new("Change The Sorting Layer", "Left mouse click to increase, right mouse click to decrease.",
                    Color.green.Darker(), "Materials/Drawable/Layer",
                    LayerChangerAction.CreateReversibleAction,
                    parent: Drawable);

            CutCopyPaste = new("Cut, Copy, Paste", "Cuts or copies a drawable type and pastes it on the selected position.",
                    Color.green.Darker(), "Materials/Drawable/CutCopyPaste",
                    CutCopyPasteAction.CreateReversibleAction,
                    parent: Drawable);

            MovePoint =
                new("Move a Point", "Moves a point of a line.",
                    Color.green.Darker().Darker(), "Materials/Charts/MoveIcon",
                    MovePointAction.CreateReversibleAction,
                    parent: Drawable);

            LineSplit =
                new("Line Split", "Splits a line on a given point.",
                    Color.green.Darker().Darker(), "Materials/Drawable/LineSplit",
                    LineSplitAction.CreateReversibleAction,
                    parent: Drawable);

            Save =
                new("Save", "Saves one or more drawables.",
                    Color.yellow, "Materials/Drawable/Save",
                    SaveAction.CreateReversibleAction,
                    parent: Drawable);

            Load =
                new("Load", "Loads one or more drawables.",
                    Color.yellow.Darker(), "Materials/ModernUIPack/Document",
                    LoadAction.CreateReversibleAction,
                    parent: Drawable);

            LinePointErase =
                new("Line Point Erase", "Erases a point from a line on a drawable.",
                    Color.red, "Materials/Drawable/LineErase",
                    LinePointEraseAction.CreateReversibleAction,
                    parent: Drawable);

            LineConnectionErase =
                new("Line Connection Erase", "Erases a line connection from a line of the chosen point.",
                    Color.red, "Materials/Drawable/LineConnectionErase",
                    LineConnectionEraseAction.CreateReversibleAction,
                    parent: Drawable);

            Erase =
                new("Erase", "Erase a complete object on a drawable.",
                    Color.red.Darker(), "Materials/Drawable/Erase",
                    EraseAction.CreateReversibleAction,
                    parent: Drawable);

            Cleaner =
                new("Cleaner", "Cleans a complete drawable.",
                    Color.red.Darker(), "Materials/ModernUIPack/Trash",
                    CleanerAction.CreateReversibleAction,
                    parent: Drawable);

            StickyNote =
                new("Sticky Note", "Manages sticky notes (spawn/move/edit/delete).",
                 Color.blue.Darker(), "Materials/Drawable/StickyNote",
                    StickyNoteAction.CreateReversibleAction,
                    parent: Drawable);
        }


        public static readonly ActionStateType Move;
        public static readonly ActionStateType Rotate;
        public static readonly ActionStateType Hide;
        public static readonly ActionStateType NewEdge;
        public static readonly ActionStateType NewNode;
        public static readonly ActionStateType EditNode;
        public static readonly ActionStateType ScaleNode;
        public static readonly ActionStateType Delete;
        public static readonly ActionStateType ShowCode;
        public static readonly ActionStateType Draw;
        public static readonly ActionStateType AcceptDivergence;

        public static readonly ActionStateTypeGroup MetricBoard;
        public static readonly ActionStateType AddBoard;
        public static readonly ActionStateType AddWidget;
        public static readonly ActionStateType MoveBoard;
        public static readonly ActionStateType MoveWidget;
        public static readonly ActionStateType DeleteBoard;
        public static readonly ActionStateType DeleteWidget;
        public static readonly ActionStateType LoadBoard;
        public static readonly ActionStateType SaveBoard;

        public readonly static ActionStateTypeGroup Drawable;
        public readonly static ActionStateType DrawFreehand;
        public readonly static ActionStateType DrawShapes;
        public readonly static ActionStateType WriteText;
        public readonly static ActionStateType AddImage;
        public readonly static ActionStateType MindMap;
        public readonly static ActionStateType ColorPicker;
        public readonly static ActionStateType Edit;
        public readonly static ActionStateType MoveRotator;
        public readonly static ActionStateType MovePoint;
        public readonly static ActionStateType LayerChanger;
        public readonly static ActionStateType CutCopyPaste;
        public readonly static ActionStateType Scale;
        public readonly static ActionStateType LineSplit;
        public readonly static ActionStateType Load;
        public readonly static ActionStateType Save;
        public readonly static ActionStateType Erase;
        public readonly static ActionStateType LinePointErase;
        public readonly static ActionStateType LineConnectionErase;
        public readonly static ActionStateType Cleaner;
        public readonly static ActionStateType StickyNote;

        #endregion

        /// <summary>
        /// Dumps all elements in <see cref="AllRootTypes"/>-
        /// Can be used for debugging.
        /// </summary>
        public static void Dump()
        {
            AllRootTypes.PreorderTraverse(DumpNode);

            bool DumpNode(AbstractActionStateType child, AbstractActionStateType parent)
            {
                Debug.Log($"child: {child.Name} parent: {Name(parent)}\n");
                return true;
            }

            string Name(AbstractActionStateType type)
            {
                const string none = "<NONE>";
                return type == null ? none : type.Name;
            }
        }
    }
}
