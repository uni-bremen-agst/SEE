using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuitGame : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Quit()
    {
    if (UnityEditor.EditorApplication.isPlaying == false) {
            Application.Quit();
        } else
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }

}
