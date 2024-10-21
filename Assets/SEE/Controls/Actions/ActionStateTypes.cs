using UnityEngine;
using SEE.Utils;
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
            // IMPORTANT NOTE: The order of the following field assignments must be exaclty
            // the same as the order of their declarations below.

            Move =
              new("Move", "Move a node within a graph",
                  Color.red.Darker(), Icons.Move,
                  MoveAction.CreateReversibleAction);

            Hide =
              new("Hide", "Hides nodes or edges",
                  Color.yellow.Darker(), Icons.EyeSlash,
                  HideAction.CreateReversibleAction);

            NewEdge =
              new("New Edge", "Draw a new edge between two nodes",
                  Color.green.Darker(), Icons.Edge,
                  AddEdgeAction.CreateReversibleAction);

            NewNode =
              new("New Node", "Create a new node",
                  Color.green.Darker(), '+',
                  AddNodeAction.CreateReversibleAction);

            EditNode =
              new("Edit Node", "Edit a node",
                  Color.green.Darker(), Icons.PenToSquare,
                  EditNodeAction.CreateReversibleAction);

            ResizeNode =
              new("Resize Node", "Change the size of a node",
                  Color.green.Darker(), Icons.Resize,
                  ResizeNodeAction.CreateReversibleAction);

            Delete =
              new("Delete", "Delete a node or an edge",
                  Color.yellow.Darker(), Icons.Trash,
                  DeleteAction.CreateReversibleAction);

            ShowCode =
              new("Show Code", "Display the source code of a node.",
                  Color.black, Icons.Code,
                  ShowCodeAction.CreateReversibleAction);

            AcceptDivergence =
              new("Accept Divergence", "Accept a diverging edge into the architecture",
                  Color.grey.Darker(), Icons.CheckedCheckbox,
                  AcceptDivergenceAction.CreateReversibleAction);

            // Metric Board actions
            MetricBoard =
                new("Metric Board", "Manipulate a metric board",
                    Color.white.Darker(), Icons.Chalkboard);

            AddBoard =
              new("Add Board", "Add a board",
                  Color.green.Darker(), '+',
                  AddBoardAction.CreateReversibleAction,
                  parent: MetricBoard);

            AddWidget =
              new("Add Widget", "Add a widget",
                  Color.green.Darker(), '+',
                  AddWidgetAction.CreateReversibleAction,
                  parent: MetricBoard);

            MoveBoard =
              new("Move Board", "Move a board",
                  Color.yellow.Darker(), Icons.Move,
                  MoveBoardAction.CreateReversibleAction,
                  parent: MetricBoard);

            MoveWidget =
              new("Move Widget", "Move a widget",
                  Color.yellow.Darker(), Icons.Move,
                  MoveWidgetAction.CreateReversibleAction,
                  parent: MetricBoard);

            DeleteBoard =
              new("Delete Board", "Delete a board",
                  Color.red.Darker(), Icons.Trash,
                  DeleteBoardAction.CreateReversibleAction,
                  parent: MetricBoard);

            DeleteWidget =
              new("Delete Widget", "Delete a widget",
                  Color.red.Darker(), Icons.Trash,
                  DeleteWidgetAction.CreateReversibleAction,
                  parent: MetricBoard);

            LoadBoard =
              new("Load Board", "Load a board",
                  Color.blue.Darker(), Icons.Import,
                  LoadBoardAction.CreateReversibleAction,
                  parent: MetricBoard);

            SaveBoard =
              new("Save Board", "Save a board",
                  Color.blue.Darker(), Icons.Export,
                  SaveBoardAction.CreateReversibleAction,
                  parent: MetricBoard);

            // Whiteboard actions
            Drawable =
              new("Drawable", "Please select the drawable mode you want to activate.",
                  Color.blue, Icons.Whiteboard);

            DrawFreehand =
                new("Draw Freehand", "Draws a line on a drawable.",
                    Color.magenta.Darker(), Icons.Brush,
            DrawFreehandAction.CreateReversibleAction,
                    parent: Drawable);

            DrawShapes =
                new("Draw Shape", "Draws various shapes on a drawable.",
                    Color.magenta.Darker(), Icons.Shapes,
                    DrawShapesAction.CreateReversibleAction,
                    parent: Drawable);

            WriteText =
                new("Write Text", "Writes a text on a drawable.",
                    Color.magenta.Darker(), Icons.Text,
                    WriteTextAction.CreateReversibleAction,
                    parent: Drawable);

            AddImage =
                new("Add an Image", "Adds an image to a drawable.",
                    Color.magenta.Darker(), Icons.Image,
                    AddImageAction.CreateReversibleAction,
                    parent: Drawable);

            MindMap =
                new("Mind Map", "Adds and controls mind-map components.",
                    Color.magenta.Darker(), Icons.FolderTree,
                    MindMapAction.CreateReversibleAction,
                    parent: Drawable);

            StickyNote =
                new("Sticky Note", "Manages sticky notes (spawn/move/edit/delete).",
                    Color.blue.Darker(), Icons.StickyNote,
                    StickyNoteAction.CreateReversibleAction,
                    parent: Drawable);

            ColorPicker =
                new("Color Picker", "Picks a color.",
                    Color.yellow.Darker(), Icons.EyeDropper,
                    ColorPickerAction.CreateReversibleAction,
                    parent: Drawable);

            Edit =
                new("Edit", "Edits a drawable.",
                    Color.green.Darker(), Icons.Edit,
                    EditAction.CreateReversibleAction,
                    parent: Drawable);

            MoveRotator =
                new("Move or Rotate", "Moves or rotates an object on a drawable.",
                    Color.green.Darker(), Icons.Move,
                    MoveRotateAction.CreateReversibleAction,
                    parent: Drawable);

            LayerChanger =
                new("Change The Sorting Layer", "Left mouse click to increase, right mouse click to decrease the layer.",
                    Color.green.Darker(), Icons.Layer,
                    LayerChangeAction.CreateReversibleAction,
                    parent: Drawable);

            CutCopyPaste =
                new("Cut, Copy, Paste", "Cuts or copies a drawable and pastes it on the selected position.",
                    Color.green.Darker(), Icons.Cut,
                    CutCopyPasteAction.CreateReversibleAction,
                    parent: Drawable);

            Scale =
                new("Scale", "Scales a drawable. Mouse wheel up to scale up, mouse wheel down to scale down.",
                    Color.green.Darker(), Icons.Scale,
                    ScaleAction.CreateReversibleAction,
                    parent: Drawable);

            MovePoint =
                new("Move a Point", "Moves a point of a line.",
                    Color.green.Darker().Darker(), Icons.MoveAPoint,
                    MovePointAction.CreateReversibleAction,
                    parent: Drawable);

            LineSplit =
                new("Line Split", "Splits a line on a given point.",
                    Color.green.Darker().Darker(), Icons.Split,
                    LineSplitAction.CreateReversibleAction,
                    parent: Drawable);

            LinePointErase =
                new("Line Point Erase", "Erases a point from a line on a drawable.",
                    Color.red, Icons.Erase,
                    LinePointEraseAction.CreateReversibleAction,
                    parent: Drawable);

            LineConnectionErase =
                new("Line Connection Erase", "Erases a line connection from a line of the chosen point.",
                    Color.red, Icons.Erase,
                    LineConnectionEraseAction.CreateReversibleAction,
                    parent: Drawable);

            Erase =
                new("Erase", "Erases a complete object on a drawable.",
                    Color.red.Darker(), Icons.Erase,
                    EraseAction.CreateReversibleAction,
                    parent: Drawable);

            Clear =
                new("Clear", "Clears a drawable surface (whiteboard / sticky note).",
                    Color.red.Darker(), Icons.Trash,
                    ClearAction.CreateReversibleAction,
                    parent: Drawable);

            Save =
                new("Save", "Saves one or more drawables.",
                    Color.yellow, Icons.Save,
                    SaveAction.CreateReversibleAction,
                    parent: Drawable);

            Load =
                new("Load", "Loads one or more drawables.",
                    Color.yellow.Darker(), Icons.Load,
                    LoadAction.CreateReversibleAction,
                    parent: Drawable);
        }

        // IMPORTANT NOTE: The order of the following field declarations must be exaclty the same
        // as the order of their assignments in the static constructor above.

        public static readonly ActionStateType Move;
        public static readonly ActionStateType Rotate;
        public static readonly ActionStateType Hide;
        public static readonly ActionStateType NewEdge;
        public static readonly ActionStateType NewNode;
        public static readonly ActionStateType EditNode;
        public static readonly ActionStateType ResizeNode;
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

        public static readonly ActionStateTypeGroup Drawable;
        public static readonly ActionStateType DrawFreehand;
        public static readonly ActionStateType DrawShapes;
        public static readonly ActionStateType WriteText;
        public static readonly ActionStateType AddImage;
        public static readonly ActionStateType MindMap;
        public static readonly ActionStateType StickyNote;
        public static readonly ActionStateType ColorPicker;
        public static readonly ActionStateType Edit;
        public static readonly ActionStateType MoveRotator;
        public static readonly ActionStateType LayerChanger;
        public static readonly ActionStateType CutCopyPaste;
        public static readonly ActionStateType Scale;
        public static readonly ActionStateType MovePoint;
        public static readonly ActionStateType LineSplit;
        public static readonly ActionStateType LinePointErase;
        public static readonly ActionStateType LineConnectionErase;
        public static readonly ActionStateType Erase;
        public static readonly ActionStateType Clear;
        public static readonly ActionStateType Save;
        public static readonly ActionStateType Load;

        #endregion

        /// <summary>
        /// Dumps all elements in <see cref="AllRootTypes"/>.
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
