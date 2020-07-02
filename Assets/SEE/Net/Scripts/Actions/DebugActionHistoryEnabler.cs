using UnityEngine;

public class DebugActionHistoryEnabler : MonoBehaviour
{
    public GameObject enablableGameObject;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            enablableGameObject.SetActive(!enablableGameObject.activeSelf);
        }
    }
}
