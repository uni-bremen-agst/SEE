using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SEE.Controls
{
    /// <summary>
    /// Adds a mark to an annoatableObject.
    /// </summary>
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

        /// <summary>
        /// Activates the mark, if the object is annotated and the annoationEditor of the object is not open.
        /// </summary>
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
