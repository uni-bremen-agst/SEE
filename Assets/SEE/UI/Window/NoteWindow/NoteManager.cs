using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.UI.Window.NoteWindow
{
    public class NoteManager : MonoBehaviour
    {
        private static NoteManager _instance;

        public static NoteManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("NoteManager");
                    _instance = go.AddComponent<NoteManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private Dictionary<string, string> notesDictionary = new Dictionary<string, string>();

        public void SaveNote(string title, string content)
        {
            if (!string.IsNullOrEmpty(title))
            {
                notesDictionary[title] = content;
            }
        }

        public string LoadNoteContent(string title)
        {
            if (notesDictionary.ContainsKey(title))
            {
                return notesDictionary[title];
            }
            return "";
        }
    }
}
