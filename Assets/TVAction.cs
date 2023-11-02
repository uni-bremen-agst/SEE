using UnityEngine;

public class DialogueCanvas : MonoBehaviour
{
    /// <summary>
    /// A Toggle to switch the Browser on and off with the key F3.
    /// </summary>
    public GameObject Browser;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
        {
            //disables/enables the Browser
            Browser.SetActive(!Browser.activeSelf);
            //disables/enables the Buttons
            for (int i=0;i<=4;i++)
            {
                transform.GetChild(i).gameObject.SetActive(!transform.GetChild(i).gameObject.activeSelf);
            }
        }
    }
}
