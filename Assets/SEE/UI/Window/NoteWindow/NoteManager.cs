using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.UI.Window.NoteWindow
{
    public class NoteManager : MonoBehaviour
    {
        private static NoteManager instance;

        private Material noteMaterial;

        public static NoteManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("NoteManager");
                    instance = go.AddComponent<NoteManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        public Dictionary<KeyValuePair<string, bool>, string> notesDictionary = new Dictionary<KeyValuePair<string, bool>, string>();

        public List<GameObject> objectList = new List<GameObject>();

        private void FindGameObjects()
        {
            noteMaterial = Resources.Load<Material>("Materials/Outliner_MAT");

            foreach (KeyValuePair<KeyValuePair<string, bool>, string> kv in notesDictionary)
            {
                GameObject gameObject = GameObject.Find(kv.Key.Key);
                if(gameObject != null)
                {
                    MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
                    objectList.Add(gameObject);

                    Material[] materialsArray = new Material[meshRenderer.materials.Length + 1];
                    Debug.Log("materialsArray.Length: " + materialsArray.Length);

                    for(int i = 0; i < meshRenderer.materials.Length; i++)
                    {
                        materialsArray.SetValue(meshRenderer.materials[i], i);
                    }
                    materialsArray.SetValue(noteMaterial, 2);

                    meshRenderer.materials = materialsArray;
                    Debug.Log("MeshRenderer.Materials.Length: " + meshRenderer.materials.Length);

                }

            }
        }

        public void SaveNote(string title, bool isPublic, string content)
        {
            if (!string.IsNullOrEmpty(title))
            {
                KeyValuePair<string, bool> keyPair = new KeyValuePair<string, bool>(title, isPublic);
                notesDictionary[keyPair] = content;
                FindGameObjects();
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
