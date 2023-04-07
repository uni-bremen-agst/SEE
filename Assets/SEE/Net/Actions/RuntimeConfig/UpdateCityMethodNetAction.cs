using SEE.Game.UI.RuntimeConfigMenu;
using SEE.Net.Actions;

public class UpdateCityMethodNetAction : AbstractNetAction
{
    public int CityIndex;
    public string WidgetPath;
    
    protected override void ExecuteOnServer() { }

    protected override void ExecuteOnClient()
    {
        RuntimeConfigMenu.GetMenuForCity(CityIndex).OnSyncMethod?.Invoke(WidgetPath);
    }
}