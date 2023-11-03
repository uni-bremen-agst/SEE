using SEE.Game;
using UnityEngine;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable;
using SEE.Utils;

namespace Assets.SEE.Game.Drawable
{
    /// <summary>
    /// This class adds a drawable holder to a drawable. 
    /// This is needed to place the objects on the drawable without them being influenced by the scale of the drawable.
    /// </summary>
    public static class DrawableHolder
    {
        /// <summary>
        /// Const with low latin alphabet.
        /// </summary>
        private const string letters = "abcdefghijklmnopqrstuvwxyz";
        /// <summary>
        /// Const with the numbers.
        /// </summary>
        private const string numbers = "0123456789";
        /// <summary>
        /// Const with allowed special characters.
        /// </summary>
        private const string specialCharacters = "!?§$%&.,_-#+*@";
        /// <summary>
        /// String that contains the low and uppder letters, numbers and special characters.
        /// It will be needed for the calculation of a random string.
        /// </summary>
        private static readonly string characters = letters + letters.ToUpper() + numbers + specialCharacters;

        /// <summary>
        /// String that contains the low and uppder letters and numbers.
        /// It will be needed for the calculation of a random string for file creation.
        /// </summary>
        private static readonly string charactersWithoutSpecial = letters + letters.ToUpper() + numbers;

        /// <summary>
        /// Calculates a random string of given length.
        /// </summary>
        /// <param name="size">The length of the random string</param>
        /// <returns>The calculated random string of given length.</returns>
        public static string GetRandomString(int size)
        {
            string randomString = "";
            for (int i = 0; i < size; i++)
            {
                randomString += characters[Random.Range(0, characters.Length)];
            }
            return randomString;
        }

        /// <summary>
        /// Calculates a random string of given length for file creation.
        /// </summary>
        /// <param name="size">The length of the random string</param>
        /// <returns>The calculated random string of given length.</returns>
        public static string GetRandomStringForFile(int size)
        {
            string randomString = "";
            for (int i = 0; i < size; i++)
            {
                randomString += charactersWithoutSpecial[Random.Range(0, charactersWithoutSpecial.Length)];
            }
            return randomString;
        }

        /// <summary>
        /// Provides the drawable holder for a given drawable.
        /// </summary>
        /// <param name="drawable">The drawable that should get a drawable holder.</param>
        /// <param name="highestParent">Is the drawable holder</param>
        /// <param name="attachedObjects">Is the parent object of <see cref="DrawableType"/></param>
        public static void Setup(GameObject drawable, out GameObject highestParent, out GameObject attachedObjects)
        {
            if (GameFinder.hasAParent(drawable))
            {
                GameObject parent = GameFinder.GetHighestParent(drawable);
                if (!parent.name.StartsWith(ValueHolder.DrawableHolderPrefix))
                {
                    highestParent = new GameObject(ValueHolder.DrawableHolderPrefix + "-" + parent.name);//drawable.GetInstanceID());
                    highestParent.transform.position = parent.transform.position;
                    highestParent.transform.rotation = parent.transform.rotation;

                    attachedObjects = new GameObject(ValueHolder.AttachedObject);
                    attachedObjects.tag = Tags.AttachedObjects;
                    attachedObjects.transform.position = highestParent.transform.position;
                    attachedObjects.transform.rotation = highestParent.transform.rotation;
                    attachedObjects.transform.SetParent(highestParent.transform);
                    parent.transform.SetParent(highestParent.transform);

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
                    highestParent = parent;
                    attachedObjects = GameFinder.FindChildWithTag(highestParent, Tags.AttachedObjects);
                }
            }
            else
            {
                highestParent = new GameObject(ValueHolder.DrawableHolderPrefix + drawable.GetInstanceID());
                highestParent.transform.position = drawable.transform.position;
                highestParent.transform.rotation = drawable.transform.rotation;

                attachedObjects = new GameObject(ValueHolder.AttachedObject);
                attachedObjects.tag = Tags.AttachedObjects;
                attachedObjects.transform.position = highestParent.transform.position;
                attachedObjects.transform.rotation = highestParent.transform.rotation;
                attachedObjects.transform.SetParent(highestParent.transform);

                drawable.transform.SetParent(highestParent.transform);

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