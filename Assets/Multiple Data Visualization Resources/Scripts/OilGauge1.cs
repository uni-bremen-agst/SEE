 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class OilGauge1 : MonoBehaviour
{

    public bool b = true;
    public Image image;
    public float speed = 0.5f;

    float time = 0f;

    public Text progress;

    public Transform oilOilGaugePivot;

    void Update()
    {
        if (b)
        {
            time += Time.deltaTime * speed;

            image.fillAmount = time;

 
            oilOilGaugePivot.localEulerAngles = Vector3.forward*(90- 180 * image.fillAmount);

            if (progress)
            {
                progress.text = ((int)(image.fillAmount * 100)).ToString();

            }

            if (time > 1)
            {

                time = 0;
            }
        }
    }


}
