﻿using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// This class provides methods to search for <see cref="DrawableType"/>s.
    /// </summary>
    public static class GameFinder
    {
        /// <summary>
        /// Searches for the drawable surface in the scene.
        /// </summary>
        /// <param name="surfaceID">the drawable surface id.</param>
        /// <param name="surfaceParentID">the parent id of the drawable surface.</param>
        /// <param name="useFindWithTagList">Option to select which list should be searched.
        /// By default, the <see cref="ValueHolder.DrawableSurfaces"> is used.
        /// When this option is set to true, the expensive <see cref="GameObject.FindGameObjectsWithTag(string)"/> functionality is used.</param>
        /// <returns>The sought-after drawable, if found. Otherwise, null.</returns>
        /// <remarks>This method will iterate over all game objects in the
        /// scene and, hence, is expensive.</remarks>
        public static GameObject FindDrawableSurface(string surfaceID, string surfaceParentID, bool useFindWithTagList = false)
        {
            GameObject searchedSurface = null;
            List<GameObject> surfaces;

            /// Selection of the list to be checked.
            if (!useFindWithTagList)
            {
                surfaces = ValueHolder.DrawableSurfaces;
            }
            else
            {
                /// Gets all drawables of the scene.
                surfaces = new(GameObject.FindGameObjectsWithTag(Tags.Drawable));
            }
            /// Search for the desired drawable surface.
            foreach (GameObject surface in surfaces)
            {
                /// Block for searching includes the parend id.
                if (!string.IsNullOrWhiteSpace(surfaceParentID)
                    && surface.transform.parent != null)
                {
                    string parentName = surface.transform.parent.gameObject.name;
                    if (surfaceParentID == parentName && surfaceID == surface.name)
                    {
                        searchedSurface = surface;
                    }
                }
                else
                {
                    /// Block for searching without parent id.
                    /// Currently not used, as the drawables from
                    /// the Whiteboard and Sticky Notes each have a parent.
                    if (surfaceID == surface.name)
                    {
                        searchedSurface = surface;
                    }
                }
            }
            /// If the search with the <see cref="ValueHolder.DrawableSurfaces"> list is unsuccessful,
            /// initiate a search using the <see cref="GameObject.FindGameObjectsWithTag(string)"/> method.
            if (!useFindWithTagList && searchedSurface == null)
            {
                return FindDrawableSurface(surfaceID, surfaceParentID, true);
            }
            return searchedSurface;
        }

        /// <summary>
        /// Searches for a child with the given name.
        /// </summary>
        /// <param name="parent">Must be an object of the drawable holder.</param>
        /// <param name="childName">The id of the searched child.</param>
        /// <param name="includeInactive">whether the inactive objects should be included.</param>
        /// <returns>The searched child, if found. Otherwise, null</returns>
        public static GameObject FindChild(GameObject parent, string childName, bool includeInactive = true)
        {
            GameObject attachedObjects = parent.GetRootParent().FindDescendantWithTag(Tags.AttachedObjects);
            return attachedObjects != null?
                attachedObjects.FindDescendant(childName, includeInactive)
                : parent.FindDescendant(childName, includeInactive);
        }

        /// <summary>
        /// Gets the drawable surface of the given object.
        /// </summary>
        /// <param name="obj">An object of the searched drawable.</param>
        /// <returns>The drawable object.</returns>
        public static GameObject GetDrawableSurface(GameObject obj)
        {
            return obj.GetRootParent().FindDescendantWithTag(Tags.Drawable);
        }

        /// <summary>
        /// Query whether the given object is located on a drawable surface.
        /// </summary>
        /// <param name="child">The child to be examined.</param>
        /// <returns>true, if the child has a drawable. Otherwise false</returns>
        public static bool HasDrawableSurface(GameObject child)
        {
            if (child.HasParentWithTag(Tags.AttachedObjects))
            {
                return GetDrawableSurface(child) != null;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Query wheter the given object is a drawable surface or
        /// is located on one.
        /// </summary>
        /// <param name="obj">The object to be examined.</param>
        /// <returns>true if the object is a drawable surface or if is located on one.</returns>
        public static bool IsOrHasDrawableSurface(GameObject obj)
        {
            return obj.CompareTag(Tags.Drawable) || HasDrawableSurface(obj);
        }

        /// <summary>
        /// Query whether the given game object is part of a drawable.
        /// It is checked whether the highest game object in the hierarchy of
        /// the given game object contains a drawable child object.
        /// </summary>
        /// <param name="component">The game object to be checked.</param>
        /// <returns>true, if a drawable will be found. Otherwise false</returns>
        public static bool IsPartOfADrawable(GameObject component)
        {
            return GetDrawableSurface(component) != null;
        }

        /// <summary>
        /// Query to check if a game object have children.
        /// Greater than 1 because the transform of the parent is included.
        /// </summary>
        /// <param name="parent">The parent object.</param>
        /// <param name="includeInactive">Whether inactive children should be considered.</param>
        /// <returns>True if the object have children.</returns>
        public static bool HaveChildren(GameObject parent, bool includeInactive = false)
        {
            return parent.GetComponentsInChildren<Transform>(includeInactive).Length > 1;
        }

        /// <summary>
        /// Query whether the child has a parent.
        /// </summary>
        /// <param name="child">The child.</param>
        /// <returns>true, if the child has a parent.</returns>
        public static bool HasParent(GameObject child)
        {
            return child.transform.parent != null;
        }

        /// <summary>
        /// Returns the next higher <see cref="DrawableType"/> object.
        /// It will be needed if a child object of the <see cref="DrawableType"/> object was selected.
        /// </summary>
        /// <param name="obj">The currently selected obj.</param>
        /// <returns>The next higher <see cref="DrawableType"/> object or null.</returns>
        public static GameObject GetDrawableTypObject(GameObject obj)
        {
            if (Tags.DrawableTypes.Contains(obj.tag))
            {
                return obj;
            }
            else if (obj.transform.parent != null
                && Tags.DrawableTypes.Contains(obj.transform.parent.tag))
            {
                return obj.transform.parent.gameObject;
            }
            else if (obj.transform.parent != null)
            {
                return GetDrawableTypObject(obj.transform.parent.gameObject);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the parent name of the drawable surface.
        /// </summary>
        /// <param name="surface">The drawable surface</param>
        /// <returns>The name, empty if no parent exists.</returns>
        public static string GetDrawableSurfaceParentName(GameObject surface)
        {
            if (surface.CompareTag(Tags.Drawable))
            {
                return HasParent(surface) ? surface.transform.parent.name : "";
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Gets the unique ID of a drawable surface.
        /// </summary>
        /// <param name="surface">The drawable surface.</param>
        /// <returns>The unique ID.</returns>
        public static string GetUniqueID(GameObject surface)
        {
            return !string.IsNullOrEmpty(GetDrawableSurfaceParentName(surface)) ?
                GetDrawableSurfaceParentName(surface) : surface.name;
        }

        /// <summary>
        /// Gets the parent object of the drawable surface.
        /// </summary>
        /// <param name="obj">An object of the drawable.</param>
        /// <returns>The parent object, null if no parent exists.</returns>
        public static GameObject GetDrawableSurfaceParent(GameObject obj)
        {
            GameObject surface = GetDrawableSurface(obj);
            if (surface != null && HasParent(surface))
            {
                return surface.transform.parent.gameObject;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if the drawable is a whiteboard.
        /// </summary>
        /// <param name="obj">An object of the drawable.</param>
        /// <returns>True if an object with a <see cref="Tags.Whiteboard"/> exists in the object tree.</returns>
        public static bool IsWhiteboard(GameObject obj)
        {
            return obj.GetRootParent().FindDescendantWithTag(Tags.Whiteboard) != null;
        }

        /// <summary>
        /// Checks if the drawable is a sticky note.
        /// </summary>
        /// <param name="obj">An object of the drawable.</param>
        /// <returns>True if an object with a <see cref="Tags.StickyNote"/> exists in the object tree.</returns>
        public static bool IsStickyNote(GameObject obj)
        {
            return obj.GetRootParent().FindDescendantWithTag(Tags.StickyNote) != null;
        }

        /// <summary>
        /// Gets the attached objects of <paramref name="obj"/>.
        /// Below this game object, the <see cref="DrawableType"/> objects are placed.
        /// </summary>
        /// <param name="obj">An object within a drawable holder.</param>
        /// <returns>the object which holds the <see cref="DrawableType"/> objects of a drawable.</returns>
        public static GameObject GetAttachedObjectsObject(GameObject obj)
        {
            return obj.GetRootParent().FindDescendantWithTag(Tags.AttachedObjects);
        }

        /// <summary>
        /// Gets a list with all <see cref="DrawableType"/> game objects of a the given <paramref name="page"/>.
        /// </summary>
        /// <param name="obj">An object of the drawable.</param>
        /// <param name="page">The page whose <see cref="DrawableType"/> objects are to be determined.</param>
        /// <returns>A list with all <see cref="DrawableType"/> game objects of the chosen page.</returns>
        public static List<GameObject> GetDrawableTypesOfPage(GameObject obj, int page)
        {
            List<GameObject> typesOfPage = new();
            GameObject attached = GetAttachedObjectsObject(obj);
            if (attached != null)
            {
                foreach (Transform typeTransform in attached.GetComponentInChildren<Transform>(true))
                {
                    if (typeTransform.GetComponent<AssociatedPageHolder>() != null
                        && typeTransform.GetComponent<AssociatedPageHolder>().AssociatedPage == page)
                    {
                        typesOfPage.Add(typeTransform.gameObject);
                    }
                }
            }
            return typesOfPage;
        }
    }
}