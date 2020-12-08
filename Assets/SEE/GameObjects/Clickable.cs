using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using SEE.Controls;

namespace SEE.GO
{
/// <summary>
/// Controlls the click intaction between mouse and annoations.
/// </summary>
    public class Clickable : MonoBehaviour
    {

        void Update()
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit Hit;

            if (Input.GetMouseButtonDown(0))
            {
                if (Physics.Raycast(ray, out Hit))
                {

                    AnnotatableObject annotatableObject = this.GetComponentInParent(typeof(AnnotatableObject)) as AnnotatableObject;
                    annotatableObject.AnnotationClicked(this.transform.parent.gameObject);
                }
            }
        }
    }
}
