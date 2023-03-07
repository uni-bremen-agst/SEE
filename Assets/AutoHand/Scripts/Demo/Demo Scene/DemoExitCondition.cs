using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DemoExitCondition : MonoBehaviour
{
    void Update(){
        if (Input.GetKeyDown(KeyCode.Space)){
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        if (Input.GetKeyDown(KeyCode.Escape)){
            Application.Quit();
        }
    }
}
