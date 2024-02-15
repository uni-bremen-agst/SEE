using SEE.UI.RuntimeConfigMenu;

namespace SEE.Net.Actions.RuntimeConfig
{
    /// <summary>
    /// Network action when an attribute was changed.
    /// </summary>
    public class UpdateCityAttributeNetAction<E> : UpdateCityNetAction
    {
        /// <summary>
        /// The changed value
        /// </summary>
        public E Value;

        /// <summary>
        /// Triggers 'SyncField' on <see cref="RuntimeTabMenu"/>.
        /// </summary>
        public override void ExecuteOnClient()
        {
            RuntimeConfigMenu.GetMenuForCity(CityIndex).SyncField?.Invoke(WidgetPath, Value);
        }
    }
}
