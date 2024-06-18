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
using UnityEngine.UI;

namespace SEE.UI.Window.NoteWindow
{
    public class NoteWindow : BaseWindow
    {
        /// <summary>
        /// Prefab for the <see cref="NoteWindow"/>.
        /// </summary>
        private static string WindowPrefab => UIPrefabFolder + "NoteWindow";

        public TMP_InputField searchField;

        //public GraphElement graphElement;

        private SwitchManager switchManager;

        private NoteManager noteManager;

        // Start is called before the first frame update
        protected override void StartDesktop()
        {
            base.StartDesktop();
            CreateWindow();
        }

        // Update is called once per frame
        private void CreateWindow()
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

            ButtonManagerBasic refreshButton = noteWindow.transform.Find("ScrollView/Viewport/Content/RefreshButton").gameObject.MustGetComponent<ButtonManagerBasic>();
            refreshButton.clickEvent.AddListener(LoadNote);

            switchManager = noteWindow.transform.Find("ScrollView/Viewport/Content/Switch").gameObject.MustGetComponent<SwitchManager>();
            switchManager.OnEvents.AddListener(onSwitch);
            switchManager.OffEvents.AddListener(offSwitch);

            noteManager = NoteManager.Instance;

            searchField.onDeselect.AddListener(_ => SaveNote(switchManager.isOn));

            LoadNote();
        }

        private void onSwitch()
        {
            SaveNote(false);
            LoadNote();
        }

        private void offSwitch()
        {
            SaveNote(true);
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

        private void SaveNote(bool isPublic)
        {
            if (isPublic)
            {
                string title = Title;
                string content = searchField.text;

                NoteManager.Instance.SaveNote(title, isPublic, content);
                new NoteSaveNetAction(title, true, content).Execute();
            }
            else
            {
                string title = Title;
                string content = searchField.text;

                NoteManager.Instance.SaveNote(title, isPublic, content);
            }
        }

        public void LoadNote()
        {
            string title = Title;
            bool isPublic = switchManager.isOn;
            searchField.text = NoteManager.Instance.LoadNote(title, isPublic);
        }

        private void OnDestroy()
        {
            // Save the note content when the window is destroyed (closed) and mark the Node/Edge with a sticky note
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
