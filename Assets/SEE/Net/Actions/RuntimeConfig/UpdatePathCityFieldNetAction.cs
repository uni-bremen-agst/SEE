using SEE.Game.UI.RuntimeConfigMenu;

namespace SEE.Net.Actions.RuntimeConfig
{
    public class UpdatePathCityFieldNetAction : AbstractNetAction
    {
        public int CityIndex;
        public string WidgetPath;
        public string Value;
        public bool IsAbsolute; 

        protected override void ExecuteOnServer()
        {
        }

        protected override void ExecuteOnClient()
        {
            if (!IsRequester()) 
                RuntimeConfigMenu.GetMenuForCity(CityIndex).OnSyncPath?.Invoke(WidgetPath, Value, IsAbsolute);
        }
    }
}
