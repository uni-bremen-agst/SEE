using SEE.Game.UI.RuntimeConfigMenu;

namespace SEE.Net.Actions.RuntimeConfig
{
    /// <summary>
    /// Network action when a list element was removed. 
    /// </summary>
    public class RemoveListElementNetAction : AbstractNetAction
    {
        /// <summary>
        /// City index
        /// </summary>
        public int CityIndex;
        
        /// <summary>
        /// Widget path
        /// </summary>
        public string WidgetPath;

        /// <summary>
        /// Does nothing on the server
        /// </summary>
        protected override void ExecuteOnServer()
        {
        }

        /// <summary>
        /// Triggers 'SyncRemoveListElement' on <see cref="RuntimeTabMenu"/>.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
                RuntimeConfigMenu.GetMenuForCity(CityIndex).SyncRemoveListElement?.Invoke(WidgetPath);
        }
    }
}