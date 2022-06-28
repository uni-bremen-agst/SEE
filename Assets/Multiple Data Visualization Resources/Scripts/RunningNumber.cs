using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class RunningNumber : MonoBehaviour
{
    public Text progress;
 
    float time = 0f;
    public float number;

    public float targetNum=30;
    public float timeCost=3;

     float timer1 = 0;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timer1 += Time.deltaTime;
         time += Time.deltaTime;
        float temp = number;

 
        if (timer1 > 0.1f)
        {
            number += targetNum / timeCost*0.1f ;
            progress.text =Mathf.RoundToInt( number).ToString();

            timer1 = 0f;
        }

        if (time > timeCost)
            {
                number = 0;
            temp=time = 0;
            }
         
    }
}
