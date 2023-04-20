using SEE.Game.UI.RuntimeConfigMenu;

namespace SEE.Net.Actions.RuntimeConfig
{
    public class AddListElementNetAction : AbstractNetAction
    {
        public int CityIndex;
        public string WidgetPath;

        protected override void ExecuteOnServer()
        {
        }

        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
                RuntimeConfigMenu.GetMenuForCity(CityIndex).OnSyncAddListElement?.Invoke(WidgetPath);
        }
    }
}