using SEE.Game;
using SEE.Command;
using UnityEngine;

public class CommandTest : MonoBehaviour
{
    public GameObject seeCityGO;

    void Start()
    {
        LoadCityCommand lci = new LoadCityCommand(seeCityGO.GetComponent<SEECity>());
        lci.ExecuteLocally();
    }
}
