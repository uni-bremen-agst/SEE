using System;

using System.Collections;

using System.Collections.Generic;

using UnityEngine;



public class DialogueCanvas : MonoBehaviour

{

    //variable to store canvas

   // public Behaviour Dialogue_Canvas;





    private void Update()

    {

        if (Input.GetKeyDown(KeyCode.F3))

        {

            //disables/enables the canvas
            for (int i=0;i<=5;i++)
            {
                transform.GetChild(i).gameObject.SetActive(!transform.GetChild(i).gameObject.activeSelf);
            }
            //transform.GetChild(0).gameObject.SetActive(!transform.GetChild(0).gameObject.activeSelf);
            //Dialogue_Canvas.enabled = !Dialogue_Canvas.enabled;

        }

    }

}
