using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageBoard : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        foreach (Image item in GetComponentsInChildren<Image>())
        {
            item.alphaHitTestMinimumThreshold = 0.1f;
        }


         
    
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
