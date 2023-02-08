using System;
using UnityEngine;

namespace SEE.Controls.Actions
{
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
}
