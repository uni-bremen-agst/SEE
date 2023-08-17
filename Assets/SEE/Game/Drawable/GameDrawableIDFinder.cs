using SEE.Game;
using System.Collections;
using UnityEngine;

namespace Assets.SEE.Game
{
    public static class GameDrawableIDFinder
    {
        public static GameObject Find(string drawableID, string parentDrawableID)
        {
            GameObject drawable = null;
            GameObject[] paintingReceivers = GameObject.FindGameObjectsWithTag("Drawable");
            ArrayList paintingReceiversList = new ArrayList(paintingReceivers);

            foreach (GameObject paintingReceiver in paintingReceiversList)
            {
                string parentName = paintingReceiver.transform.parent.gameObject.name;
                if (string.Equals(parentDrawableID, parentName) &&
                      string.Equals(drawableID, paintingReceiver.name))
                {
                    drawable = paintingReceiver;
                }
            }

            return drawable;
        }

        public static GameObject FindChild(GameObject drawable, string lineName)
        {
            return (drawable.transform.Find(lineName) != null) ? 
                drawable.transform.Find(lineName).gameObject : null;
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
    }
}