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

        private const string path = "C:\\Users\\Fujitsu\\Desktop\\SEETxt\\";

        public TMP_InputField searchField;

        public GraphElement Graph;

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

            searchField = noteWindow.transform.Find("InputField").gameObject.MustGetComponent<TMP_InputField>();

            searchField.onSelect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = false);
            searchField.onDeselect.AddListener(_ => SEEInput.KeyboardShortcutsEnabled = true);

            ButtonManagerBasic saveButton = noteWindow.transform.Find("SaveButton").gameObject.MustGetComponent<ButtonManagerBasic>();
            saveButton.clickEvent.AddListener(WriteToFile);

            ButtonManagerBasic loadButton = noteWindow.transform.Find("LoadButton").gameObject.MustGetComponent<ButtonManagerBasic>();
            loadButton.clickEvent.AddListener(LoadFromFile);
        }

        [MenuItem("Examples/Save Texture to file")]
        public void WriteToFile()
        {
            string fileName = path + Title + ".txt";
            TMP_Text stringText = searchField.transform.Find("TextArea/Text").gameObject.MustGetComponent<TMP_Text>();
            File.WriteAllText(fileName, stringText.text);
        }

        public void LoadFromFile()
        {
            /*Texture2D texture = Selection.activeObject as Texture2D;
            if(texture == null)
            {
                EditorUtility.DisplayDialog(
                    "Select Texture",
                    "You must select a texture first!",
                    "OK");
                return;
            }*/
            string path = EditorUtility.OpenFilePanel("Overwrite with txt", "", "txt");
            if (path.Length != 0)
            {
                //var fileContent = File.ReadAllBytes(path);
                string fileContent = File.ReadAllText(path);
                Debug.Log(fileContent);
                searchField.text = fileContent;
            }

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
