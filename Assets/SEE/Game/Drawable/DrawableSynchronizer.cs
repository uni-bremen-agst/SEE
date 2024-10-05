using SEE.Game.Drawable.Configurations;
using SEE.Net.Actions.Drawable;
using System.Collections;
using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// This class synchronizes the order in layer, the <see cref="DrawableType"/>
    /// objects as well as sticky notes to all clients.
    /// </summary>
    public static class DrawableSynchronizer
    {
        /// <summary>
        /// First, the order in layers is synchronized over the network.
        /// Then, the non-existing drawables are provided as sticky notes.
        /// (It can only be sticky notes since only sticky notes can be spawned during runtime).
        /// Finally, for each drawable, the corresponding DrawableType objects are synchronized.
        ///
        /// Note: The issue here is that the synchronizer is always executed when a new player joins,
        /// and synchronization is performed for all players.
        /// This can lead to delays, especially when there are many images in the game world.
        /// </summary>
        public static void Synchronize(ulong client)
        {
            ulong[] clients = { client };
            new SynchronizeCurrentOrderInLayer(ValueHolder.MaxOrderInLayer).Execute(clients);

            ArrayList drawables = new(GameObject.FindGameObjectsWithTag(Tags.Drawable));
            foreach (GameObject drawable in drawables)
            {
                if (GameFinder.GetDrawableSurfaceParentName(drawable).Contains(ValueHolder.StickyNotePrefix))
                {
                    new StickyNoteSpawnNetAction(DrawableConfigManager.GetDrawableConfig(drawable)).Execute(clients);
                }

                if (GameFinder.GetAttachedObjectsObject(drawable) != null)
                {
                    string drawableParent = GameFinder.GetDrawableSurfaceParentName(drawable);
                    Transform[] allChildren = GameFinder.GetAttachedObjectsObject(drawable)
                        .GetComponentsInChildren<Transform>();

                    foreach (Transform childTransform in allChildren)
                    {
                        GameObject child = childTransform.gameObject;

                        switch (child.tag)
                        {
                            case Tags.Line:
                                new DrawNetAction(drawable.name, drawableParent,
                                    LineConf.GetLine(child)).Execute(clients);
                                break;
                            case Tags.DText:
                                new WriteTextNetAction(drawable.name, drawableParent,
                                    TextConf.GetText(child)).Execute(clients);
                                break;
                            case Tags.Image:
                                new AddImageNetAction(drawable.name, drawableParent,
                                    ImageConf.GetImageConf(child)).Execute(clients);
                                break;
                            case Tags.MindMapNode:
                                new MindMapCreateNodeNetAction(drawable.name, drawableParent,
                                    MindMapNodeConf.GetNodeConf(child)).Execute(clients);
                                break;
                        }
                    }
                }
            }
        }
    }
}