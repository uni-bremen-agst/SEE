using System;
using System.Collections.Generic;
using UnityEngine;
using ViveSR.anipal.Lip;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// Changed version of <see cref="SRanipal_AvatarLipSample_v2"/>. Used to Initialize LipShape Tables.
    /// </summary>
    public class AvatarSRanipalLipV2 : MonoBehaviour
    {
        /// <summary>
        /// Stored LipShapeTables
        /// </summary>
        [SerializeField] private List<LipShapeTable_v2> lipShapeTables;

        /// <summary>
        /// Lip Weightings
        /// </summary>
        private Dictionary<LipShape_v2, float> lipWeightings;

        /// <summary>
        /// In order to use our fake blendshapes from <see cref="AvatarBlendshapeExpressions"/>, we need to create a
        /// lipshape for each blendshape. For this purpose we need a <see cref="LipShapeTable_v2"/>
        /// which also contains a <see cref="LipShapeTable_v2"/> to store all Lip Shapes.
        /// </summary>
        private void Start()
        {
            if (!SRanipal_Lip_Framework.Instance.EnableLip)
            {
                enabled = false;
                return;
            }

            // Create new LipShapeTable
            lipShapeTables = new List<LipShapeTable_v2>(new LipShapeTable_v2[1]);

            // Add empty LipShapeTable to list of LipShapeTables.
            lipShapeTables[0] = new LipShapeTable_v2();

            SkinnedMeshRenderer skinnedMeshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();

            if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh.blendShapeCount > 0)
            {
                lipShapeTables[0].skinnedMeshRenderer = skinnedMeshRenderer;
                int lipShapeTableSize = skinnedMeshRenderer.sharedMesh.blendShapeCount;

                lipShapeTables[0].lipShapes = new LipShape_v2[lipShapeTableSize];

                for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; ++i)
                {
                    string elementName = skinnedMeshRenderer.sharedMesh.GetBlendShapeName(i);

                    lipShapeTables[0].lipShapes[i] = LipShape_v2.None;
                    foreach (LipShape_v2 lipShape in (LipShape_v2[])Enum.GetValues(typeof(LipShape_v2)))
                    {
                        // Match blendshape name with lip shape name.
                        if (elementName == lipShape.ToString())
                        {
                            // Add correct lip shape to list of lip shapes.
                            lipShapeTables[0].lipShapes[i] = lipShape;
                        }
                    }
                }
            }
            SetLipShapeTables(lipShapeTables);
        }

        /// <summary>
        /// If the Lip Framework has the status WORKING, we will set the values of the Facial Tracker to
        /// the Blendshapes (<see cref="UpdateLipShapes"/>).
        /// </summary>
        private void Update()
        {
            if (SRanipal_Lip_Framework.Status != SRanipal_Lip_Framework.FrameworkStatus.WORKING)
            {
                return;
            }

            SRanipal_Lip_v2.GetLipWeightings(out lipWeightings);
            UpdateLipShapes(lipWeightings);
        }

        /// <summary>
        /// Checks and sets the LipShapeTables.
        /// </summary>
        public void SetLipShapeTables(List<LipShapeTable_v2> lipShapeTables)
        {
            bool valid = true;
            if (lipShapeTables == null)
            {
                valid = false;
            }
            else
            {
                for (int table = 0; table < lipShapeTables.Count; ++table)
                {
                    if (lipShapeTables[table].skinnedMeshRenderer == null)
                    {
                        valid = false;
                        break;
                    }
                    for (int shape = 0; shape < lipShapeTables[table].lipShapes.Length; ++shape)
                    {
                        LipShape_v2 lipShape = lipShapeTables[table].lipShapes[shape];
                        if (lipShape > LipShape_v2.Max || lipShape < 0)
                        {
                            valid = false;
                            break;
                        }
                    }
                }
            }
            if (valid)
            {
                this.lipShapeTables = lipShapeTables;
            }
        }

        /// <summary>
        /// Updates all Lip Shapes and sets the BlendShapeWeights (<see cref="RenderModelLipShape"/>) of each
        /// Blendshape to match the Lip Shape Values from the Face Tracker.
        /// </summary>
        public void UpdateLipShapes(Dictionary<LipShape_v2, float> lipWeightings)
        {
            foreach (LipShapeTable_v2 table in lipShapeTables)
            {
                RenderModelLipShape(table, lipWeightings);
            }
        }

        /// <summary>
        /// Sets Blendshape Weights.
        /// </summary>
        private static void RenderModelLipShape(LipShapeTable_v2 lipShapeTable, Dictionary<LipShape_v2, float> weighting)
        {
            for (int i = 0; i < lipShapeTable.lipShapes.Length; i++)
            {
                int targetIndex = (int)lipShapeTable.lipShapes[i];
                if (targetIndex > (int)LipShape_v2.Max || targetIndex < 0)
                {
                    continue;
                }
                lipShapeTable.skinnedMeshRenderer.SetBlendShapeWeight(i, weighting[(LipShape_v2)targetIndex] * 100);
            }
        }
    }
}