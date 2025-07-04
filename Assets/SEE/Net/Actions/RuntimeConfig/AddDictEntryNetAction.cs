using SEE.UI.RuntimeConfigMenu;

namespace SEE.Net.Actions.RuntimeConfig
{
    /// <summary>
    /// Network action when a dictionary entry was added.
    /// </summary>
    public class AddDictEntryNetAction : UpdateCityNetAction
    {
        /// <summary>
        /// The key to be added.
        /// </summary>
        public string Key;

        /// <summary>
        /// The name of the value type for which an object should be created.
        /// </summary>
        public string ValueType;

        /// <summary>
        /// Triggers 'SyncAddDictEntry' on <see cref="RuntimeTabMenu"/>.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            RuntimeConfigMenu.GetMenuForCity(CityIndex).SyncAddDictEntry?.Invoke(WidgetPath, Key, ValueType);
        }
    }
}
