using SEE.Utils;
using UnityEngine;

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
            new ActionStateType("Move", "Move a node within a graph",
                                Color.red.Darker(), "Materials/Charts/MoveIcon",
                                MoveAction.CreateReversibleAction);
        public static ActionStateType Rotate { get; } =
            new ActionStateType("Rotate", "Rotate everything around the selected node within a graph",
                                Color.blue.Darker(), "Materials/ModernUIPack/Refresh",
                                RotateAction.CreateReversibleAction);
        public static ActionStateTypeGroup Hide { get; } =
            new ActionStateTypeGroup("Hide", "Hides nodes or edges",
                                     Color.yellow.Darker(), "Materials/ModernUIPack/Eye");

        public static ActionStateType HideConnectedEdges { get; } =
            new ActionStateType("Hide Connected Edges", "Hides connected edges",
                                Color.yellow.Darker(), "Materials/ModernUIPack/Eye",
                                HideNodeAction.CreateReversibleAction,
                                Hide);
        public static ActionStateType NewEdge { get; } =
            new ActionStateType("New Edge", "Draw a new edge between two nodes",
                                Color.green.Darker(), "Materials/ModernUIPack/Minus",
                                AddEdgeAction.CreateReversibleAction);
        public static ActionStateType NewNode { get; } =
            new ActionStateType("New Node", "Create a new node",
                                Color.green.Darker(), "Materials/ModernUIPack/Plus",
                                AddNodeAction.CreateReversibleAction);
        public static ActionStateType EditNode { get; } =
            new ActionStateType("Edit Node", "Edit a node",
                                Color.green.Darker(), "Materials/ModernUIPack/Settings",
                                EditNodeAction.CreateReversibleAction);
        public static ActionStateType ScaleNode { get; } =
            new ActionStateType("Scale Node", "Scale a node",
                                Color.green.Darker(), "Materials/ModernUIPack/Crop",
                                ScaleNodeAction.CreateReversibleAction);
        public static ActionStateType Delete { get; } =
            new ActionStateType("Delete", "Delete a node or an edge",
                                Color.yellow.Darker(), "Materials/ModernUIPack/Trash",
                                DeleteAction.CreateReversibleAction);
        public static ActionStateType ShowCode { get; } =
            new ActionStateType("Show Code", "Display the source code of a node.",
                                Color.black, "Materials/ModernUIPack/Document",
                                ShowCodeAction.CreateReversibleAction);
        public static ActionStateType Draw { get; } =
            new ActionStateType("Draw", "Draw a line",
                                Color.magenta.Darker(), "Materials/ModernUIPack/Pencil",
                                DrawAction.CreateReversibleAction);
        public static ActionStateType MetricBoard { get; } =
            new ActionStateType("Metric Board", "Configure Metric Boards",
                                Color.cyan, "Materials/40+ Simple Icons - Free/Mixer_Simple_Icons_UI.png",
                                MetricBoardAction.CreateReversibleAction);

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
