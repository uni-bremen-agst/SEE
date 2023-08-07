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
    }
}