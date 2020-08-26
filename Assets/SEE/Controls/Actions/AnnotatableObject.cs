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
        public bool isAnnotated = false;

        public List<GameObject> annotations;

        private GameObject annotationEditor = null;

        private static GameObject annotationEditorPrefab;

        private static GameObject annotationOnPaperPrefab;

        private bool editorOpen = false;

        private GameObject annotationToEdit;

        protected override void Awake()
        {
            base.Awake();
            if (annotationEditorPrefab == null)
            {
                // Filename of the prefab for the text on paper excluding its file extension .prefab
                string path = "Prefabs/AnnotationEditor3D";
                annotationEditorPrefab = Resources.Load<GameObject>(path);
                if (annotationEditorPrefab == null)
                {
                    Debug.LogErrorFormat("Prefab {0} not found.\n", path);
                }
            }
            if (annotationOnPaperPrefab == null)
            {
                // Filename of the prefab for the text on paper excluding its file extension .prefab
                string path = "Prefabs/TextOnPaper";
                annotationOnPaperPrefab = Resources.Load<GameObject>(path);
                if (annotationOnPaperPrefab == null)
                {
                    Debug.LogErrorFormat("Prefab {0} not found.\n", path);
                }
            }
        }

        public void OpenAnnotationEditor()
        {
            if (annotationEditor == null)
            {
                annotationEditor = Instantiate(annotationEditorPrefab, Vector3.zero, Quaternion.identity);
                annotationEditor.transform.SetParent(gameObject.transform);
                Vector3 position = transform.position;
                position.y = BoundingBox.GetRoof(GameObjectHierarchy.Descendants(gameObject, Tags.Node)) + annotationEditor.GetComponent<RectTransform>().rect.height / 2.0f;
                annotationEditor.transform.position = position;
                annotationEditor.SetActive(true);
            }
            else
            {
                ResetAnnotationEditor();
                HideInformation();
                base.ShowInformation();
                annotationEditor.SetActive(true);
            }
        }

        public void CloseAnnotationEditor()
        {
            editorOpen = false;
            annotationEditor.SetActive(false);
            ShowInformation();
            MakeAnnotationsUnClickable();
        }

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

        public void Annotate(string annotation)
        {
            GameObject annotationOnPaper = Instantiate(annotationOnPaperPrefab, Vector3.zero, Quaternion.identity);
            annotationOnPaper.name = "Annotation";
            annotationOnPaper.GetComponent<TextGUIAndPaperResizer>().Text = ResizeAnnotation(annotation);

            // Now textOnPaper has been re-sized properly; so we can derive its absolute height.
            float paperHeight = TextGUIAndPaperResizer.Height(annotationOnPaper);

            // We want to put the label above the roof of the gameObject. The gameObject,
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
                position.y = BoundingBox.GetRoof(texts) + paperHeight * 1.1f;

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
            text.AddComponent<BoxCollider2D>();
            text.AddComponent<Clickable>();
            text.GetComponent<BoxCollider2D>().enabled = false;
            text.GetComponent<Clickable>().enabled = false;
            annotations.Add(annotationOnPaper);
            isAnnotated = true;
        }

        public void EditAnnotation(string annotation)
        {
            CloseAnnotationEditor();
            annotationToEdit.GetComponent<TextGUIAndPaperResizer>().Text = ResizeAnnotation(annotation);
            TextGUIAndPaperResizer.Height(annotationToEdit);
        }

        public void StartEditing(GameObject annotation)
        {
            HideInformation();
            base.ShowInformation();
            annotationToEdit = annotation;

            GameObject editingInput = annotationEditor.transform.Find("Editing/EditingInput").gameObject;
            editingInput.GetComponent<TMP_InputField>().text = annotationToEdit.GetComponent<TextGUIAndPaperResizer>().Text.Replace(System.Environment.NewLine, " ");

            annotationEditor.transform.Find("EditorMenu").gameObject.SetActive(false);
            annotationEditor.transform.Find("Editing").gameObject.SetActive(true);
            annotationEditor.SetActive(true);
        }

        public void MakeAnnotationsClickable(bool delete)
        {
            ShowInformation();
            foreach (GameObject game in annotations)
            {
                GetComponentInChildren<Clickable>().enabled = true;
                GetComponentInChildren<BoxCollider2D>().enabled = true;
                if (delete)
                {
                    GetComponentInChildren<Clickable>().SetDelete(true);
                }
                else
                {
                    GetComponentInChildren<Clickable>().SetDelete(false);
                }
            }
        }

        public void MakeAnnotationsUnClickable()
        {
            foreach (GameObject game in annotations)
            {
                GetComponentInChildren<Clickable>().enabled = false;
                GetComponentInChildren<BoxCollider2D>().enabled = false;
            }
        }

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

        private string ResizeAnnotation(string annotation)
        {
            string annotationNewLine = annotation;
            int informationLength = textOnPaper.GetComponent<TextGUIAndPaperResizer>().Text.Length;
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
            Debug.Log(annotationNewLine);
            return annotationNewLine;
        }
    }
}
