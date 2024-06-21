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
        public static void Synchronize()
        {
            new SynchronizeCurrentOrderInLayer(ValueHolder.CurrentOrderInLayer).Execute();

            ArrayList surfaces = new(GameObject.FindGameObjectsWithTag(Tags.Drawable));
            foreach (GameObject surface in surfaces)
            {
                if (GameFinder.GetDrawableSurfaceParentName(surface).Contains(ValueHolder.StickyNotePrefix))
                {
                    new StickyNoteSpawnNetAction(DrawableConfigManager.GetDrawableConfig(surface)).Execute();
                }

                if (GameFinder.GetAttachedObjectsObject(surface) != null)
                {
                    string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);
                    Transform[] allChildren = GameFinder.GetAttachedObjectsObject(surface)
                        .GetComponentsInChildren<Transform>();

                    foreach (Transform childTransform in allChildren)
                    {
                        GameObject child = childTransform.gameObject;

                        switch (child.tag)
                        {
                            case Tags.Line:
                                new DrawNetAction(surface.name, surfaceParentName,
                                    LineConf.GetLine(child)).Execute();
                                break;
                            case Tags.DText:
                                new WriteTextNetAction(surface.name, surfaceParentName,
                                    TextConf.GetText(child)).Execute();
                                break;
                            case Tags.Image:
                                new AddImageNetAction(surface.name, surfaceParentName,
                                    ImageConf.GetImageConf(child)).Execute();
                                break;
                            case Tags.MindMapNode:
                                new MindMapCreateNodeNetAction(surface.name, surfaceParentName,
                                    MindMapNodeConf.GetNodeConf(child)).Execute();
                                break;
                        }
                    }
                }
            }
        }
    }
}