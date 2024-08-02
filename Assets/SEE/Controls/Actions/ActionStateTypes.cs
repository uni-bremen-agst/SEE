using SEE.Utils;
using UnityEngine;
using SEE.Controls.Actions.HolisticMetrics;

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
                  Color.red.Darker(), Icons.Move,
                  MoveAction.CreateReversibleAction);

            Rotate =
              new("Rotate", "Rotate the selected node and its children within a graph",
                  Color.blue.Darker(), Icons.Rotate,
                  RotateAction.CreateReversibleAction);

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

            ScaleNode =
              new("Scale Node", "Scale a node",
                  Color.green.Darker(), Icons.Scale,
                  ScaleNodeAction.CreateReversibleAction);

            Delete =
              new("Delete", "Delete a node or an edge",
                  Color.yellow.Darker(), Icons.Trash,
                  DeleteAction.CreateReversibleAction);

            ShowCode =
              new("Show Code", "Display the source code of a node.",
                  Color.black, Icons.Code,
                  ShowCodeAction.CreateReversibleAction);

            Draw =
              new("Draw", "Draw a line",
                  Color.magenta.Darker(), Icons.Pencil,
                  DrawAction.CreateReversibleAction);

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
