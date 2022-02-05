using System.Linq;

namespace SEE.Game.UI.Menu
{
    /// <summary>
    /// A menu in which the user can choose one active selection out of a menu.
    /// It is assumed that only one selection can be active at a time.
    /// </summary>
    public partial class SelectionMenu: SimpleMenu<ToggleMenuEntry>
    {
        protected override void OnEntrySelected(ToggleMenuEntry entry)
        {
            // Disable all entries except the selected one, this will automatically call DoExitAction()
            ActiveEntry = entry;
            // This will ensure that DoAction() is called on entry
            base.OnEntrySelected(entry);
        }

        /// <summary>
        /// Returns the first active entry in the <see cref="entries"/> list.
        /// If no entry is active, <c>null</c> will be returned.
        /// If this value is changed, all other entries will be set inactive.
        /// </summary>
        public ToggleMenuEntry ActiveEntry
        {
            get => entries.SingleOrDefault(entry => entry.Active);
            set => entries.ForEach(entry => entry.Active = entry == value);
        }
    }
}
