 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class SimpleProgress : MonoBehaviour
{

    public bool b = true;
    public Image image;
    public float speed;
    float time = 0f;

    public void Start()
    {
        speed = Random.Range(0.2f, 0.5f);
        image = GetComponent<Image>();
    }

    void Update()
    {
        if (b)
        {
            time += Time.deltaTime * speed;
            image.fillAmount = time;
  

            if (time > 1)
            {

                time = 0;
            }
        }
    }


}
