using SEE.UI.RuntimeConfigMenu;

namespace SEE.Net.Actions.RuntimeConfig
{
    /// <summary>
    /// Network action when a list element was added.
    /// </summary>
    public class AddListElementNetAction : UpdateCityNetAction
    {
        /// <summary>
        /// Triggers 'SyncAddListElement' on <see cref="RuntimeTabMenu"/>.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            RuntimeConfigMenu.GetMenuForCity(CityIndex).SyncAddListElement?.Invoke(WidgetPath);
        }
    }
}
