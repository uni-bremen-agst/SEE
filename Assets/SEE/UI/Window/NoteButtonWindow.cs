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

namespace SEE.UI.Window
{
    public class NoteButtonWindow : PlatformDependentComponent
    {
        private string windowPrefab => UIPrefabFolder + "NoteButtonWindow";
        private GameObject noteButtonWindow;
        public TMP_InputField contentField;
        private WindowSpace manager;

        public void OpenWindow()
        {
            noteButtonWindow = PrefabInstantiator.InstantiatePrefab(windowPrefab, Canvas.transform, false);
            noteButtonWindow.name = "NoteButtonWindowlol";
            manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
            Debug.Log("manager.Length:" + manager.Windows.Count);

            ButtonManagerBasic saveButton = noteButtonWindow.transform.Find("Content/SaveButton").gameObject.MustGetComponent<ButtonManagerBasic>();
            Debug.Log("SaveButton: " + saveButton);
            saveButton.clickEvent.AddListener(WriteToFile);

            ButtonManagerBasic loadButton = noteButtonWindow.transform.Find("Content/LoadButton").gameObject.MustGetComponent<ButtonManagerBasic>();
            loadButton.clickEvent.AddListener(LoadFromFile);

            ButtonManagerBasic deleteButton = noteButtonWindow.transform.Find("Content/DeleteButton").gameObject.MustGetComponent<ButtonManagerBasic>();
            deleteButton.clickEvent.AddListener(DeleteFile);

            ButtonManagerBasic refreshButton = noteButtonWindow.transform.Find("Content/RefreshButton").gameObject.MustGetComponent<ButtonManagerBasic>();
            //refreshButton.clickEvent.AddListener(LoadNote);

            /*switchManager = noteButtonWindow.transform.Find("ScrollView/Viewport/Content/Switch").gameObject.MustGetComponent<SwitchManager>();
            switchManager.OnEvents.AddListener(onSwitch);
            switchManager.OffEvents.AddListener(offSwitch);

            noteManager = NoteManager.Instance;*/

            //contentField.onDeselect.AddListener(_ => SaveNote(switchManager.isOn));
        }

        /// <summary>
        /// Make the note public.
        /// </summary>
        private void onSwitch()
        {
            //SaveNote(false);
            //LoadNote();
        }

        /// <summary>
        /// Make the note private.
        /// </summary>
        private void offSwitch()
        {
            //SaveNote(true);
            //LoadNote();
        }

        /// <summary>
        /// Saves the content into a file.
        /// </summary>
        private void WriteToFile()
        {
            Debug.Log("Pressed on SaveButton");
            Debug.Log("manager.Length:" + manager.Windows.Count);
            BaseWindow activeWindow = manager.ActiveWindow;
            //string content = activeWindow;
            Debug.Log("activeWindow.name: " + activeWindow.name);
            Debug.Log("activeWindow:" + activeWindow);
            Debug.Log("activeWindow.Window: " + activeWindow.Window);

            /*string path = EditorUtility.SaveFilePanel(
            "Save Note",
            "",
            "Note",
            "");
            if (path.Length != 0)
            {
                content = contentField.text;
                if (content != null)
                    File.WriteAllText(path, content);
            }*/
        }

        /// <summary>
        /// Loads the content from a file.
        /// </summary>
        private void LoadFromFile()
        {
            string path = EditorUtility.OpenFilePanel("Overwrite with txt", "", "");
            if (path.Length != 0)
            {
                string fileContent = File.ReadAllText(path);
                contentField.text = fileContent;
            }
        }

        /// <summary>
        /// Deletes the content from the note.
        /// </summary>
        private void DeleteFile()
        {
            contentField.text = "";
        }
    }
}
