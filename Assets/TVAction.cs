using UnityEngine;

public class DialogueCanvas : MonoBehaviour
{
    /// <summary>
    /// A Toggle to switch the Browser on and off with the key F3.
    /// </summary>

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
        {
            //disables/enables the Browser
            for (int i=0;i<=5;i++)
            {
                transform.GetChild(i).gameObject.SetActive(!transform.GetChild(i).gameObject.activeSelf);
            }
        }
    }
}
