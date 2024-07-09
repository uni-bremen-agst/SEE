using SEE.Controls;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

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
        /// Reference to the graph element(node/edge) associated with this window.
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

            if (!noteButtonWindow.isOpen)
            {
                noteButtonWindow.OpenWindow(CreateSaveButtonAction(), CreateLoadButtonAction(), CreateDeleteButtonAction(), CreateRefreshButtonAction(), CreatePublicToggleAction());
            }

            searchField.onDeselect.AddListener(_ => SaveNote(noteButtonWindow.publicToggle.isOn));
            LoadNote();
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
                string path = EditorUtility.OpenFilePanel("Load Note", "", "");
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
                noteManager.objectList.Remove(removeGO);
                KeyValuePair<string, bool> currentNoteKey = new KeyValuePair<string, bool>(activeWin.graphElementRef.Elem.ID, noteButtonWindow.publicToggle.isOn);
                noteManager.notesDictionary.Remove(currentNoteKey);
                KeyValuePair<string, bool> oppositeNoteKey = new KeyValuePair<string, bool>(activeWin.graphElementRef.Elem.ID, !noteButtonWindow.publicToggle.isOn);
                if (!noteManager.notesDictionary.ContainsKey(oppositeNoteKey))
                {
                    RemoveOutline(removeGO);
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
                bool isPublic = noteButtonWindow.publicToggle.isOn;
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
            MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
            Material[] gameObjects = new Material[meshRenderer.materials.Length - 1];
            Array.Copy(meshRenderer.materials, gameObjects, meshRenderer.materials.Length - 1);
            meshRenderer.materials = gameObjects;
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
            //Nothing needs to be done since multiplayer is already implemented for NoteWindow
            throw new NotImplementedException();
        }

        public override void UpdateFromNetworkValueObject(WindowValues valueObject)
        {
            //Nothing needs to be done since multiplayer is already implemented for NoteWindow
            throw new NotImplementedException();
        }

        public override WindowValues ToValueObject()
        {
            //Nothing needs to be done since multiplayer is already implemented for NoteWindow
            throw new NotImplementedException();
        }
    }
}
