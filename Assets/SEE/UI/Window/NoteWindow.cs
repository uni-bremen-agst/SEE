using Michsky.UI.ModernUIPack;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.UI.Window
{
    public class NoteWindow : BaseWindow
    {
        /// <summary>
        /// Prefab for the <see cref="NoteWindow"/>.
        /// </summary>
        private static string WindowPrefab => UIPrefabFolder + "NoteWindow";

        public TMP_InputField searchField;

        // Start is called before the first frame update
        protected override void StartDesktop()
        {
            base.StartDesktop();
            CreateWindow();
        }

        // Update is called once per frame
        public void CreateWindow()
        {
            GameObject noteWindow = PrefabInstantiator.InstantiatePrefab(WindowPrefab, Window.transform.Find("Content"), false);
            noteWindow.name = "Note Window";

            searchField = noteWindow.transform.Find("ScrollView/Viewport/Content/InputField").gameObject.MustGetComponent<TMP_InputField>();

            searchField.onSelect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = false);
            searchField.onDeselect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = true);

            ButtonManagerBasic saveButton = noteWindow.transform.Find("ScrollView/Viewport/Content/SaveButton").gameObject.MustGetComponent<ButtonManagerBasic>();
            saveButton.clickEvent.AddListener(WriteToFile);

            ButtonManagerBasic loadButton = noteWindow.transform.Find("ScrollView/Viewport/Content/LoadButton").gameObject.MustGetComponent<ButtonManagerBasic>();
            loadButton.clickEvent.AddListener(LoadFromFile);

            ButtonManagerBasic deleteButton = noteWindow.transform.Find("ScrollView/Viewport/Content/DeleteButton").gameObject.MustGetComponent<ButtonManagerBasic>();
            deleteButton.clickEvent.AddListener(DeleteFile);

            LoadNote();
        }

        private void WriteToFile()
        {
            string path = EditorUtility.SaveFilePanel(
            "Save Note",
            "",
            "Note",
            "");
            if (path.Length != 0)
            {
                string stringData = searchField.text;
                if (stringData != null)
                    File.WriteAllText(path, stringData);
            }
        }

        private void LoadFromFile()
        {
            string path = EditorUtility.OpenFilePanel("Overwrite with txt", "", "");
            if (path.Length != 0)
            {
                string fileContent = File.ReadAllText(path);
                searchField.text = fileContent;
            }
        }

        private void DeleteFile()
        {
            searchField.text = "";
        }

        private void SaveNote()
        {
            PlayerPrefs.SetString(Title, searchField.text);
            //PlayerPrefs.Save();
        }

        private void LoadNote()
        {
            if (searchField != null)
            {
                if (PlayerPrefs.HasKey(Title))
                {
                    searchField.text = PlayerPrefs.GetString(Title);
                }
            }
        }

        private void OnDestroy()
        {
            // Save the note content when the window is destroyed (closed)
            SaveNote();
        }

        public override void RebuildLayout()
        {
            // Nothing needs to be done.
        }

        protected override void InitializeFromValueObject(WindowValues valueObject)
        {
            // TODO : Should metric windows be sent over the network?
            throw new NotImplementedException();
        }

        public override void UpdateFromNetworkValueObject(WindowValues valueObject)
        {
            // TODO : Should metric windows be sent over the network?
            throw new NotImplementedException();
        }

        public override WindowValues ToValueObject()
        {
            // TODO : Should metric windows be sent over the network?
            throw new NotImplementedException();
        }
    }
}
