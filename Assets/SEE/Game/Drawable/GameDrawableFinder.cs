using SEE.Game;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.SEE.Game
{
    public static class GameDrawableFinder
    {
        public static GameObject Find(string drawableID, string parentDrawableID)
        {
            GameObject searchedDrawable = null;
            List<GameObject> drawables = new (GameObject.FindGameObjectsWithTag(Tags.Drawable));

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
                } else
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
                GameObject attachedObjects = FindChildWithTag(GetHighestParent(parent), Tags.AttachedObjects); //neu
                if (attachedObjects != null)
                {
                    allChildren = attachedObjects.GetComponentsInChildren<Transform>();//parent.GetComponentsInChildren<Transform>();
                } else
                {
                    allChildren = parent.GetComponentsInChildren<Transform>();
                }
            } else
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

        public static GameObject FindDrawableParent(GameObject child)
        {
            /*
            Transform transform = child.transform;
            while(transform.parent != null)
            {
                if (transform.parent.gameObject.CompareTag(Tags.Drawable))
                {
                    return transform.parent.gameObject;
                }
                transform = transform.parent;
            }
            return null;
            */
            return FindChildWithTag(GetHighestParent(child), Tags.Drawable);
        }

        public static bool hasDrawableParent(GameObject child)
        {
            if (hasParentWithTag(child, Tags.AttachedObjects)) {
                return FindDrawableParent(child) != null;
            } else
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

        public static bool hasChildWithTag(GameObject parent, string tag)
        {
            return FindChildWithTag(parent, tag) != null;
        }

        public static bool hasAParent(GameObject child)
        {
            return child.transform.parent != null;
        }

        public static bool hasParentWithTag(GameObject child, String tag)
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
            return hasAParent(drawable) ? drawable.transform.parent.name : "";
        }

        public static GameObject GetHighestParent(GameObject child)
        {/*
            if (drawable.CompareTag(Tags.Drawable))
            {
                if (drawable.transform.parent != null)
                {
                    return drawable.transform.parent.gameObject;
                }
            } else
            {
                drawable = GetHighestParent(FindDrawableParent(drawable));
            }
            return drawable;
            */
            if (child.transform.parent != null)
            {
                return GetHighestParent(child.transform.parent.gameObject);
            } else
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