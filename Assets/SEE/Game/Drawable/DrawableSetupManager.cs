using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// This class adds a parent object with an area for the <see cref="DrawableType"/> objects to a drawable surface.
    /// This is needed to place the objects on the surface without them being influenced
    /// by the scale of the surface.
    /// </summary>
    public static class DrawableSetupManager
    {
        /// <summary>
        /// Provides the drawable holder for a given drawable.
        /// </summary>
        /// <param name="surface">The drawable surface that should get a drawable holder.</param>
        /// <param name="highestParent">Is the drawable holder</param>
        /// <param name="attachedObjects">Is the parent object of <see cref="DrawableType"/></param>
        public static void Setup(GameObject surface, out GameObject highestParent, out GameObject attachedObjects)
        {
            if (GameFinder.HasParent(surface))
            {
                GameObject parent = GameFinder.GetHighestParent(surface);
                /// Block for drawable holder creation.
                if (!parent.name.StartsWith(ValueHolder.DrawableHolderPrefix))
                {
                    highestParent = new GameObject(ValueHolder.DrawableHolderPrefix + "-" + parent.name);
                    highestParent.transform.SetPositionAndRotation(parent.transform.position, parent.transform.rotation);
                    attachedObjects = new GameObject(ValueHolder.AttachedObject)
                    {
                        tag = Tags.AttachedObjects
                    };
                    attachedObjects.transform.SetPositionAndRotation(highestParent.transform.position, highestParent.transform.rotation);
                    attachedObjects.transform.SetParent(highestParent.transform);
                    parent.transform.SetParent(highestParent.transform);

                    /// Copies the order in layer component to the highest parent.
                    if (parent.GetComponentInChildren<OrderInLayerValueHolder>() != null)
                    {
                        OrderInLayerValueHolder highestHolder = highestParent.AddComponent<OrderInLayerValueHolder>();
                        OrderInLayerValueHolder oldHolder = parent.GetComponentInChildren<OrderInLayerValueHolder>();
                        highestHolder.OrderInLayer = oldHolder.OrderInLayer;
                        highestHolder.OriginPosition = oldHolder.OriginPosition;
                        Destroyer.Destroy(oldHolder);
                    }
                }
                else
                {
                    /// Block if the drawable holder already exists.
                    highestParent = parent;
                    attachedObjects = GameFinder.FindChildWithTag(highestParent, Tags.AttachedObjects);
                }
            }
            else
            {
                /// Block for the case where the drawable has no parent and is thus the highest instance.
                /// Does not occur so far.
                /// Both whiteboards and sticky notes have a parent object, as both offer borders for collision detection.
                /// It creates a new parent for the drawable as well as an attached objects object.
                highestParent = new(ValueHolder.DrawableHolderPrefix + surface.GetInstanceID());
                highestParent.transform.SetPositionAndRotation(surface.transform.position, surface.transform.rotation);
                attachedObjects = new GameObject(ValueHolder.AttachedObject)
                {
                    tag = Tags.AttachedObjects
                };
                attachedObjects.transform.SetPositionAndRotation(highestParent.transform.position, highestParent.transform.rotation);
                attachedObjects.transform.SetParent(highestParent.transform);

                surface.transform.SetParent(highestParent.transform);

                /// Copies the order in layer component to the highest parent.
                if (surface.GetComponentInChildren<OrderInLayerValueHolder>() != null)
                {
                    OrderInLayerValueHolder highestHolder = highestParent.AddComponent<OrderInLayerValueHolder>();
                    OrderInLayerValueHolder oldHolder = surface.GetComponentInChildren<OrderInLayerValueHolder>();
                    highestHolder.OrderInLayer = oldHolder.OrderInLayer;
                    highestHolder.OriginPosition = oldHolder.OriginPosition;
                    Destroyer.Destroy(oldHolder);
                }
            }
        }
    }
}