using SEE.GO;
using SEE.Utils;
using SEE.DataModel;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
{
    /// <summary>
    /// Implements interactions with a annotatable game object.
    /// </summary>
    public class AnnotatableObject : GrabbableObject
    {
        /// <summary>
        /// True if the object is currently annotated.
        /// </summary>
        public bool isAnnotated = false;

        /// <summary>
        /// List of Annoations.
        /// </summary>
        public List<GameObject> annotations;


        private GameObject annotationEditor = null;

        private static GameObject annotationEditorPrefab;

        /// <summary>
        /// True if the annoationEditor is currently open.
        /// </summary>
        private bool editorOpen = false;

        /// <summary>
        /// True if the current interaction is deleting.
        /// </summary>
        private bool delete = false;


        private GameObject annotationToEdit;

        /// <summary>
        /// Pointer objext for annoationEditor.
        /// </summary>
        private GameObject guiPointer;

        /// <summary>
        /// True if annoations are allowed.
        /// </summary>
        private bool isAnnotatable;

        protected override void Awake()
        {
            base.Awake();
            if (annotationEditorPrefab == null)
            {
                // Filename of the prefab for the annoation editor excluding its file extension .prefab
                string path = "Prefabs/AnnotationEditor3D";
                annotationEditorPrefab = Resources.Load<GameObject>(path);
                if (annotationEditorPrefab == null)
                {
                    Debug.LogErrorFormat("Prefab {0} not found.\n", path);
                }
            }
            guiPointer = GameObject.FindWithTag("GUICamera");
        }

        public void OpenAnnotationEditor()
        {
            if (isAnnotatable)
            {
                if (annotationEditor == null)
                {
                    annotationEditor = Instantiate(annotationEditorPrefab, Vector3.zero, Quaternion.identity);
                    annotationEditor.transform.SetParent(gameObject.transform);
                    if (guiPointer.transform.root.gameObject.activeSelf)
                    {
                        annotationEditor.GetComponent<Canvas>().worldCamera = guiPointer.GetComponent<Camera>();
                    }
                    Vector3 position = transform.position;
                    position.y = BoundingBox.GetRoof(GameObjectHierarchy.Descendants(gameObject, Tags.Node)) + annotationEditor.GetComponent<RectTransform>().rect.height / 2.0f;
                    annotationEditor.transform.position = position;
                    annotationEditor.SetActive(true);
                    SwitchGUIPointer(true);
                    guiPointer.GetComponent<Pointer>().SetSelectionState(Pointer.SelectionState.UI, this);
                }
                else
                {
                    ResetAnnotationEditor();
                    HideInformation();
                    base.ShowInformation();
                    annotationEditor.SetActive(true);
                    SwitchGUIPointer(true);
                    guiPointer.GetComponent<Pointer>().SetSelectionState(Pointer.SelectionState.UI, this);
                }
            }
            else
            {
                editorOpen = false;
            }
        }

        public void CloseAnnotationEditor()
        {
            editorOpen = false;
            annotationEditor.SetActive(false);
            ShowInformation();
            MakeAnnotationsUnClickable();
            SwitchGUIPointer(false);
            guiPointer.GetComponent<Pointer>().SetSelectionState(Pointer.SelectionState.None, this);
        }

        /// <summary>
        /// Actives and deactives the ray for the interactions with the annoation editor in VR.
        /// </summary>
        /// <param name="state">the new state of the GUIPointer</param>
        private void SwitchGUIPointer(bool state)
        {
            guiPointer.GetComponent<LineRenderer>().enabled = state;
            guiPointer.GetComponent<Pointer>().enabled = state;
            guiPointer.transform.GetChild(0).gameObject.SetActive(state);
        }

        /// <summary>
        /// Resets the annoation editor to its main menu.
        /// </summary>
        private void ResetAnnotationEditor()
        {
            foreach (Transform child in annotationEditor.transform)
            {
                child.gameObject.SetActive(false);
                if (child.gameObject.name == "EditorMenu")
                {
                    child.gameObject.SetActive(true);
                }
            }
        }

        /// <summary>
        /// Adds the given <paramref name="annotation"/> the object.
        /// </summary>
        /// <param name="annotation">the text of the annotation to be added</param>
        public void Annotate(string annotation)
        {
            GameObject annotationOnPaper = Instantiate(textOnPaperPrefab, Vector3.zero, Quaternion.identity);
            annotationOnPaper.name = "Annotation";
            annotationOnPaper.GetComponent<TextGUIAndPaperResizer>().Text = ResizeAnnotation(annotation);

            // Now textOnPaper has been re-sized properly; so we can derive its absolute height.
            float paperHeight = TextGUIAndPaperResizer.Height(annotationOnPaper);

            // We want to put the annoation above the roof of the gameObject. The gameObject,
            // however, could be composed of multiple child objects of different height
            // (e.g., an inner node which typically has a very low height because it is
            // visualized as an area, but the area contains many child objects).
            // That is why we gather the roof of the complete object hierarchy rooted
            // by gameObject.
            Vector3 position = transform.position; // absolute world co-ordinates of center
            if (annotations.Count > 0)
            {
                List<GameObject> texts = new List<GameObject>();
                foreach (GameObject game in annotations)
                {
                    texts.Add(game.transform.Find("Text").gameObject);
                }
                position.y = BoundingBox.GetRoof(texts) + paperHeight / 1.3f;

            }
            else
            {
                List<GameObject> texts = new List<GameObject>();
                texts.Add(textOnPaper.transform.Find("Text").gameObject);
                position.y = BoundingBox.GetRoof(texts) + paperHeight / 1.3f;
            }
            annotationOnPaper.transform.position = position;

            annotationOnPaper.transform.SetParent(gameObject.transform);
            GameObject text = annotationOnPaper.transform.Find("Text").gameObject;
            text.AddComponent<Clickable>();
            text.GetComponent<Clickable>().enabled = false;
            annotations.Add(annotationOnPaper);
            isAnnotated = true;
        }

        /// <summary>
        /// Changes the annoationToEdit text to the given <paramref name="annotation"/>.
        /// </summary>
        /// <param name="annotation">the new annotation text</param>
        public void EditAnnotation(string annotation)
        {
            CloseAnnotationEditor();
            annotationToEdit.GetComponent<TextGUIAndPaperResizer>().Text = ResizeAnnotation(annotation);
            TextGUIAndPaperResizer.Height(annotationToEdit);
        }

        /// <summary>
        /// Sets the annotaitonToEdit to the given <paramref name="annotation"/> object.
        /// </summary>
        /// <param name="annotation">the annotation to be edited</param>
        public void StartEditing(GameObject annotation)
        {
            HideInformation();
            SwitchGUIPointer(true);
            base.ShowInformation();
            annotationToEdit = annotation;

            GameObject editingInput = annotationEditor.transform.Find("Editing/EditingInput").gameObject;
            editingInput.GetComponent<TMP_InputField>().text = annotationToEdit.GetComponent<TextGUIAndPaperResizer>().Text.Replace(System.Environment.NewLine, " ");
            guiPointer.GetComponent<Pointer>().SetSelectionState(Pointer.SelectionState.UI, this);
            annotationEditor.transform.Find("EditorMenu").gameObject.SetActive(false);
            annotationEditor.transform.Find("Editing").gameObject.SetActive(true);
            annotationEditor.SetActive(true);
        }

        /// <summary>
        /// Removes the given <paramref name="annotation"/> from the object.
        /// </summary>
        /// <param name="annotation">the annotation to be removed</param>
        public void RemoveAnnotation(GameObject annotation)
        {
            GameObject.Destroy(annotation.gameObject);
            annotations.Remove(annotation);
            List<string> annotationsCopy = new List<string>();
            if (annotations.Count > 0)
            {
                foreach (GameObject game in annotations)
                {
                    annotationsCopy.Add(game.GetComponent<TextGUIAndPaperResizer>().Text.Replace(System.Environment.NewLine, " "));
                    GameObject.Destroy(game.gameObject);
                }
                annotations.Clear();
                foreach (string anno in annotationsCopy)
                {
                    Annotate(anno);
                }
            }
            else
            {
                isAnnotated = false;
            }
            CloseAnnotationEditor();
        }

        /// <summary>
        /// Removes all annoations from the object.
        /// </summary>
        public void RemoveAllAnnotations()
        {
            foreach (GameObject annotation in annotations)
            {
                GameObject.Destroy(annotation);
            }
            annotations.Clear();
            isAnnotated = false;
        }

        /// <summary>
        /// Depending on delete the given <paramref name="annotation"/> is either deleted or edited.
        /// </summary>
        /// <param name="annotation">the clicked annotation</param>
        public void AnnotationClicked(GameObject annotation)
        {
            if (delete)
            {
                new Net.RemoveAnnotationAction(this, annotation).Execute();
            }
            else
            {
                StartEditing(annotation);
                guiPointer.GetComponent<Pointer>().SetSelectionState(Pointer.SelectionState.UI, this);
            }
        }

        public void MakeAnnotationsClickable(bool delete)
        {
            if (isAnnotated)
            {
                this.delete = delete;
                ShowInformation();
                foreach (GameObject game in annotations)
                {
                    game.transform.Find("Paper").gameObject.SetActive(true);
                    GetComponentInChildren<Clickable>().enabled = true;
                }
                guiPointer.GetComponent<Pointer>().SetSelectionState(Pointer.SelectionState.Annotations, this);
            }
            else
            {
                Debug.LogWarning("No Annations available");
                OpenAnnotationEditor();
            }
        }

        public void MakeAnnotationsUnClickable()
        {
            foreach (GameObject game in annotations)
            {
                game.transform.Find("Paper").gameObject.SetActive(false);
                GetComponentInChildren<Clickable>().enabled = false;
            }
            guiPointer.GetComponent<Pointer>().SetSelectionState(Pointer.SelectionState.None, this);

        }

        public override void ShowInformation()
        {
            base.ShowInformation();
            foreach (GameObject game in annotations)
            {
                game.SetActive(true);
            }
        }

        public override void HideInformation()
        {
            base.HideInformation();
            foreach (GameObject game in annotations)
            {
                game.SetActive(false);
            }
        }

        public override void Hovered(bool isOwner)
        {
            base.Hovered(isOwner);
            ShowInformation();
        }

        public override void Unhovered()
        {
            base.Unhovered();
            HideInformation();
        }

        public void SetEditorState(bool editorOpen)
        {
            this.editorOpen = editorOpen;
        }

        public bool GetEditorState()
        {
            return editorOpen;
        }

        public void SetIsAnnotatable(bool annotatble)
        {
            this.isAnnotatable = annotatble;
        }

        public bool GetIsAnnotatable()
        {
            return isAnnotatable;
        }

        /// <summary>
        /// Resizes the text of the annotation.
        /// </summary>
        /// <param name="annotation">the raw text of the annotation to be added</param>
        /// <returns>the formated text of the annotation to be added </returns>
        private string ResizeAnnotation(string annotation)
        {
            string annotationNewLine = annotation;
            // Determines the line length of the annotations
            int informationLength = 30;
            if (informationLength < annotationNewLine.Length)
            {
                for (int i = 1; i * informationLength < annotationNewLine.Length; i++)
                {
                    if (i == 1)
                    {
                        if (annotationNewLine.Substring(0, informationLength).Contains(" "))
                        {
                            int indexLastSpace = annotationNewLine.LastIndexOf(" ", informationLength, informationLength);
                            annotationNewLine = annotationNewLine.Remove(indexLastSpace, 1).Insert(indexLastSpace, System.Environment.NewLine);
                        }
                        else
                        {
                            int indexNextSpace = annotationNewLine.IndexOf(" ", informationLength);
                            if (indexNextSpace != null && indexNextSpace != -1)
                            {
                                annotationNewLine = annotationNewLine.Remove(indexNextSpace, 1).Insert(indexNextSpace, System.Environment.NewLine);
                            }
                        }
                    }
                    else
                    {
                        int index = i * informationLength + (2 * (i - 1));
                        if (index >= annotationNewLine.Length)
                        {
                            index = annotationNewLine.Length - 1;
                        }
                        index = annotationNewLine.LastIndexOf(System.Environment.NewLine, index) + informationLength;
                        if (index < annotationNewLine.Length)
                        {
                            if (annotationNewLine.Substring(index - informationLength, informationLength).Contains(" "))
                            {
                                int indexLastSpace = annotationNewLine.LastIndexOf(" ", index, informationLength);
                                annotationNewLine = annotationNewLine.Remove(indexLastSpace, 1).Insert(indexLastSpace, System.Environment.NewLine);
                            }
                            else
                            {
                                int indexNextSpace = annotationNewLine.IndexOf(" ", index);
                                if (indexNextSpace != null && indexNextSpace != -1)
                                {
                                    annotationNewLine = annotationNewLine.Remove(indexNextSpace, 1).Insert(indexNextSpace, System.Environment.NewLine);
                                }
                            }
                        }
                    }
                }
                int startIndexLastLine = annotationNewLine.LastIndexOf(System.Environment.NewLine, annotationNewLine.Length - 1) + 1;
                if (annotationNewLine.Length - 1 - startIndexLastLine > informationLength)
                {
                    int indexLastSpace = annotationNewLine.LastIndexOf(" ", startIndexLastLine + informationLength);
                    annotationNewLine = annotationNewLine.Remove(indexLastSpace, 1).Insert(indexLastSpace, System.Environment.NewLine);
                }
            }
            return annotationNewLine;
        }
    }
}
