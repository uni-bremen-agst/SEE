using SEE.Utils;
using UnityEngine;
using SEE.Controls.Actions.HolisticMetrics;
using Assets.SEE.Controls.Actions.Whiteboard;
using Assets.SEE.Controls.Actions.Drawable;

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
            Whiteboard =
              new("Whiteboard", "Please select the whiteboard mode you want to activate.",
                  Color.blue, "Materials/ModernUIPack/Pencil");

            DrawOn =
                new("Draw on", "Draw a line on a drawable",
                    Color.magenta.Darker(), "Materials/ModernUIPack/Pencil",
                    DrawOnAction.CreateReversibleAction,
                    parent: Whiteboard);

            DrawShapes =
                new("Draw Shape", "Draw different shapes on a drawable",
                    Color.magenta.Darker(), "Materials/ModernUIPack/Pencil",
                    DrawShapesAction.CreateReversibleAction,
                    parent: Whiteboard);

            ColorPicker =
                new("Color picker", "Picks a color",
                    Color.yellow.Darker(), "Materials/ModernUIPack/Pencil",
                    ColorPickerAction.CreateReversibleAction,
                    parent: Whiteboard);

            EditLine =
                new("Edit line", "Edit the selected line.",
                    Color.green.Darker(), "Materials/ModernUIPack/Pencil",
                    EditLineAction.CreateReversibleAction,
                    parent: Whiteboard);

            MoveRotator =
                new("Move or Rotate", "Move or rotates a Object on the Drawable.",
                    Color.green.Darker(), "Materials/ModernUIPack/Pencil",
                    MoveRotatorAction.CreateReversibleAction,
                    parent: Whiteboard);

            MovePoint =
                new("Moves a point", "Moves a point of a line.",
                    Color.green.Darker(), "Materials/ModernUIPack/Pencil",
                    MovePointAction.CreateReversibleAction,
                    parent: Whiteboard);

            LayerChanger =
                new("Change the sorting layer", "Left mouse click to increase, right mouse click to decrease",
                    Color.green.Darker(), "Materials/ModernUIPack/Pencil",
                    LayerChangerAction.CreateReversibleAction,
                    parent: Whiteboard);

            LineSplit =
                new("Line split", "Splittes a line on a given point.",
                    Color.green.Darker(), "Materials/ModernUIPack/Pencil",
                    LineSplitAction.CreateReversibleAction,
                    parent: Whiteboard);

            Erase =
                new("Erase", "Erase a complete object on a drawable",
                    Color.red, "Materials/ModernUIPack/Pencil",
                    EraseAction.CreateReversibleAction,
                    parent: Whiteboard);

            LinePointErase =
                new("Line point erase", "Erase a point from a line on a drawable",
                    Color.red, "Materials/ModernUIPack/Pencil",
                    LinePointEraseAction.CreateReversibleAction,
                    parent: Whiteboard);

            Cleaner =
                new("Cleaner", "Cleans a complete drawable",
                    Color.red.Darker(), "Materials/ModernUIPack/Pencil",
                    CleanerAction.CreateReversibleAction,
                    parent: Whiteboard);
        }

        public readonly static ActionStateType Move;
        public readonly static ActionStateType Rotate;
        public readonly static ActionStateType Hide;
        public readonly static ActionStateType NewEdge;
        public readonly static ActionStateType NewNode;
        public readonly static ActionStateType EditNode;
        public readonly static ActionStateType ScaleNode;
        public readonly static ActionStateType Delete;
        public readonly static ActionStateType ShowCode;
        public readonly static ActionStateType Draw;

        public readonly static ActionStateTypeGroup MetricBoard;
        public readonly static ActionStateType AddBoard;
        public readonly static ActionStateType AddWidget;
        public readonly static ActionStateType MoveBoard;
        public readonly static ActionStateType MoveWidget;
        public readonly static ActionStateType DeleteBoard;
        public readonly static ActionStateType DeleteWidget;
        public readonly static ActionStateType LoadBoard;
        public readonly static ActionStateType SaveBoard;

        public readonly static ActionStateTypeGroup Whiteboard;
        public readonly static ActionStateType DrawOn;
        public readonly static ActionStateType DrawShapes;
        public readonly static ActionStateType ColorPicker;
        public readonly static ActionStateType EditLine;
        public readonly static ActionStateType MoveRotator;
        public readonly static ActionStateType MovePoint;
        public readonly static ActionStateType LayerChanger;
        public readonly static ActionStateType LineSplit;
        public readonly static ActionStateType Erase;
        public readonly static ActionStateType LinePointErase;
        public readonly static ActionStateType Cleaner;

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
                const string None = "<NONE>";
                return type == null ? None : type.Name;
            }
        }
    }
}
