using SEE.DataModel.DG;
using SEE.GO;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Functions to destroy GameObjects.
    /// </summary>
    public static class Destroyer
    {
        /// <summary>
        /// Destroys given game object using UnityEngine.Object when in
        /// game mode or UnityEngine.Object.DestroyImmediate when in editor mode.
        /// </summary>
        /// <param name="gameObject"></param>
        public static void DestroyGameObject(GameObject gameObject)
        {
            // We must use DestroyImmediate when we are in the editor mode.
            if (Application.isPlaying)
            {
                // playing either in a built player or in the player of the editor
                Object.Destroy(gameObject);
            }
            else
            {
                // game is not played; we are in the editor mode
                Object.DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        /// Destroys given <paramref name="component"/> using UnityEngine.Object when in
        /// game mode or UnityEngine.Object.DestroyImmediate when in editor mode.
        /// </summary>
        /// <param name="component"></param>
        public static void DestroyComponent(Component component)
        {
            // We must use DestroyImmediate when we are in the editor mode.
            if (Application.isPlaying)
            {
                // playing either in a built player or in the player of the editor
                Object.Destroy(component);
            }
            else
            {
                // game is not played; we are in the editor mode
                Object.DestroyImmediate(component);
            }
        }

        /// <summary>
        /// Destroys given <paramref name="gameObject"/> with all his attached childs.
        /// </summary>
        /// <param name="gameObject"></param>
        public static void DestroyGameObjectWithChilds(GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out NodeRef nodeRef))
            {
                for (int i = 0; i < gameObject.transform.childCount; i++) {
                    GameObject child = gameObject.transform.GetChild(i).gameObject;
                    DestroyGameObjectWithChilds(child);
                }
                DestroyEdges(gameObject);
                DestroyGameObject(gameObject);
            }
        }

        /// <summary>
        /// Searches all Edges attached to given <paramref name="nodeRef"/> 
        /// and returns their Ids.
        /// </summary>
        /// <param name="nodeRef"></param>
        private static List<string> GetEdgeIds(NodeRef nodeRef)
        {
            List<String> edgeIDs = new List<string>();
            foreach (Edge edge in nodeRef.node.Outgoings)
            {
                edgeIDs.Add(edge.ID);
            }
            foreach (Edge edge in nodeRef.node.Incomings)
            {
                edgeIDs.Add(edge.ID);
            }
            return edgeIDs;
        }

        /// <summary>
        /// Searches through all childs of given <paramref name="gameObject"/>
        /// and deletes all Edges attached to given childs.
        /// </summary>
        /// <param name="gameObject"></param>
        private static void DestroyEdges(GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out NodeRef nodeRef))
            {
                List<String> edgeIDs = GetEdgeIds(nodeRef);

                GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
                foreach (GameObject go in allObjects)
                {
                    if (go.activeInHierarchy)
                    {
                        foreach (String edgeID in edgeIDs)
                        {
                            if (edgeID.Equals(go.name))
                            {
                                Destroyer.DestroyGameObject(go);
                            }
                        }
                    }
                }
            }
        }
    }
}