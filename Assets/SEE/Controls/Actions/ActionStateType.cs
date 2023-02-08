using System;
using System.Collections.Generic;
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
                                HideConnectedEdgesAction.CreateReversibleAction,
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

    /// <summary>
    /// A state action that can be added to the PlayerMenu.
    /// Super class of <see cref="ActionStateType"/> and <see cref="ActionStateTypeGroup"/>.
    /// </summary>
    public class AbstractActionStateType
    {
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
        /// The parent of this action state type, i.e., the <see cref="ActionStateTypeGroup"/>
        /// this action state type belongs to. May be null if this action state type is not
        /// nested in a <see cref="ActionStateTypeGroup"/>.
        /// </summary>
        /// <remarks>Do not use "set". It is public here only because of C# restrictions.
        /// It must be used only within <see cref="ActionStateTypeGroup.Add(AbstractActionStateType)"/>.
        /// </remarks>
        public ActionStateTypeGroup Parent { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The Name of this ActionStateType. Must be unique.</param>
        /// <param name="description">Description for this ActionStateType.</param>
        /// <param name="color">Color for this ActionStateType.</param>
        /// <param name="iconPath">Path to the material of the icon for this ActionStateType.</param>
        /// <param name="parent">The parent of this action state type. Can be <c>null</c>, in
        /// which case this action type is considered a root at the top level of the PlayerMenu.</param>
        /// <exception cref="ArgumentException">When the given <paramref name="name"/> is not unique.
        /// </exception>
        /// <remarks>Calling this constructor will add this action type to
        /// <see cref="ActionStateTypes.AllRootTypes"/>.</remarks>
        protected AbstractActionStateType(string name, string description, Color color, string iconPath,
                                          ActionStateTypeGroup parent)
        {
            Name = name;
            Description = description;
            Color = color;
            IconPath = iconPath;
            Parent = parent;
            parent?.Add(this);
            ActionStateTypes.Add(this);
        }
    }

    /// <summary>
    /// A group of other <see cref="AbstractActionStateType"/>s. It is not itself
    /// executable but just serves as a container for nested
    /// <see cref="AbstractActionStateType"/>s. In terms of menu, it represents
    /// a submenu.
    /// </summary>
    public class ActionStateTypeGroup : AbstractActionStateType
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The Name of this ActionStateType. Must be unique.</param>
        /// <param name="description">Description for this ActionStateType.</param>
        /// <param name="color">Color for this ActionStateType.</param>
        /// <param name="iconPath">Path to the material of the icon for this ActionStateType.</param>
        /// <param name="parent">The parent of this action state type. Can be <c>null</c>, in
        /// which case this action type is considered a root at the top level of the PlayerMenu.</param>
        /// <exception cref="ArgumentException">When the given <paramref name="name"/> is not unique.
        /// </exception>
        /// <remarks>Calling this constructor will add this action type to
        /// <see cref="ActionStateTypes.AllRootTypes"/>.</remarks>
        public ActionStateTypeGroup(string name, string description, Color color, string iconPath,
                                    ActionStateTypeGroup parent = null)
            : base(name, description, color, iconPath, parent)
        {
        }

        /// <summary>
        /// Ordered list of child action state types.
        /// </summary>
        public IList<AbstractActionStateType> Children = new List<AbstractActionStateType>();

        /// <summary>
        /// Adds <paramref name="actionStateType"/> as a member of this group.
        /// </summary>
        /// <param name="actionStateType">child to be added</param>
        public void Add(AbstractActionStateType actionStateType)
        {
            Children.Add(actionStateType);
        }

        #region Equality & Comparators

        public override bool Equals(object obj)
        {
            // TODO: Is this re-definition actually needed?
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }
            ActionStateTypeGroup other = (ActionStateTypeGroup)obj;
            if (Children.Count != other.Children.Count)
            {
                return false;
            }
            for (int i = 0; i < Children.Count; i++)
            {
                if (!Children[i].Equals(other.Children[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            // TODO: Is this re-definition actually needed?
            int result = Name.GetHashCode() + Description.GetHashCode();
            foreach (AbstractActionStateType child in Children)
            {
                result += child.GetHashCode();
            }
            return result;
        }
        #endregion
    }

    /// <summary>
    /// The type of a state-based action that can be executed. In terms
    /// of the PlayerMenu it is a leaf of the menu.
    /// </summary>
    public class ActionStateType : AbstractActionStateType
    {
        /// <summary>
        /// Delegate to be called to create a new instance of this kind of action.
        /// May be null if none needs to be created (in which case this delegate will
        /// not be called).
        /// </summary>
        public CreateReversibleAction CreateReversible { get; }

        /// <summary>
        /// Constructor for ActionStateType.
        /// Because this class replaces an enum, values of this class may only be created inside of it,
        /// hence the visibility modifier is set to private.
        /// </summary>
        /// <param name="name">The Name of this ActionStateType. Must be unique.</param>
        /// <param name="description">Description for this ActionStateType.</param>
        /// <param name="parent">The parent of this action in the nesting hierarchy in the menu.</param>
        /// <param name="color">Color for this ActionStateType.</param>
        /// <param name="iconPath">Path to the material of the icon for this ActionStateType.</param>
        /// <param name="createReversible">The delegate to be called when the action has finished
        /// and a new instance needs to be created to continue.</param>
        /// <param name="parent">The parent of this action state type. Can be <c>null</c>, in
        /// which case this action type is considered a root at the top level of the PlayerMenu.</param>
        /// <exception cref="ArgumentException">When the given <paramref name="name"/> is not unique.
        /// </exception>
        /// <remarks>Calling this constructor will add this action type to
        /// <see cref="ActionStateTypes.AllRootTypes"/>.</remarks>
        public ActionStateType(string name, string description, Color color, string iconPath,
                               CreateReversibleAction createReversible,
                               ActionStateTypeGroup parent = null)
            : base(name, description, color, iconPath, parent)
        {
            CreateReversible = createReversible;
        }

        #region Equality & Comparators

        public override bool Equals(object obj)
        {
            // TODO: Is this re-definition actually needed?
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() && ((ActionStateType)obj).CreateReversible == CreateReversible;
        }

        public override int GetHashCode()
        {
            // TODO: Is this re-definition actually needed?
            return CreateReversible.GetHashCode();
        }

        #endregion
    }
}
