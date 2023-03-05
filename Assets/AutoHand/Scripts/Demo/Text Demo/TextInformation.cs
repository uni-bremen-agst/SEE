using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextInformation : MonoBehaviour{
    public GameObject activateImage;
    public GameObject deactivateImage;
    public GameObject[] texts;


    bool active;
    
    public void ActivateText() {
        active = true;
        foreach(var text in texts) {
            text.SetActive(active);
        }
        
        activateImage.SetActive(true);
        deactivateImage.SetActive(false);
    }


    public void DeactivateText() {
        active = false;
        foreach(var text in texts) {
            text.SetActive(active);
        }
        
        activateImage.SetActive(false);
        deactivateImage.SetActive(true);
    }

    public void ToggleText() {
        if(active)
            DeactivateText();
        else
            ActivateText();
    }
}
