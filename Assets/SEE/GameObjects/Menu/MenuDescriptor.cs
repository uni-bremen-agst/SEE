using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// A delegate to be called when a menu entry was selected.
    /// </summary>
    public delegate void EntryEvent();

    /// <summary>
    /// Describes a menu entry of the circular menu.
    /// </summary>
    public struct MenuDescriptor
    {
        /// <summary>
        /// The text that should appear for the menu entry. May be null or
        /// the empty string in which case no text will be shown.
        /// </summary>
        public readonly string Label;
        /// <summary>
        /// The callback to be called when the menu entry is selected.
        /// May be null if no callback is requested.
        /// </summary>
        public readonly EntryEvent EntryOn;
        /// <summary>
        /// The callback to be called when the menu entry is deselected.
        /// If the menu entry is transient, deselection will happen automatically
        /// after a certain amount of time. Even for automatic deselection, this
        /// callback will be triggered.
        /// May be null if no callback is requested.
        /// </summary>
        public readonly EntryEvent EntryOff;
        /// <summary>
        /// A transient menu entry is an entry that will become deselected automatically
        /// after a certain period of time (implicit deselection). Before this point in
        /// time, it can be deselected explicitly, too.
        /// </summary>
        public readonly bool IsTransient;
        /// <summary>
        /// The path of the prefab containing the sprite to be instantiated for this
        /// menu entry. This must be a circular sprite.
        /// </summary>
        public readonly string SpriteFile;
        /// <summary>
        /// The color of the sprite to be used when the menu entry is activated.
        /// </summary>
        public readonly Color ActiveColor;
        /// <summary>
        /// The color of the sprite to be used when the menu entry is inactivate.
        /// </summary>
        public readonly Color InactiveColor;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="label"> The text that should appear for the menu entry. May be null or
        /// the empty string in which case no text will be shown.</param>
        /// <param name="spriteFile">The path of the prefab containing the sprite to be instantiated for this
        /// menu entry. This must be a circular sprite.</param>
        /// <param name="activeColor">The color of the sprite to be used when the menu entry is activated.</param>
        /// <param name="inactiveColor">The color of the sprite to be used when the menu entry is inactivate.</param>
        /// <param name="entryOn">The callback to be called when the menu entry is deselected.
        /// If the menu entry is transient, deselection will happen automatically
        /// after a certain amount of time. Even for automatic deselection, this
        /// callback will be triggered.
        /// May be null if no callback is requested.</param>
        /// <param name="entryOff">The callback to be called when the menu entry is deselected.
        /// If the menu entry is transient, deselection will happen automatically
        /// after a certain amount of time. Even for automatic deselection, this
        /// callback will be triggered.
        /// May be null if no callback is requested.</param>
        /// <param name="isTransient">A transient menu entry is an entry that will become deselected automatically
        /// after a certain period of time (implicit deselection). Before this point in
        /// time, it can be deselected explicitly, too.</param>
        public MenuDescriptor
            (string label,
            string spriteFile,
            Color activeColor,
            Color inactiveColor,
            EntryEvent entryOn,
            EntryEvent entryOff, 
            bool isTransient)
        {
            Label = label;
            EntryOn = entryOn;
            EntryOff = entryOff;
            IsTransient = isTransient;
            SpriteFile = spriteFile;
            ActiveColor = activeColor;
            InactiveColor = inactiveColor;
        }
    }
}