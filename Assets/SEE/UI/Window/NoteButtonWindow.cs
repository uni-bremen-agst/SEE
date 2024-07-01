using Michsky.UI.ModernUIPack;
using SEE.Controls;
using SEE.GO;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SEE.UI.Window
{
    /// <summary>
    /// Manages the note button window, allowing for display and interaction with note-related UI elements.
    /// </summary>
    public class NoteButtonWindow : MonoBehaviour
    {
        private static NoteButtonWindow instance;

        /// <summary>
        /// Gets the singleton instance of the <see cref="NoteButtonWindow"/> class.
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
        /// Flag indicating whether the NoteButtonWindow is open.
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

            ButtonManagerBasic saveButton = noteButtonWindow.transform.Find("Content/SaveButton").gameObject.MustGetComponent<ButtonManagerBasic>();
            saveButton.clickEvent.AddListener(saveButtonAction);

            ButtonManagerBasic loadButton = noteButtonWindow.transform.Find("Content/LoadButton").gameObject.MustGetComponent<ButtonManagerBasic>();
            loadButton.clickEvent.AddListener(loadButtonAction);

            ButtonManagerBasic deleteButton = noteButtonWindow.transform.Find("Content/DeleteButton").gameObject.MustGetComponent<ButtonManagerBasic>();
            deleteButton.clickEvent.AddListener(deleteButtonAction);

            ButtonManagerBasic refreshButton = noteButtonWindow.transform.Find("Content/RefreshButton").gameObject.MustGetComponent<ButtonManagerBasic>();
            refreshButton.clickEvent.AddListener(refreshButtonAction);

            publicToggle = noteButtonWindow.transform.Find("Content/PublicToggle").gameObject.MustGetComponent<Toggle>();
            publicToggle.onValueChanged.AddListener(publicToggleAction);

            isOpen = true;
        }

        /// <summary>
        /// Destroys the note button window.
        /// </summary>
        public void DestroyWindow()
        {
            Destroyer.Destroy(noteButtonWindow);
        }
    }
}
