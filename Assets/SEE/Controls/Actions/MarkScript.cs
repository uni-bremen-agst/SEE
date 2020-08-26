using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SEE.Controls
{
    public class MarkScript : MonoBehaviour
    {
        private LineRenderer mark;

        void Start()
        {
            this.gameObject.AddComponent<LineRenderer>();
            mark = this.gameObject.GetComponent<LineRenderer>();
            mark.material = new Material(Shader.Find("Unlit/Texture"));
            mark.SetColors(Color.blue, Color.blue); //@ToDO funktioniert nicht?
            mark.material.color = Color.blue; // -"-
            mark.SetWidth(0.0005f, 0.0005f);
            mark.SetPosition(0, this.transform.position);
            mark.SetPosition(1, this.transform.position + new Vector3(0, 0.1f, 0));
            mark.enabled = false;
        }

        void Update()
        {
            AnnotatableObject annotatableObject = this.GetComponentInParent(typeof(AnnotatableObject)) as AnnotatableObject;
            if (annotatableObject.isAnnotated == true && !annotatableObject.GetEditorState())
            {
                mark.enabled = true;
            }
            else
            {
                mark.enabled = false;
            }
        }
    }
}
