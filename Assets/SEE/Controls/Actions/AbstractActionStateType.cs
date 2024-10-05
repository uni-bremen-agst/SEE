using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Super class of <see cref="ActionStateType"/> and <see cref="ActionStateTypeGroup"/>.
    /// </summary>
    public abstract class AbstractActionStateType
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
        /// The FontAwesome codepoint of the icon for this action. See <see cref="Icons"/> for more information.
        /// The icon itself should be a visual representation of the action.
        /// Will be used in the <see cref="DesktopMenu"/>.
        /// </summary>
        public char Icon { get; }

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
        /// <param name="icon">The icon which shall be displayed alongside this entry,
        /// given as a FontAwesome codepoint.</param>
        /// <param name="group">The group this action state type belongs to; may be null.</param>
        /// <param name="register">If true, this action state type will be registered in <see cref="ActionStateTypes"/>.</param>
        protected AbstractActionStateType(string name, string description, Color color, char icon,
                                          ActionStateTypeGroup group, bool register)
        {
            Name = name;
            Description = description;
            Color = color;
            Icon = icon;
            group?.Add(this);
            if (register)
            {
                ActionStateTypes.Add(this);
            }
        }
    }
}
