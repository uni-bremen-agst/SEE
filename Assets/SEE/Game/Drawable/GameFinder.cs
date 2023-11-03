using SEE.Game;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.Drawable
{
    public static class GameFinder
    {
        public static GameObject Find(string drawableID, string parentDrawableID)
        {
            GameObject searchedDrawable = null;
            List<GameObject> drawables = new(GameObject.FindGameObjectsWithTag(Tags.Drawable));

            foreach (GameObject drawable in drawables)
            {
                if (parentDrawableID != null && !parentDrawableID.Equals(""))
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
                    if (string.Equals(drawableID, drawable.name))
                    {
                        searchedDrawable = drawable;
                    }
                }
            }

            return searchedDrawable;
        }

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

        public static GameObject FindDrawable(GameObject child)
        {
            return FindChildWithTag(GetHighestParent(child), Tags.Drawable);
        }

        public static bool hasDrawable(GameObject child)
        {
            if (hasParentWithTag(child, Tags.AttachedObjects))
            {
                return FindDrawable(child) != null;
            }
            else
            {
                return false;
            }
        }

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

        // NEEDED FOR MINDMAP NODE
        public static List<GameObject> FindAllChildrenWithTagExceptParentHasTag(GameObject parent, string childTag, string parentTag)
        {
            List<GameObject> gameObjects = new();
            Transform[] allChildren = parent.GetComponentsInChildren<Transform>();
            foreach (Transform childTransform in allChildren)
            {
                if (childTransform.gameObject.CompareTag(childTag) && !childTransform.parent.gameObject.CompareTag(parentTag))
                {
                    gameObjects.Add(childTransform.gameObject);
                }
            }
            return gameObjects;
        }

        public static bool hasChildWithTag(GameObject parent, string tag)
        {
            return FindChildWithTag(parent, tag) != null;
        }

        public static bool hasAParent(GameObject child)
        {
            return child.transform.parent != null;
        }

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

        public static GameObject GetAttachedObjectsObject(GameObject drawable)
        {
            return FindChildWithTag(GetHighestParent(drawable), Tags.AttachedObjects);
        }
    }
}