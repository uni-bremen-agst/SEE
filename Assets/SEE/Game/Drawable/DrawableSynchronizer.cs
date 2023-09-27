using Assets.SEE.Net.Actions.Whiteboard;
using SEE.Game;
using System.Collections;
using UnityEngine;

namespace Assets.SEE.Game.Drawable
{
    public static class DrawableSynchronizer
    {
        public static void Synchronize()
        {
            // TODO First add the drawables that were spawned during runtime. 

            ArrayList paintingReceiversList = new ArrayList(GameObject.FindGameObjectsWithTag(Tags.Drawable));
            foreach (GameObject drawable in paintingReceiversList)
            {
                if (GameDrawableFinder.GetAttachedObjectsObject(drawable) != null)
                {
                    Transform[] allChildren = GameDrawableFinder.GetAttachedObjectsObject(drawable).GetComponentsInChildren<Transform>();

                    foreach (Transform childTransform in allChildren)
                    {
                        GameObject child = childTransform.gameObject;

                        switch (child.tag)
                        {
                            case Tags.Line:
                                new DrawOnNetAction(drawable.name, GameDrawableFinder.GetDrawableParentName(drawable), Line.GetLine(child)).Execute();
                                break;
                                // TODO Add other DrawableTypes
                        }
                    }
                }
            }
        }
    }
}