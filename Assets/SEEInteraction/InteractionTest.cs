using SEE.Game;
using SEE.Interact;
using UnityEngine;

public class InteractionTest : MonoBehaviour
{
    public GameObject seeCityGO;

    void Start()
    {
        LoadCityInteraction lci = new LoadCityInteraction(seeCityGO.GetComponent<SEECity>());
        lci.Execute();
    }
}
