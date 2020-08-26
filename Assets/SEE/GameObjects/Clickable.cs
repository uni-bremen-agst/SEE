using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using SEE.Controls;

namespace SEE.GO
{
    public class Clickable : MonoBehaviour
    {
        private bool delete;
        void Awake()
        {
            delete = false;
        }
        // Update is called once per frame
        void Update()
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit Hit;

            if (Input.GetMouseButtonDown(0))
            {
                if (Physics.Raycast(ray, out Hit))
                {
                    AnnotatableObject annotatableObject = this.GetComponentInParent(typeof(AnnotatableObject)) as AnnotatableObject;
                    if (delete == true)
                    {
                        new Net.RemoveAnnotationAction(annotatableObject, this.transform.parent.gameObject).Execute();
                    }
                    else
                    {
                        annotatableObject.StartEditing(this.transform.parent.gameObject);
                    }
                }
            }
        }

        public void SetDelete(bool delete)
        {
            this.delete = delete;
        }
    }
}
