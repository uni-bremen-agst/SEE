using SEE.DataModel.DG;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.UI.Window.NoteWindow
{
    /// <summary>
    /// Manages notes, including storage, and retrieval.
    /// This class also handles the outlining of nodes and edges in the game.
    /// </summary>
    public class NoteManager : MonoBehaviour
    {
        private static NoteManager instance;

        /// <summary>
        /// The material to outline nodes and edges.
        /// </summary>
        private Material noteMaterial;

        /// <summary>
        /// Provides a instance of the <see cref="NoteManager"/> class.
        /// </summary>
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

        /// <summary>
        /// This Dictionary saves the content of the notes.
        /// </summary>
        public Dictionary<KeyValuePair<string, bool>, string> notesDictionary = new Dictionary<KeyValuePair<string, bool>, string>();

        /// <summary>
        /// List of GameObjects. It is used to check íf the GameObject is highlighted or not.
        /// </summary>
        public List<GameObject> objectList = new List<GameObject>();

        /// <summary>
        /// Finds the <see cref="GameObject"/> associated with the specified <paramref name="graphElementRef"/>
        /// and marks it with an outline.
        /// </summary>
        /// <param name="graphElementRef">Reference to the graph element.</param>
        private void OutlineGameObject(GraphElementRef graphElementRef)
        {
            GameObject gameObject = graphElementRef.gameObject;
            if (gameObject != null)
            {
                noteMaterial = Resources.Load<Material>(gameObject.IsNode() ? "Materials/Outliner_MAT" : "Materials/OutlinerEdge_MAT");
                MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
                if (meshRenderer.materials[meshRenderer.materials.Length - 1].name != noteMaterial.name)
                {
                    objectList.Add(gameObject);
                    Material[] materialsArray = new Material[meshRenderer.materials.Length + 1];
                    Array.Copy(meshRenderer.materials, materialsArray, meshRenderer.materials.Length);
                    materialsArray[meshRenderer.materials.Length] = noteMaterial;
                    meshRenderer.materials = materialsArray;
                }
            }
        }

        /// <summary>
        /// Saves a note in the <see cref="notesDictionary"/>.
        /// </summary>
        /// <param name="graphElementRef">Reference to the graph element (node/edge).</param>
        /// <param name="isPublic">Specifies whether the note is public or private.</param>
        /// <param name="content">Content of the note to save.</param>
        public void SaveNote(GraphElementRef graphElementRef, bool isPublic, string content)
        {
            GraphElement graphElement = graphElementRef.Elem;
            if (!string.IsNullOrEmpty(graphElement.ID))
            {
                KeyValuePair<string, bool> keyPair = new KeyValuePair<string, bool>(graphElement.ID, isPublic);
                notesDictionary[keyPair] = content;
                OutlineGameObject(graphElementRef);
            }
            //Debug.Log("graphRef + isPublic + content " + graphElementRef + isPublic +content);
            //Debug.Log("notesDictionary.length: " + notesDictionary.Count);
        }

        /// <summary>
        /// Loads a note from the <see cref="notesDictionary"/>.
        /// </summary>
        /// <param name="title">Identifier of the node/edge.</param>
        /// <param name="isPublic">Specifies whether to load the public or private note.</param>
        /// <returns>Content of the loaded note.</returns>
        public string LoadNote(string title, bool isPublic)
        {
            KeyValuePair<string, bool> keyPair = new KeyValuePair<string, bool>(title, isPublic);
            if (notesDictionary.ContainsKey(keyPair))
            {
                return notesDictionary[keyPair];
            }
            return "";
        }
    }
}
