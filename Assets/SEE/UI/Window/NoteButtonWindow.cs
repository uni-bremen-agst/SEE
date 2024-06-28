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

namespace SEE.UI.Window
{
    public class NoteButtonWindow : PlatformDependentComponent
    {
        private string windowPrefab => UIPrefabFolder + "NoteButtonWindow";
        private GameObject noteButtonWindow;
        public string contentText;
        private WindowSpace manager;

        public void OpenWindow(UnityAction saveButtonAction, UnityAction loadButtonAction, UnityAction deleteButtonAction, UnityAction refreshButtonAction)
        {
            noteButtonWindow = PrefabInstantiator.InstantiatePrefab(windowPrefab, Canvas.transform, false);

            ButtonManagerBasic saveButton = noteButtonWindow.transform.Find("Content/SaveButton").gameObject.MustGetComponent<ButtonManagerBasic>();
            saveButton.clickEvent.AddListener(saveButtonAction);

            ButtonManagerBasic loadButton = noteButtonWindow.transform.Find("Content/LoadButton").gameObject.MustGetComponent<ButtonManagerBasic>();
            loadButton.clickEvent.AddListener(loadButtonAction);

            ButtonManagerBasic deleteButton = noteButtonWindow.transform.Find("Content/DeleteButton").gameObject.MustGetComponent<ButtonManagerBasic>();
            deleteButton.clickEvent.AddListener(deleteButtonAction);

            ButtonManagerBasic refreshButton = noteButtonWindow.transform.Find("Content/RefreshButton").gameObject.MustGetComponent<ButtonManagerBasic>();
            refreshButton.clickEvent.AddListener(refreshButtonAction);

            /*switchManager = noteButtonWindow.transform.Find("ScrollView/Viewport/Content/Switch").gameObject.MustGetComponent<SwitchManager>();
            switchManager.OnEvents.AddListener(onSwitch);
            switchManager.OffEvents.AddListener(offSwitch);

            noteManager = NoteManager.Instance;*/

            //contentField.onDeselect.AddListener(_ => SaveNote(switchManager.isOn));
        }

        public void DestroyWindow()
        {
            Destroyer.Destroy(noteButtonWindow);
        }
    }
}
