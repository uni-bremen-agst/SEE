using System.Collections.Generic;
using SEE.Controls;

namespace SEE.Game.UI
{
    /// <summary>
    /// Represents a menu of various actions the user can choose from.
    /// The Menu can consists of multiple MenuEntries of the type <paramref name="T"/>
    /// and can have multiple representations depending on the platform used.
    /// </summary>
    /// <typeparam name="T">the type of entries used. Must be derived from <see cref="MenuEntry"/>.</typeparam>
    /// <seealso cref="MenuEntry"/>
    public class Menu<T>: RenderableComponent where T : MenuEntry
    {
        /// <summary>
        /// A list of menu entries for this menu.
        /// </summary>
        /// <seealso cref="MenuEntry"/>
        protected readonly IList<T> entries = new List<T>();

        /// <summary>
        /// Adds an <paramref name="entry"/> to this menu's <see cref="entries"/>.
        /// This method must be called <i>before</i> this component's Awake() method has been called.
        /// </summary>
        /// <param name="entry">The entry to add to this menu.</param>
        public void AddEntry(T entry)
        {
            entries.Add(entry);
        }
        
        protected override bool RenderComponent(PlayerSettings.PlayerInputType inputType)
        {
            switch (inputType)
            {
                //TODO: Implement these
                case PlayerSettings.PlayerInputType.Desktop: return false;
                case PlayerSettings.PlayerInputType.TouchGamepad: return false;
                case PlayerSettings.PlayerInputType.VR: return false;
                case PlayerSettings.PlayerInputType.HoloLens: return false;
                case PlayerSettings.PlayerInputType.None: return true;  // no UI has to be rendered
                default: return false;
            }
        }

        /// <summary>
        /// Called when an entry in the menu is selected.
        /// </summary>
        /// <param name="entry">The entry which was selected.</param>
        public virtual void OnEntrySelected(T entry)
        {
            entry.DoAction();
        }
    }
}