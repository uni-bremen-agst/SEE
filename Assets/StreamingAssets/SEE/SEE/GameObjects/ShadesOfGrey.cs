using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// Provides materials with 30 different shades of grey. It is generally
    /// claimed that humans cannot distinguish more than this number. 
    /// These materials are intended to be shared.
    /// </summary>
    public class ShadesOfGrey
    {
        /// <summary>
        /// Yields a grey material whose grey is a linear interpolation of value
        /// w.r.t range [0, 1]. Value 0 corresponds to white and value 1 to black.
        /// 
        /// Precondition: 0 <= value <= 1, otherwise an exception is thrown.
        /// 
        /// Result must be used read only.
        /// </summary>
        /// <param name="value">value to be mapped onto a grey material</param>
        /// <returns>grey read-only material</returns>
        public static Material GetGreyMaterial(float value)
        {
            if (value < 0.0f || value > 1.0f)
            {
                throw new System.Exception("Expected value must be in the range [0, 1]. Actual value is " + value);
            }
            return materials[Mathf.RoundToInt(value * (materials.Length - 1))];
        }

        /// <summary>
        /// The grey materials we can return.
        /// </summary>
        private static readonly Material[] materials = CreateMaterials();

        /// <summary>
        /// Creates and returns 30 materials with different shades of grey
        /// as a linear interpolation between white and black.
        /// </summary>
        /// <returns></returns>
        private static Material[] CreateMaterials()
        {
            Material materialPrefab = Resources.Load<Material>(Materials.OpaqueMaterialName);
            Material[] materials = new Material[30];
            float value = 0.0f;
            float inc = 1.0f / materials.Length;

            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = new Material(materialPrefab);
                materials[i].color = Color.Lerp(Color.white, Color.black, value);
                value += inc;
            }
            return materials;
        }
    }
}
