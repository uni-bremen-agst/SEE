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
            mark.SetWidth(0.002f, 0.0015f);
            mark.SetPosition(0, this.transform.position);
            mark.SetPosition(1, this.transform.position + new Vector3(0, 0.3f, 0));
            mark.enabled = false;
        }

        void Update()
        {
            AnnotatableObject annotatableObject = this.GetComponentInParent(typeof(AnnotatableObject)) as AnnotatableObject;
            if (annotatableObject.isAnnotated == true && !annotatableObject.GetEditorState())
            {
                mark.enabled = true;
                mark.SetPosition(0, this.transform.position);
                mark.SetPosition(1, this.transform.position + new Vector3(0, 0.3f, 0));
            }
            else
            {
                mark.enabled = false;
            }
        }
    }
}
