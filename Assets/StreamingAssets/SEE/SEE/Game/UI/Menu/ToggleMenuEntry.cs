using UnityEngine;
using UnityEngine.Events;

namespace SEE.Game.UI.Menu
{
    /// <summary>
    /// This class differs from a normal <see cref="MenuEntry"/> in that the entries represent states
    /// which can be on or off.
    /// </summary>
    public class ToggleMenuEntry: MenuEntry
    {
        /// <summary>
        /// The action to be taken when the entry is deselected.
        /// </summary>
        private readonly UnityAction DoExitAction;

        /// <summary>
        /// Whether this entry is currently active (i.e., toggled).
        /// </summary>
        private bool active;
        
        /// <summary>
        /// Whether this entry is currently active (i.e. toggled).
        /// Setting this value will automatically call <see cref="DoExitAction"/> on deactivation.
        /// Note that <see cref="DoAction"/> is <b>not</b> called and must be executed manually.
        /// </summary>
        public bool Active
        {
            set
            {
                if (!value && active)
                {
                    // Will be disabled only when deactivating for the first time
                    DoExitAction();
                }
                active = value;
                
            }
            get => active;
        }

        /// <summary>
        /// Instantiates and returns a new ToggleMenuEntry.
        /// </summary>
        /// <param name="active">Whether the entry should be active on creation.</param>
        /// <param name="entryAction">What action to take when the entry is selected.</param>
        /// <param name="exitAction">What action to take when the entry is deselected. May be <c>null</c>.</param>
        /// <param name="title">The title of the entry.</param>
        /// <param name="description">A description of the entry.</param>
        /// <param name="entryColor">The color with which this entry shall be displayed.</param>
        /// <param name="icon">The icon which will be displayed alongside this entry.</param>
        /// <param name="enabled">Whether this entry should be enabled on creation.</param>
        public ToggleMenuEntry(bool active, UnityAction entryAction, UnityAction exitAction, string title, 
                               string description = null, Color entryColor = default, Sprite icon = null,
                               bool enabled = true) 
            : base(entryAction, title, description, entryColor, enabled, icon)
        {
            this.active = active;
            DoExitAction = exitAction ?? (() => {});
        }
    }
}
