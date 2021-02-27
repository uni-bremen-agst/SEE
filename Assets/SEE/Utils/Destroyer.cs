﻿using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.GO;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// Functions to destroy game objects in game or editor mode.
    /// </summary>
    public static class Destroyer
    {
        /// <summary>
        /// Destroys given game object using UnityEngine.Object when in
        /// game mode or UnityEngine.Object.DestroyImmediate when in editor mode.
        /// </summary>
        /// <param name="gameObject">game object to be destroyed</param>
        public static void DestroyGameObject(GameObject gameObject)
        {
            // We must use DestroyImmediate when we are in the editor mode.
            if (Application.isPlaying)
            {
                // playing either in a built player or in the player of the editor
                UnityEngine.Object.Destroy(gameObject);
            }
            else
            {
                // game is not played; we are in the editor mode
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        /// Destroys given <paramref name="component"/> using UnityEngine.Object when in
        /// game mode or UnityEngine.Object.DestroyImmediate when in editor mode.
        /// </summary>
        /// <param name="component">component to be destroyed</param>
        public static void DestroyComponent(Component component)
        {
            // We must use DestroyImmediate when we are in the editor mode.
            if (Application.isPlaying)
            {
                // playing either in a built player or in the player of the editor
                UnityEngine.Object.Destroy(component);
            }
            else
            {
                // game is not played; we are in the editor mode
                UnityEngine.Object.DestroyImmediate(component);
            }
        }

        /// <summary>
        /// Destroys given <paramref name="gameObject"/> with all its attached children
        /// and incoming and outgoing edges. This method is intended to be used for
        /// game objects representing graph nodes having a component 
        /// <see cref=">SEE.DataModel.NodeRef"/>. If the <paramref name="gameObject"/> 
        /// does not have a <see cref=">SEE.DataModel.NodeRef"/> component, nothing
        /// happens.
        /// </summary>
        /// <param name="gameObject">game object to be destroyed</param>
        public static void DestroyGameObjectWithChildren(GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out NodeRef nodeRef))
            {
                for (int i = 0; i < gameObject.transform.childCount; i++) {
                    GameObject child = gameObject.transform.GetChild(i).gameObject;
                    DestroyGameObjectWithChildren(child);
                }
                DestroyEdges(gameObject);
                DestroyGameObject(gameObject);
            }
        }

        /// <summary>
        /// Returns the IDs of all incoming and outgoing edges for <paramref name="nodeRef"/>.
        /// </summary>
        /// <param name="nodeRef">node whose incoming and outgoing edges are requested</param>
        /// <returns>IDs of all incoming and outgoing edges</returns>
        private static HashSet<string> GetEdgeIds(NodeRef nodeRef)
        {
            HashSet<String> edgeIDs = new HashSet<string>();
            foreach (Edge edge in nodeRef.Value.Outgoings)
            {
                edgeIDs.Add(edge.ID);
            }
            foreach (Edge edge in nodeRef.Value.Incomings)
            {
                edgeIDs.Add(edge.ID);
            }
            return edgeIDs;
        }

        /// <summary>
        /// Searches through all childs of given <paramref name="gameObject"/>
        /// and deletes all Edges attached to given childs.
        /// 
        /// This method is intended to be used for game objects representing graph nodes
        /// having a component <see cref=">SEE.DataModel.NodeRef"/>. If the 
        /// <paramref name="gameObject"/> does not have a <see cref=">SEE.DataModel.NodeRef"/>
        /// component, nothing happens.
        /// </summary>
        /// <param name="gameObject">game node whose incoming and outgoing edges are to be destroyed</param>
        private static void DestroyEdges(GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out NodeRef nodeRef))
            {
                HashSet<String> edgeIDs = GetEdgeIds(nodeRef);

                foreach (GameObject edge in GameObject.FindGameObjectsWithTag(Tags.Edge))
                {
                    if (edge.activeInHierarchy && edgeIDs.Contains(edge.name))
                    {
                        Destroyer.DestroyGameObject(edge);
                    }
                }
            }
        }
    }
}