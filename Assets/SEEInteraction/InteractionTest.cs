using SEE;
using UnityEngine;

public class InteractionTest : MonoBehaviour
{
    public GameObject building;

    void Start()
    {
        DeleteBuildingInteraction dbi = new DeleteBuildingInteraction(building);
        dbi.Execute();
    }
}
