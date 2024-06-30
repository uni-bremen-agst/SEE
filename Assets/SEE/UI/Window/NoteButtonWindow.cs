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
    public class NoteButtonWindow : MonoBehaviour
    {
        private static NoteButtonWindow instance;

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

        private string windowPrefab = "Prefabs/UI/NoteButtonWindow";
        private GameObject noteButtonWindow;
        public string contentText;
        public Toggle publicToggle;
        public bool flag = false;

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

            flag = true;
        }

        public void DestroyWindow()
        {
            Destroyer.Destroy(noteButtonWindow);
        }
    }
}
