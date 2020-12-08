using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SEE.Controls;


namespace SEE.GO
{
    /// <summary>
    /// Implements interactions with an annoationEditor game object.
    /// </summary>
    public class AnnotationEditor : MonoBehaviour
    {
        public TMP_InputField annotationToAdd;
        public TMP_InputField annotationToEdit;
        private GameObject actor;

        /// <summary>
        /// Adds an annotation with the text of annotationToAdd.
        /// </summary>
        public void AddAnnotation()
        {
            AnnotatableObject annotatableObject = this.GetComponentInParent(typeof(AnnotatableObject)) as AnnotatableObject;
            new Net.AddAnnotationAction(annotatableObject, annotationToAdd.text).Execute();
            annotationToAdd.text = "";
        }

        /// <summary>
        /// Initiats the process of removing an annoation.
        /// </summary>
        public void RemoveAnnotatio()
        {
            AnnotatableObject annotatableObject = this.GetComponentInParent(typeof(AnnotatableObject)) as AnnotatableObject;
            annotatableObject.MakeAnnotationsClickable(true);
        }

        /// <summary>
        /// Initiats the process of editing an annoation.
        /// </summary>
        public void StartEditing()
        {
            AnnotatableObject annotatableObject = this.GetComponentInParent(typeof(AnnotatableObject)) as AnnotatableObject;
            annotatableObject.MakeAnnotationsClickable(false);
        }

        /// <summary>
        /// Edits an annotation with the text of annotationToEdit.
        /// </summary>
        public void EditAnnotation()
        {
            AnnotatableObject annotatableObject = this.GetComponentInParent(typeof(AnnotatableObject)) as AnnotatableObject;
            new Net.EditAnnotationAction(annotatableObject, annotationToEdit.text).Execute();
            annotationToEdit.text = "";
        }

        /// <summary>
        /// Closes the annoationEditor.
        /// </summary>
        public void QuitAnnotationEditor()
        {
            AnnotatableObject annotatableObject = this.GetComponentInParent(typeof(AnnotatableObject)) as AnnotatableObject;
            annotatableObject.CloseAnnotationEditor();
        }

        /// <summary>
        /// Deactives and actives movement via keybord in order to prefend movement while typing.
        /// </summary>
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
