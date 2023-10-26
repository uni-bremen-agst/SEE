using SEE.Net.Actions.Drawable;
using SEE.Game;
using System.Collections;
using UnityEngine;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable;

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
                if (GameFinder.GetAttachedObjectsObject(drawable) != null)
                {
                    string drawableParent = GameFinder.GetDrawableParentName(drawable);
                    Transform[] allChildren = GameFinder.GetAttachedObjectsObject(drawable).GetComponentsInChildren<Transform>();

                    foreach (Transform childTransform in allChildren)
                    {
                        GameObject child = childTransform.gameObject;

                        switch (child.tag)
                        {
                            case Tags.Line:
                                new DrawOnNetAction(drawable.name, drawableParent, LineConf.GetLine(child)).Execute();
                                break;
                            case Tags.DText:
                                new WriteTextNetAction(drawable.name, drawableParent, TextConf.GetText(child)).Execute();
                                break;
                            case Tags.Image:
                                new AddImageNetAction(drawable.name, drawableParent, ImageConf.GetImageConf(child)).Execute();
                                break;
                        }
                    }
                }
            }
        }
    }
}