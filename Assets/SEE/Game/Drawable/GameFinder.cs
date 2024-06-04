using System.Collections.Generic;
using SEE.Game.Drawable.Configurations;
using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// This class provides methods to search.
    /// </summary>
    public static class GameFinder
    {
        /// <summary>
        /// Searches for the drawable in the scene.
        /// </summary>
        /// <param name="drawableID">the drawable id.</param>
        /// <param name="parentDrawableID">the parent id of the drawable.</param>
        /// <returns>The sought-after drawable, if found. Otherwise, null.</returns>
        public static GameObject FindDrawable(string drawableID, string parentDrawableID)
        {
            GameObject searchedDrawable = null;
            /// Gets all drawables of the scene.
            List<GameObject> drawables = new(GameObject.FindGameObjectsWithTag(Tags.Drawable));

            foreach (GameObject drawable in drawables)
            {
                /// Block for searching includes the parend id.
                if (parentDrawableID != null && !parentDrawableID.Equals("") 
                    && drawable.transform.parent != null)
                {
                    string parentName = drawable.transform.parent.gameObject.name;
                    if (string.Equals(parentDrawableID, parentName) &&
                          string.Equals(drawableID, drawable.name))
                    {
                        searchedDrawable = drawable;
                    }
                }
                else
                {
                    /// Block for searching without parent id.
                    /// Currently not used, as the drawables from 
                    /// the Whiteboard and Sticky Notes each have a parent.
                    if (string.Equals(drawableID, drawable.name))
                    {
                        searchedDrawable = drawable;
                    }
                }
            }
            return searchedDrawable;
        }

        /// <summary>
        /// Searches for a child with the given name.
        /// </summary>
        /// <param name="parent">Must be an object of the drawable holder.</param>
        /// <param name="childName">The id of the searched child.</param>
        /// <returns>The searched child, if found. Otherwise, null</returns>
        public static GameObject FindChild(GameObject parent, string childName)
        {
            Transform[] allChildren;
            if (parent.CompareTag(Tags.Drawable))
            {
                GameObject attachedObjects = FindChildWithTag(GetHighestParent(parent), Tags.AttachedObjects);
                if (attachedObjects != null)
                {
                    allChildren = attachedObjects.GetComponentsInChildren<Transform>();
                }
                else
                {
                    allChildren = parent.GetComponentsInChildren<Transform>();
                }
            }
            else
            {
                allChildren = parent.GetComponentsInChildren<Transform>();
            }
            foreach (Transform child in allChildren)
            {
                if (child.gameObject.name.Equals(childName))
                {
                    return child.gameObject;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the drawable of the given object.
        /// </summary>
        /// <param name="obj">An object of the searched drawable.</param>
        /// <returns>The drawable object.</returns>
        public static GameObject GetDrawable(GameObject obj)
        {
            return FindChildWithTag(GetHighestParent(obj), Tags.Drawable);
        }

        /// <summary>
        /// Query whether the given object is located on a drawable.
        /// </summary>
        /// <param name="child">The child to be examined.</param>
        /// <returns>true, if the child has a drawable. Otherwise false</returns>
        public static bool hasDrawable(GameObject child)
        {
            if (hasParentWithTag(child, Tags.AttachedObjects))
            {
                return GetDrawable(child) != null;
            }
            else
            {
                return false;
            }
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
            return GetDrawable(component) != null;
        }

        /// <summary>
        /// Searches for a child with a specific tag.
        /// </summary>
        /// <param name="parent">The parent of the children.</param>
        /// <param name="tag">The tag to be searched.</param>
        /// <returns>the first founded child with the searched tag.</returns>
        public static GameObject FindChildWithTag(GameObject parent, string tag)
        {
            Transform[] allChildren = parent.GetComponentsInChildren<Transform>();
            foreach (Transform childTransform in allChildren)
            {
                if (childTransform.gameObject.CompareTag(tag))
                {
                    return childTransform.gameObject;
                }
            }
            return null;
        }

        /// <summary>
        /// Searches for all children with the given tag.
        /// </summary>
        /// <param name="parent">The parent of the children</param>
        /// <param name="tag">The tag to be searched</param>
        /// <returns>All children with the searched tag.</returns>
        public static List<GameObject> FindAllChildrenWithTag(GameObject parent, string tag)
        {
            List<GameObject> gameObjects = new();
            Transform[] allChildren = parent.GetComponentsInChildren<Transform>();
            foreach (Transform childTransform in allChildren)
            {
                if (childTransform.gameObject.CompareTag(tag))
                {
                    gameObjects.Add(childTransform.gameObject);
                }
            }
            return gameObjects;
        }

        /// <summary>
        /// Searches for all children with the given tag (<paramref name="childTag"/>) except the parent 
        /// has a specific tag (<paramref name="parentTag"/>).
        /// Will be used for mind map nodes.
        /// </summary>
        /// <param name="parent">The parent of the children</param>
        /// <param name="childTag">The tag to be searched</param>
        /// <param name="parentTag">The execpt tag</param>
        /// <returns>All children with the searched tag, except those whose parents have the specific tag.</returns>
        public static List<GameObject> FindAllChildrenWithTagExceptParentHasTag(GameObject parent, 
            string childTag, string parentTag)
        {
            List<GameObject> gameObjects = new();
            Transform[] allChildren = parent.GetComponentsInChildren<Transform>();
            foreach (Transform childTransform in allChildren)
            {
                if (childTransform.gameObject.CompareTag(childTag) 
                    && !childTransform.parent.gameObject.CompareTag(parentTag))
                {
                    gameObjects.Add(childTransform.gameObject);
                }
            }
            return gameObjects;
        }

        /// <summary>
        /// Query whether the parent has a child with the searched tag.
        /// </summary>
        /// <param name="parent">The parent</param>
        /// <param name="tag">The tag to be searched.</param>
        /// <returns>true, if a child with the searched tag exists.</returns>
        public static bool hasChildWithTag(GameObject parent, string tag)
        {
            return FindChildWithTag(parent, tag) != null;
        }

        /// <summary>
        /// Query wheter the child has a parent.
        /// </summary>
        /// <param name="child">The child.</param>
        /// <returns>true, if the child has a parent.</returns>
        public static bool hasAParent(GameObject child)
        {
            return child.transform.parent != null;
        }

        /// <summary>
        /// Query wheter the child has a parent with the given tag.
        /// </summary>
        /// <param name="child">The child.</param>
        /// <param name="tag">The tag to be searched.</param>
        /// <returns>true, if a parent with the given tag exists.</returns>
        public static bool hasParentWithTag(GameObject child, string tag)
        {
            Transform transform = child.transform;
            while (transform.parent != null)
            {
                if (transform.parent.gameObject.CompareTag(tag))
                {
                    return true;
                }
                transform = transform.parent;
            }
            return false;
        }

        /// <summary>
        /// Gets the parent name of the drawable.
        /// </summary>
        /// <param name="drawable">The drawable</param>
        /// <returns>The name, empty if no parent exists.</returns>
        public static string GetDrawableParentName(GameObject drawable)
        {
            if (drawable.CompareTag(Tags.Drawable))
            {
                return hasAParent(drawable) ? drawable.transform.parent.name : "";
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Gets the highest parent of the given child.
        /// Usually the drawable holder.
        /// </summary>
        /// <param name="child">The child.</param>
        /// <returns>The highest parent</returns>
        public static GameObject GetHighestParent(GameObject child)
        {
            if (child.transform.parent != null)
            {
                return GetHighestParent(child.transform.parent.gameObject);
            }
            else
            {
                return child;
            }
        }

        /// <summary>
        /// Gets the attached objects object.
        /// Below this game object, the <see cref="DrawableType"/> objects are placed.
        /// </summary>
        /// <param name="obj">An object within the drawable holder.</param>
        /// <returns>the object which holds the <see cref="DrawableType"/> objects of a drawable.</returns>
        public static GameObject GetAttachedObjectsObject(GameObject obj)
        {
            return FindChildWithTag(GetHighestParent(obj), Tags.AttachedObjects);
        }
    }
}