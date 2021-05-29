using UnityEngine;

namespace SEE.Game
{
    public class MaterialChanger
    {
        public MaterialChanger(GameObject gameObject, Material localSpecialMaterial, Material remoteSpecialMaterial)
        {
            this.gameObject = gameObject;
            LocalSpecialMaterial = localSpecialMaterial;
            RemoteSpecialMaterial = remoteSpecialMaterial;
        }

        private readonly GameObject gameObject;

        public readonly Material LocalSpecialMaterial;
        public readonly Material RemoteSpecialMaterial;

        /// <summary>
        /// The material before the object was hovered so that it can be restored
        /// when the object is no longer hovered. While hovering, a highlighting
        /// material will be used.
        /// </summary>
        private Material oldMaterial;

        /// <summary>
        /// Assigns the special material to the gameObject and stores the original 
        /// material in oldMaterial.
        /// </summary>
        /// <param name="isInitiator">Whether this client should visually be the initiator of the
        /// special material selection.</param>
        public void UseSpecialMaterial(bool isInitiator)
        {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (!oldMaterial)
            {
                oldMaterial = renderer.sharedMaterial;
            }

            renderer.sharedMaterial = isInitiator ? LocalSpecialMaterial : RemoteSpecialMaterial;
            renderer.sharedMaterial.renderQueue = oldMaterial.renderQueue;
        }

        /// <summary>
        /// Resets the original material of gameObject using the material stored in oldMaterial.
        /// </summary>
        public void ResetMaterial()
        {
            if (oldMaterial)
            {
                gameObject.GetComponent<Renderer>().sharedMaterial = oldMaterial;
            }
            oldMaterial = null;
        }
    }

}