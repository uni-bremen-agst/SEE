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

        /// <summary>
        /// The searchfield for <see cref="NoteWindow"/>.
        /// </summary>
        public TMP_InputField searchField;

        /// <summary>
        /// The GraphElement for the depending Node/Edge
        /// </summary>
        public GraphElementRef graphElementRef;

        private SwitchManager switchManager;

        private NoteManager noteManager;

        // Start is called before the first frame update
        protected override void StartDesktop()
        {
            base.StartDesktop();
            CreateWindow();
        }

        /// <summary>
        /// Creates the <see cref="NoteWindow"/> and loads data if avaiable.
        /// </summary>
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

        /// <summary>
        /// Make the note public.
        /// </summary>
        private void onSwitch()
        {
            SaveNote(false);
            LoadNote();
        }

        /// <summary>
        /// Make the note private.
        /// </summary>
        private void offSwitch()
        {
            SaveNote(true);
            LoadNote();
        }

        /// <summary>
        /// Saves the content into a file.
        /// </summary>
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

        /// <summary>
        /// Loads the content from a file.
        /// </summary>
        private void LoadFromFile()
        {
            string path = EditorUtility.OpenFilePanel("Overwrite with txt", "", "");
            if (path.Length != 0)
            {
                string fileContent = File.ReadAllText(path);
                searchField.text = fileContent;
            }
        }

        /// <summary>
        /// Deletes the content from the note.
        /// </summary>
        private void DeleteFile()
        {
            searchField.text = "";
        }

        /// <summary>
        /// Saves the Note
        /// </summary>
        /// <param name="isPublic">should the note be saved to other clients</param>
        private void SaveNote(bool isPublic)
        {
            if (isPublic)
            {
                string content = searchField.text;

                NoteManager.Instance.SaveNote(graphElementRef, isPublic, content);
                new NoteSaveNetAction(graphElementRef, true, content).Execute();
            }
            else
            {
                string content = searchField.text;
                NoteManager.Instance.SaveNote(graphElementRef, isPublic, content);
            }
        }

        /// <summary>
        /// Loads the content into the note.
        /// </summary>
        public void LoadNote()
        {
            string graphID = graphElementRef.Elem.ID;
            bool isPublic = switchManager.isOn;
            searchField.text = NoteManager.Instance.LoadNote(graphID, isPublic);
        }

        public void OnDestroy()
        {
            WindowSpace manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
            if (manager.Windows.Count == 0)
            {
                GameObject gameObject = GameObject.Find("UI Canvas").transform.Find("NoteButtonWindow(Clone)").gameObject;
                gameObject.SetActive(false);
            }
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
