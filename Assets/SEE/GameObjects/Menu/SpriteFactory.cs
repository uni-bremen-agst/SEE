using UnityEngine;
using System;
using SEE.DataModel;

namespace SEE.GO.Menu
{
    /// <summary>
    /// A factory for creating sprites based on prefabs.
    /// </summary>
    public static class SpriteFactory
    {
        /// <summary>
        /// Returns a sprites as an instantiation of a prefab with given <paramref name="spriteFilename"/>
        /// with a CircleCollider2D. The sprite will be tagged by <see cref="Tags.UI"/> and added
        /// to the layer UI.
        /// 
        /// Precondition: <paramref name="spriteFilename"/> exists, otherwise an exception will
        /// be thrown.
        /// </summary>
        /// <param name="spriteFilename">filename of the sprite prefab to be instantiated</param>
        /// <param name="radius">the radius of the circular sprite requested</param>
        /// <param name="depth">the depth (z axis) of the circular sprite requested</param>
        /// <param name="color">the color of the circular sprite requested</param>
        /// <returns></returns>
        public static GameObject NewCircularSprite(string spriteFilename, float radius, float depth, Color color)
        {
            UnityEngine.Object prefab = IconFactory.LoadSprite(spriteFilename);
            if (prefab == null)
            {
                throw new Exception("Prefab" + spriteFilename + " not found.");
            }
            else
            {
                GameObject result = UnityEngine.Object.Instantiate(prefab) as GameObject;
                result.tag = Tags.UI;
                // add the object to the UI layer so that it does not collide with other game objects
                result.layer = LayerMask.NameToLayer("UI");
                if (result.TryGetComponent<SpriteRenderer>(out SpriteRenderer renderer))
                {
                    renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    renderer.color = color;
                }
                result.transform.localScale = new Vector3(2 * radius, 2 * radius, depth);
                result.AddComponent<CircleCollider2D>();
                return result;
            }
        }
    }
}