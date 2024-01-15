using SEE.Game.Drawable.Configurations;
using SEE.Net.Actions.Drawable;
using System.Collections;
using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// This class provides a synchronizer for the order in layer, the <see cref="DrawableType"/> objects as well as sticky notes.
    /// </summary>
    public static class DrawableSynchronizer
    {
        /// <summary>
        /// First, the Order in Layer value is synchronized over the network. 
        /// Then, the non-existing drawables are provided as sticky notes. 
        /// (It can only be sticky notes since only sticky notes can be spawned during runtime). 
        /// Finally, for each drawable, the corresponding DrawableType objects are synchronized.
        /// 
        /// Note: The issue here is that the synchronizer is always executed when a new player joins, 
        /// and synchronization is performed for all players.
        /// This can lead to delays, especially when there are many images in the game world.
        /// </summary>
        public static void Synchronize()
        {
            new SynchronizeCurrentOrderInLayer(ValueHolder.currentOrderInLayer).Execute();

            ArrayList drawables = new(GameObject.FindGameObjectsWithTag(Tags.Drawable));
            foreach (GameObject drawable in drawables)
            {
                if (GameFinder.GetDrawableParentName(drawable).Contains(ValueHolder.StickyNotePrefix))
                {
                    new StickyNoteSpawnNetAction(DrawableConfigManager.GetDrawableConfig(drawable)).Execute();
                }

                if (GameFinder.GetAttachedObjectsObject(drawable) != null)
                {
                    string drawableParent = GameFinder.GetDrawableParentName(drawable);
                    Transform[] allChildren = GameFinder.GetAttachedObjectsObject(drawable)
                        .GetComponentsInChildren<Transform>();

                    foreach (Transform childTransform in allChildren)
                    {
                        GameObject child = childTransform.gameObject;

                        switch (child.tag)
                        {
                            case Tags.Line:
                                new DrawNetAction(drawable.name, drawableParent, 
                                    LineConf.GetLine(child)).Execute();
                                break;
                            case Tags.DText:
                                new WriteTextNetAction(drawable.name, drawableParent, 
                                    TextConf.GetText(child)).Execute();
                                break;
                            case Tags.Image:
                                new AddImageNetAction(drawable.name, drawableParent, 
                                    ImageConf.GetImageConf(child)).Execute();
                                break;
                            case Tags.MindMapNode:
                                new MindMapCreateNodeNetAction(drawable.name, drawableParent, 
                                    MindMapNodeConf.GetNodeConf(child)).Execute();
                                break;
                        }
                    }
                }
            }
        }
    }
}