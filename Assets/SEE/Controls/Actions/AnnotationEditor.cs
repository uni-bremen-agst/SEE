using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SEE.Controls;


namespace SEE.GO
{
    public class AnnotationEditor : MonoBehaviour
    {
        public TMP_InputField annotationToAdd;
        public TMP_InputField annotationToEdit;
        private GameObject actor;

        public void AddAnnotation()
        {
            AnnotatableObject annotatableObject = this.GetComponentInParent(typeof(AnnotatableObject)) as AnnotatableObject;
            new Net.AddAnnotationAction(annotatableObject, annotationToAdd.text).Execute();
            annotationToAdd.text = "";
        }

        public void RemoveAnnotatio()
        {
            AnnotatableObject annotatableObject = this.GetComponentInParent(typeof(AnnotatableObject)) as AnnotatableObject;
            annotatableObject.MakeAnnotationsClickable(true);
            annotatableObject.ShowInformation();
        }

        public void StartEditing()
        {
            AnnotatableObject annotatableObject = this.GetComponentInParent(typeof(AnnotatableObject)) as AnnotatableObject;
            annotatableObject.MakeAnnotationsClickable(false);
        }

        public void EditAnnotation()
        {
            AnnotatableObject annotatableObject = this.GetComponentInParent(typeof(AnnotatableObject)) as AnnotatableObject;
            new Net.EditAnnotationAction(annotatableObject, annotationToEdit.text).Execute();
            annotationToEdit.text = "";
        }

        public void QuitAnnotationEditor()
        {
            AnnotatableObject annotatableObject = this.GetComponentInParent(typeof(AnnotatableObject)) as AnnotatableObject;
            annotatableObject.CloseAnnotationEditor();
        }

        public void Moveable()
        {
            if (actor == null)
            {
                GameObject[] cameras = GameObject.FindGameObjectsWithTag("MainCamera");
                foreach (GameObject gameObject in cameras)
                {
                    if (gameObject.activeSelf == true)
                    {
                        actor = gameObject;
                    }
                }
            }
            actor.GetComponent<DesktopCameraAction>().enabled = !actor.GetComponent<DesktopCameraAction>().enabled;
            actor.GetComponent<Selection2DAction>().enabled = !actor.GetComponent<Selection2DAction>().enabled;
        }

    }
}
