using System;
using UnityEngine;

namespace SEE.Game.UI.PropertyDialog
{
    /// <summary>
    /// A property that can be set by the user in a dialog.
    /// </summary>
    public abstract class Property : PlatformDependentComponent
    {
        /// <summary>
        /// The name of the property.
        /// </summary>
        public string Name;
        /// <summary>
        /// The description of the property.
        /// </summary>
        public string Description;

        /// <summary>
        /// Sets the parent of this property to <paramref name="parent"/>.
        /// </summary>
        /// <param name="parent">new parent of the property</param>
        public abstract void SetParent(GameObject parent);

        /// <summary>
        /// Will be called just before the dialog containing this property
        /// is shown to the user. Subclasses can place their preparation
        /// for this event there.
        /// </summary>
        public virtual void GetReady() {}
    }

    /// <summary>
    /// A property with a value that can be changed in a <see cref="PropertyDialog"/>.
    /// </summary>
    /// <typeparam name="T">The type of the property's value.</typeparam>
    public abstract class Property<T> : Property
    {
        /// <summary>
        /// The value of the property.
        /// </summary>
        public abstract T Value { get; set; }
    }
}