using Michsky.UI.ModernUIPack;
using SEE.Controls;
using SEE.Controls.Actions.Drawable;
using SEE.DataModel.DG;
using SEE.Game.Drawable;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SEE.UI.Window.NoteWindow
{
    /// <summary>
    /// Manages the note window, providing UI elements and functionality for creating, loading, saving, and deleting notes.
    /// </summary>
    public class NoteWindow : BaseWindow
    {
        /// <summary>
        /// Prefab for the <see cref="NoteWindow"/>.
        /// </summary>
        private static string WindowPrefab => UIPrefabFolder + "NoteWindow";

        /// <summary>
        /// Input field for searching within the <see cref="NoteWindow"/>.
        /// </summary>
        public TMP_InputField searchField;

        /// <summary>
        ///Reference to the graph element(node/edge) associated with this window.
        /// </summary>
        public GraphElementRef graphElementRef;

        /// <summary>
        /// An instance of the <see cref="NoteButtonWindow"/> used to manage the note buttons and their interactions.
        /// </summary>
        private NoteButtonWindow noteButtonWindow;

        /// <summary>
        /// An instance of the <see cref="NoteManager"/> used to manage the notes.
        /// </summary>
        private NoteManager noteManager;

        /// <summary>
        /// An instance of the <see cref="WindowSpace"/> used to manage the window space and its components.
        /// </summary>
        private WindowSpace manager;

        private Toggle publicToggle;

        /// <summary>
        /// Creates and configures the <see cref="NoteWindow"/>.
        /// </summary>
        protected override void StartDesktop()
        {
            base.StartDesktop();
            CreateWindow();
        }

        /// <summary>
        /// Creates and configures the note window, including setting up the UI elements and their associated actions.
        /// </summary>
        private void CreateWindow()
        {
            noteManager = NoteManager.Instance;
            manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
            noteButtonWindow = NoteButtonWindow.Instance;

            GameObject noteWindow = PrefabInstantiator.InstantiatePrefab(WindowPrefab, Window.transform.Find("Content"), false);
            noteWindow.name = "Note Window";

            searchField = noteWindow.transform.Find("ScrollView/Viewport/Content/InputField").gameObject.MustGetComponent<TMP_InputField>();
            searchField.onSelect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = false);
            searchField.onDeselect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = true);
            searchField.onDeselect.AddListener(_ => SaveNote(publicToggle.isOn));

            // Set up button actions
            SetUpButton(noteWindow, "Content/SaveButton", CreateSaveButtonAction());
            SetUpButton(noteWindow, "Content/LoadButton", CreateLoadButtonAction());
            SetUpButton(noteWindow, "Content/DeleteButton", CreateDeleteButtonAction());
            SetUpButton(noteWindow, "Content/RefreshButton", CreateRefreshButtonAction());

            publicToggle = noteWindow.transform.Find("Content/PublicToggle").gameObject.MustGetComponent<Toggle>();
            publicToggle.onValueChanged.AddListener(CreatePublicToggleAction());

            if (!noteButtonWindow.isOpen)
            {
                noteButtonWindow.OpenWindow(CreateSaveButtonAction(), CreateLoadButtonAction(), CreateDeleteButtonAction(), CreateRefreshButtonAction(), CreatePublicToggleAction());
            }

            LoadNote();
        }

        /// <summary>
        /// Sets up a button with the specified path and action.
        /// </summary>
        /// <param name="noteWindow">The note window GameObject.</param>
        /// <param name="path">The path to the button within the note window.</param>
        /// <param name="action">The action to assign to the button.</param>
        private void SetUpButton(GameObject noteWindow, string path, UnityAction action)
        {
            ButtonManagerBasic button = noteWindow.transform.Find(path).gameObject.MustGetComponent<ButtonManagerBasic>();
            button.clickEvent.AddListener(action);
        }

        /// <summary>
        /// Creates the save button action. It saves the content from the <see cref="NoteWindow"/> into a file.</param>
        /// </summary>
        /// <returns>The UnityAction for the save button.</returns>
        private UnityAction CreateSaveButtonAction()
        {
            return () =>
            {
                manager.ActiveWindow.gameObject.TryGetComponent<NoteWindow>(out NoteWindow activeWin);
                noteButtonWindow.contentText = activeWin.searchField.text;
                string content = noteButtonWindow.contentText;
                string path = EditorUtility.SaveFilePanel("Save Note", "", "Note", "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (!string.IsNullOrEmpty(content))
                        File.WriteAllText(path, content);
                }
            };
        }

        /// <summary>
        /// Creates the load button action. It loads the content from a file to the <see cref="NoteWindow"/>.
        /// </summary>
        /// <returns>The UnityAction for the load button.</returns>
        private UnityAction CreateLoadButtonAction()
        {
            return () =>
            {
                manager.ActiveWindow.gameObject.TryGetComponent<NoteWindow>(out NoteWindow activeWin);
                string path = EditorUtility.OpenFilePanel("Overwrite with txt", "", "");
                if (!string.IsNullOrEmpty(path))
                {
                    string fileContent = File.ReadAllText(path);
                    activeWin.searchField.text = fileContent;
                }
            };
        }

        /// <summary>
        /// Creates the delete button action. It deletes the content and outline if the Node or Edge has no note.
        /// </summary>
        /// <returns>The UnityAction for the delete button.</returns>
        private UnityAction CreateDeleteButtonAction()
        {
            return () =>
            {
                manager.ActiveWindow.gameObject.TryGetComponent<NoteWindow>(out NoteWindow activeWin);
                activeWin.searchField.text = "";
                GameObject removeGO = manager.ActiveWindow.gameObject;
                KeyValuePair<string, bool> kv = new KeyValuePair<string, bool>(activeWin.graphElementRef.Elem.ID, publicToggle.isOn);
                noteManager.notesDictionary.Remove(kv);
                KeyValuePair<string, bool> kv2 = new KeyValuePair<string, bool>(activeWin.graphElementRef.Elem.ID, !publicToggle.isOn);
                if (!noteManager.notesDictionary.ContainsKey(kv2))
                {
                    RemoveOutline(removeGO);
                    noteManager.objectList.Remove(removeGO);
                }
            };
        }

        /// <summary>
        /// Creates the refresh button action. It loads the new content to <see cref="NoteWindow"/>.
        /// </summary>
        /// <returns>The UnityAction for the refresh button.</returns>
        private UnityAction CreateRefreshButtonAction()
        {
            return () =>
            {
                manager.ActiveWindow.gameObject.TryGetComponent<NoteWindow>(out NoteWindow activeWin);
                string graphID = activeWin.graphElementRef.Elem.ID;
                bool isPublic = publicToggle.isOn;
                activeWin.searchField.text = NoteManager.Instance.LoadNote(graphID, isPublic);
            };
        }

        /// <summary>
        /// Creates the public toggle action.
        /// </summary>
        /// <returns>The UnityAction for the public toggle.</returns>
        private UnityAction<bool> CreatePublicToggleAction()
        {
            return (bool isPublic) =>
            {
                manager.ActiveWindow.gameObject.TryGetComponent<NoteWindow>(out NoteWindow activeWin);
                string content = activeWin.searchField.text;
                string graphID = activeWin.graphElementRef.Elem.ID;

                if (!isPublic)
                {
                    NoteManager.Instance.SaveNote(activeWin.graphElementRef.Elem.ID, !isPublic, content);
                    new NoteSaveNetAction(activeWin.graphElementRef.Elem.ID, !isPublic, content).Execute();
                }
                else
                {
                    NoteManager.Instance.SaveNote(activeWin.graphElementRef.Elem.ID, !isPublic, content);
                }
                activeWin.searchField.text = NoteManager.Instance.LoadNote(graphID, isPublic);
            };
        }

        /// <summary>
        /// Removes the outline from the specified <paramref name="gameObject"/>.
        /// </summary>
        /// <param name="gameObject">The game object to remove the outline from.</param>
        private void RemoveOutline(GameObject gameObject)
        {
            Material noteMaterial = Resources.Load<Material>("Materials/Outliner_MAT");
            MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
            string oldMaterialName = meshRenderer.materials[meshRenderer.materials.Length - 1].name;
            string newName = oldMaterialName.Replace(" (Instance)", "");
            if (newName == noteMaterial.name)
            {
                Material[] gameObjects = new Material[meshRenderer.materials.Length - 1];
                Array.Copy(meshRenderer.materials, gameObjects, meshRenderer.materials.Length - 1);
                meshRenderer.materials = gameObjects;
            }
        }

        /// <summary>
        /// Saves the current note content for the specified <see cref="GraphElementRef"/>.
        /// </summary>
        /// <param name="isPublic">Indicates whether the note should be saved as public or private.</param>
        private void SaveNote(bool isPublic)
        {
            if (!string.IsNullOrEmpty(searchField.text))
            {
                string graphElement = graphElementRef.Elem.ID;
                if (isPublic)
                {
                    string content = searchField.text;

                    NoteManager.Instance.SaveNote(graphElement, isPublic, content);
                    new NoteSaveNetAction(graphElement, true, content).Execute();
                }
                else
                {
                    string content = searchField.text;
                    NoteManager.Instance.SaveNote(graphElement, isPublic, content);
                }
            }
        }

        /// <summary>
        /// Loads the content into the note.
        /// </summary>
        public void LoadNote()
        {
            string graphID = graphElementRef.Elem.ID;
            bool isPublic = false;
            searchField.text = NoteManager.Instance.LoadNote(graphID, isPublic);
        }

        /// <summary>
        /// Called when the object is destroyed.
        /// If there are no instances of <see cref="NoteWindow"/> present, it destroys the <see cref="NoteButtonWindow"/>.
        /// </summary>
        public void OnDestroy()
        {
            if (!ContainsNoteWindow())
            {
                noteButtonWindow.DestroyWindow();
            }
        }

        /// <summary>
        /// Checks if there are any instances of <see cref="NoteWindow"/> in the current window space.
        /// </summary>
        /// <returns>True if a <see cref="NoteWindow"/> is found, otherwise false.</returns>
        private bool ContainsNoteWindow()
        {
            WindowSpace manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
            foreach (BaseWindow baseWindow in manager.Windows)
            {
                if (baseWindow.gameObject.GetComponent<NoteWindow>())
                {
                    return true;
                }
            }
            return false;
        }

        public override void RebuildLayout()
        {
            // Nothing needs to be done.
        }

        protected override void InitializeFromValueObject(WindowValues valueObject)
        {
            if (valueObject is not NoteWindowValues noteValues)
            {
                throw new UnsupportedTypeException(typeof(NoteWindowValues), valueObject.GetType());
            }
            if (noteValues.Text != null)
            {
                // Nothings needs to be done
            }
        }

        public override void UpdateFromNetworkValueObject(WindowValues valueObject)
        {
            if(valueObject is not NoteWindowValues noteValues)
            {
                throw new UnsupportedTypeException(typeof(NoteWindowValues), valueObject.GetType());
            }
        }

        public override WindowValues ToValueObject()
        {
            string attachedTo = gameObject.name;
            return new NoteWindowValues(Title, attachedTo, searchField.text);
        }
    }

    /// <summary>
    /// Represents the values of a code window needed to re-create its content.
    /// Used for serialization when sending a <see cref="CodeWindow"/> over the network.
    /// </summary>
    [Serializable]
    public class NoteWindowValues : WindowValues
    {
        /// <summary>
        /// Text of the note window. May be <c>null</c> or <c>empty</c>, in which case <see cref="Path"/> is not <c>null</c>.
        /// </summary>
        [field: SerializeField]
        public string Text { get; private set; }


        /// <summary>
        /// The line number which is currently visible in / at the top of the code window.
        /// </summary>
        //[field: SerializeField]
        //public int VisibleLine { get; private set; }

        /// <summary>
        /// Creates a new CodeWindowValues object from the given parameters.
        /// Note that either <paramref name="text"/> or <paramref name="title"/> must not be <c>null</c>.
        /// </summary>
        /// <param name="title">The title of the code window.</param>
        /// <param name="attachedTo">Name of the game object the note window is attached to.</param>
        /// <param name="text">The text of the code window. May be <c>null</c>, in which case
        /// May be <c>null</c>, in which case <paramref name="text"/> may not.</param>
        /// <exception cref="ArgumentException">Thrown when both <paramref name="path"/> and
        /// <paramref name="text"/> are <c>null</c>.</exception>
        internal NoteWindowValues(string title, string attachedTo = null, string text = null) : base(title, attachedTo)
        {
            if (text == null)
            {
                throw new ArgumentException("Either text or filename must not be null!");
            }

            Text = text;
        }
    }
}
