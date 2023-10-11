using SEE.Net.Actions.Drawable;
using SEE.Game;
using System.Collections;
using UnityEngine;
using SEE.Game.Drawable.Configurations;

namespace Assets.SEE.Game.Drawable
{
    public static class DrawableSynchronizer
    {

        public static void Synchronize()
        {
            new SynchronizeCurrentOrderInLayer(ValueHolder.currentOrderInLayer).Execute();

            // TODO First add the drawables that were spawned during runtime. 

            ArrayList drawables = new ArrayList(GameObject.FindGameObjectsWithTag(Tags.Drawable));
            foreach (GameObject drawable in drawables)
            {
                if (GameDrawableFinder.GetAttachedObjectsObject(drawable) != null)
                {
                    string drawableParent = GameDrawableFinder.GetDrawableParentName(drawable);
                    Transform[] allChildren = GameDrawableFinder.GetAttachedObjectsObject(drawable).GetComponentsInChildren<Transform>();

                    foreach (Transform childTransform in allChildren)
                    {
                        GameObject child = childTransform.gameObject;

                        switch (child.tag)
                        {
                            case Tags.Line:
                                new DrawOnNetAction(drawable.name, drawableParent, Line.GetLine(child)).Execute();
                                break;
                            case Tags.DText:
                                new WriteTextNetAction(drawable.name, drawableParent, Text.GetText(child)).Execute();
                                break;
                                // TODO Add other DrawableTypes
                        }
                    }
                }
            }
        }
    }
}