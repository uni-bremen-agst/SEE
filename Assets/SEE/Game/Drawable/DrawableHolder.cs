using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// This class adds a drawable holder to a drawable.
    /// This is needed to place the objects on the drawable without them being influenced
    /// by the scale of the drawable.
    /// </summary>
    public static class DrawableHolder
    {
        /// <summary>
        /// Provides the drawable holder for a given drawable.
        /// </summary>
        /// <param name="drawable">The drawable that should get a drawable holder.</param>
        /// <param name="highestParent">Is the drawable holder</param>
        /// <param name="attachedObjects">Is the parent object of <see cref="DrawableType"/></param>
        public static void Setup(GameObject drawable, out GameObject highestParent, out GameObject attachedObjects)
        {
            if (GameFinder.HasParent(drawable))
            {
                GameObject parent = GameFinder.GetHighestParent(drawable);
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
                        highestHolder.SetOrderInLayer(oldHolder.GetOrderInLayer());
                        highestHolder.SetOriginPosition(oldHolder.GetOriginPosition());
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
                highestParent = new(ValueHolder.DrawableHolderPrefix + drawable.GetInstanceID());
                highestParent.transform.SetPositionAndRotation(drawable.transform.position, drawable.transform.rotation);
                attachedObjects = new GameObject(ValueHolder.AttachedObject)
                {
                    tag = Tags.AttachedObjects
                };
                attachedObjects.transform.SetPositionAndRotation(highestParent.transform.position, highestParent.transform.rotation);
                attachedObjects.transform.SetParent(highestParent.transform);

                drawable.transform.SetParent(highestParent.transform);

                /// Copies the order in layer component to the highest parent.
                if (drawable.GetComponentInChildren<OrderInLayerValueHolder>() != null)
                {
                    OrderInLayerValueHolder highestHolder = highestParent.AddComponent<OrderInLayerValueHolder>();
                    OrderInLayerValueHolder oldHolder = drawable.GetComponentInChildren<OrderInLayerValueHolder>();
                    highestHolder.SetOrderInLayer(oldHolder.GetOrderInLayer());
                    highestHolder.SetOriginPosition(oldHolder.GetOriginPosition());
                    Destroyer.Destroy(oldHolder);
                }
            }
        }
    }
}