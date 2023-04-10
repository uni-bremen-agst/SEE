using SEE.Game.UI.RuntimeConfigMenu;
using SEE.Net.Actions;

public class UpdateCityMethodNetAction : AbstractNetAction
{
    public int CityIndex;
    public string MethodName;

    protected override void ExecuteOnServer()
    {
    }

    protected override void ExecuteOnClient()
    {
        if (!IsRequester())
            RuntimeConfigMenu.GetMenuForCity(CityIndex).OnSyncMethod?.Invoke(MethodName);
    }
}