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
        /// A list of all available ActionStateTypes as a tree. This list contains
        /// the roots of the forrest only. The descendants can be retrieved from
        /// those roots by traversing the tree. All elements in the tree can be
        /// derived at once via <see cref="AllRootTypes.Elements()"/>.
        /// </summary>
        public static Forrest<AbstractActionStateType> AllRootTypes { get; }
            = new Forrest<AbstractActionStateType>();

        /// <summary>
        /// Adds <paramref name="actionStateType"/> to the list of all action state
        /// types <see cref="AllRootTypes"/>.
        /// </summary>
        /// <param name="actionStateType">to be added</param>
        public static void Add(AbstractActionStateType actionStateType)
        {
            // Check for duplicates
            //if (AllTypes.Any(x => x.Name == actionStateType.Name))
            //{
            //    throw new ArgumentException($"Duplicate ActionStateTypes {actionStateType.Name} must not exist!");
            //}
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
            /// Important note: As a side effect of mentioning this <see cref="ActionStateType.Move"/>
            /// here, its initializer will be executed. C# has a lazy evaluation
            /// of initializers. This will make sure that it actually has a
            /// defined value (not <c>null</c>).
            return Move;
        }

        #region Static Types
        public static ActionStateType Move { get; } =
            new("Move", "Move a node within a graph",
                Color.red.Darker(), "Materials/Charts/MoveIcon",
                MoveAction.CreateReversibleAction);
        public static ActionStateType Rotate { get; } =
            new("Rotate", "Rotate the selected node and its children within a graph",
                Color.blue.Darker(), "Materials/ModernUIPack/Refresh",
                RotateAction.CreateReversibleAction);
        public static ActionStateType Hide { get; } =
            new("Hide", "Hides nodes or edges",
                Color.yellow.Darker(), "Materials/ModernUIPack/Eye", HideAction.CreateReversibleAction);
        public static ActionStateType NewEdge { get; } =
            new("New Edge", "Draw a new edge between two nodes",
                Color.green.Darker(), "Materials/ModernUIPack/Minus",
                AddEdgeAction.CreateReversibleAction);
        public static ActionStateType NewNode { get; } =
            new("New Node", "Create a new node",
                Color.green.Darker(), "Materials/ModernUIPack/Plus",
                AddNodeAction.CreateReversibleAction);
        public static ActionStateType EditNode { get; } =
            new("Edit Node", "Edit a node",
                Color.green.Darker(), "Materials/ModernUIPack/Settings",
                EditNodeAction.CreateReversibleAction);
        public static ActionStateType ScaleNode { get; } =
            new("Scale Node", "Scale a node",
                Color.green.Darker(), "Materials/ModernUIPack/Crop",
                ScaleNodeAction.CreateReversibleAction);
        public static ActionStateType Delete { get; } =
            new("Delete", "Delete a node or an edge",
                Color.yellow.Darker(), "Materials/ModernUIPack/Trash",
                DeleteAction.CreateReversibleAction);
        public static ActionStateType ShowCode { get; } =
            new("Show Code", "Display the source code of a node.",
                Color.black, "Materials/ModernUIPack/Document",
                ShowCodeAction.CreateReversibleAction);
        public static ActionStateType Draw { get; } =
            new("Draw", "Draw a line",
                 Color.magenta.Darker(), "Materials/ModernUIPack/Pencil",
                 DrawAction.CreateReversibleAction);
        public static ActionStateTypeGroup MetricBoard { get; } =
            new("Metric Board", "Manipulate a metric board",
                 Color.white.Darker(), "Materials/ModernUIPack/Pencil");
        public static ActionStateType AddBoard { get; } =
            new("Add Board", "Add a board",
                 Color.green.Darker(), "Materials/ModernUIPack/Plus",
                 AddBoardAction.CreateReversibleAction,
                 parent: MetricBoard);
        public static ActionStateType AddWidget { get; } =
            new("Add Widget", "Add a widget",
                Color.green.Darker(), "Materials/ModernUIPack/Plus",
                AddWidgetAction.CreateReversibleAction,
                parent: MetricBoard);
        public static ActionStateType MoveBoard { get; } =
            new("Move Board", "Move a board",
                Color.yellow.Darker(), "Materials/Charts/MoveIcon",
                MoveBoardAction.CreateReversibleAction,
                parent: MetricBoard);
        public static ActionStateType MoveWidget { get; } =
            new("Move Widget", "Move a widget",
                Color.yellow.Darker(), "Materials/Charts/MoveIcon",
                MoveWidgetAction.CreateReversibleAction,
                parent: MetricBoard);
        public static ActionStateType DeleteBoard { get; } =
            new("Delete Board", "Delete a board",
                Color.red.Darker(), "Materials/ModernUIPack/Trash",
                DeleteBoardAction.CreateReversibleAction,
                parent: MetricBoard);
        public static ActionStateType DeleteWidget { get; } =
            new("Delete Widget", "Delete a widget",
                Color.red.Darker(), "Materials/ModernUIPack/Trash",
                DeleteWidgetAction.CreateReversibleAction,
                parent: MetricBoard);
        public static ActionStateType LoadBoard { get; } =
            new("Load Board", "Load a board",
                Color.blue.Darker(), "Materials/ModernUIPack/Document",
                LoadBoardAction.CreateReversibleAction,
                parent: MetricBoard);
        public static ActionStateType SaveBoard { get; } =
            new("Save Board", "Save a board",
                Color.blue.Darker(), "Materials/ModernUIPack/Document",
                SaveBoardAction.CreateReversibleAction,
                parent: MetricBoard);

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
