using Michsky.UI.ModernUIPack;
using SEE.Controls;
using SEE.GO;
using SEE.UI.Window.NoteWindow;
using SEE.Utils;
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
    /// Manages the note button window, allowing for display and interaction with note-related UI elements.
    /// </summary>
    public class NoteButtonWindow : MonoBehaviour
    {
        /// <summary>
        /// The instance of the <see cref="NoteButtonWindow"/> class.
        /// </summary>
        private static NoteButtonWindow instance;

        /// <summary>
        /// Gets instance of the <see cref="NoteButtonWindow"/> class.
        /// </summary>
        public static NoteButtonWindow Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<NoteButtonWindow>();
                    if (instance == null)
                    {
                        GameObject go = GameObject.Find("NoteManager");
                        instance = go.AddComponent<NoteButtonWindow>();
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// Path to the prefab for the NoteButtonWindow UI.
        /// </summary>
        private string windowPrefab = "Prefabs/UI/NoteButtonWindow";

        /// <summary>
        /// Instance of the instantiated NoteButtonWindow GameObject.
        /// </summary>
        private GameObject noteButtonWindow;

        /// <summary>
        /// Text content of the note.
        /// </summary>
        public string contentText;

        /// <summary>
        /// Toggle to set the note as public or private.
        /// </summary>
        public Toggle publicToggle;

        /// <summary>
        /// Flag indicating whether the <see cref="NoteButtonWindow"/> is open or not.
        /// </summary>
        public bool isOpen = false;

        /// <summary>
        /// Opens the note button window and assigns actions to the buttons and toggle.
        /// </summary>
        /// <param name="saveButtonAction">Action to be performed when the save button is clicked.</param>
        /// <param name="loadButtonAction">Action to be performed when the load button is clicked.</param>
        /// <param name="deleteButtonAction">Action to be performed when the delete button is clicked.</param>
        /// <param name="refreshButtonAction">Action to be performed when the refresh button is clicked.</param>
        /// <param name="publicToggleAction">Action to be performed when the public/private toggle is changed.</param>
        public void OpenWindow(UnityAction saveButtonAction, UnityAction loadButtonAction, UnityAction deleteButtonAction, UnityAction refreshButtonAction,
            UnityAction<bool> publicToggleAction)
        {
            noteButtonWindow = PrefabInstantiator.InstantiatePrefab(windowPrefab, GameObject.Find("UI Canvas").transform, false);

            // Set up button actions
            SetUpButton(noteButtonWindow, "Content/SaveButton", saveButtonAction);
            SetUpButton(noteButtonWindow, "Content/LoadButton", loadButtonAction);
            SetUpButton(noteButtonWindow, "Content/DeleteButton", deleteButtonAction);
            SetUpButton(noteButtonWindow, "Content/RefreshButton", refreshButtonAction);

            publicToggle = noteButtonWindow.transform.Find("Content/PublicToggle").gameObject.MustGetComponent<Toggle>();
            publicToggle.onValueChanged.AddListener(publicToggleAction);

            isOpen = true;
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
        /// Destroys the note button window.
        /// </summary>
        public void DestroyWindow()
        {
            NoteButtonWindow buttonWindow = GameObject.Find("NoteManager").GetComponent<NoteButtonWindow>();
            Destroyer.Destroy(noteButtonWindow);
            Destroy(buttonWindow);
        }
    }
}
