using SEE.Game;
using System.Collections;
using UnityEngine;

namespace Assets.SEE.Game
{
    public static class GameDrawableFinder
    {
        public static GameObject Find(string drawableID, string parentDrawableID)
        {
            GameObject drawable = null;
            GameObject[] paintingReceivers = GameObject.FindGameObjectsWithTag(Tags.Drawable);
            ArrayList paintingReceiversList = new ArrayList(paintingReceivers);

            foreach (GameObject paintingReceiver in paintingReceiversList)
            {
                if (parentDrawableID != null && !parentDrawableID.Equals(""))
                {
                    string parentName = paintingReceiver.transform.parent.gameObject.name;
                    if (string.Equals(parentDrawableID, parentName) &&
                          string.Equals(drawableID, paintingReceiver.name))
                    {
                        drawable = paintingReceiver;
                    }
                } else
                {
                    if (string.Equals(drawableID, paintingReceiver.name))
                    {
                        drawable = paintingReceiver;
                    }
                }
            }

            return drawable;
        }

        public static GameObject FindChild(GameObject drawable, string childName)
        {
            Transform[] allChildren = drawable.GetComponentsInChildren<Transform>();
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
        }

        public static bool hasDrawableParent(GameObject child)
        {
            return FindDrawableParent(child) != null;
        }

        public static GameObject FindChildWithTag(GameObject parent, string tag)
        {
            foreach (Transform childTransform in parent.transform)
            {
                if (childTransform.gameObject.CompareTag(tag))
                {
                    return childTransform.gameObject;
                }
            }
            return null;
        }

        public static bool hasChildWithTag(GameObject parent, string tag)
        {
            return FindChildWithTag(parent, tag) != null;
        }

        public static bool hasAParent(GameObject drawable)
        {
            return drawable.transform.parent != null;
        }

        public static string GetDrawableParentName(GameObject drawable)
        {
            return hasAParent(drawable) ? drawable.transform.parent.name : "";
        }
    }
}