using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.UI.Window.NoteWindow
{
    public class NoteManager : MonoBehaviour
    {
        private static NoteManager instance;

        public static NoteManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("NoteManager");
                    instance = go.AddComponent<NoteManager>();
                    //DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private Dictionary<KeyValuePair<string, bool>, string> notesDictionary = new Dictionary<KeyValuePair<string, bool>, string>();

        public void SaveNote(string title, bool isPublic, string content)
        {
            if (!string.IsNullOrEmpty(title))
            {
                KeyValuePair<string, bool> keyPair = new KeyValuePair<string, bool>(title, isPublic);
                notesDictionary[keyPair] = content;
                //Debug.Log("DictionaryLength: " + notesDictionary.Count);
            }
        }

        public string LoadNote(string title, bool isPublic)
        {
            KeyValuePair<string, bool> keyPair = new KeyValuePair<string, bool>(title, isPublic);
            if (notesDictionary.ContainsKey(keyPair))
            {
                return notesDictionary[keyPair];
            }
            return "";
        }

        private void OnApplicationQuit()
        {
            Destroy(gameObject); // Löscht das GameObject und somit auch die Komponente
        }
    }
}
