using System;
using System.Collections.Generic;
using UnityEngine;
//using UMA; 
//using UMA.PoseTools;

namespace SEE.Game.Avatars
{
    /// <summary>
    /// Original Version by Andrew Marunchak - https://forum.unity.com/threads/uma-unity-multipurpose-avatar-on-the-asset-store.219175/page-210
    /// </summary>
    internal class AvatarBlendshapeExpressions : MonoBehaviour
    {

        public SkinnedMeshRenderer targetSkinnedRenderer;
        public Mesh bakedMesh;
        //public UMAExpressionPlayer expressionPlayer;
        
        private void Start()
        {
            /*
            if (this.transform.parent.GetComponent<UMAExpressionPlayer>() == null)
            {
                Debug.Log("Please ensure an expression player has been added to the parent UMA GameObject - this script will now stop running");
                return;
            }
            else
            {
                expressionPlayer = this.transform.parent.GetComponent<UMAExpressionPlayer>();
            }
            */
            
            
            targetSkinnedRenderer = this.GetComponent<SkinnedMeshRenderer>(); 
            bakedMesh = new Mesh();
            Debug.Log("UMA skinned mesh found - now baking");
            targetSkinnedRenderer.BakeMesh(bakedMesh);

            Vector3[] junkData = new Vector3[bakedMesh.vertices.Length];
            
            // Setup fake blendshapes

            // Jaw Blendshapes
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Jaw_Left", 100, junkData, junkData, junkData); // Jaw_Left_Right - range (-1 - 0)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Jaw_Right", 100, junkData, junkData, junkData); // Jaw_Left_Right - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Jaw_Forward", 100, junkData, junkData, junkData); //  Jaw_Forward_Back - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Jaw_Open", 100, junkData, junkData, junkData); // Jaw_Open_Close - range (0 - 1) 

            // Mouth
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Ape_Shape", 100, junkData, junkData, junkData); // Ganzen Mund nach unten ziehen TODO

            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Upper_Left", 100, junkData, junkData, junkData); // Mouth Left_Right - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Upper_Right", 100, junkData, junkData, junkData); // Mouth Left_Right - range (-1 - 0)

            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Lower_Left", 100, junkData, junkData, junkData); // Mouth Left_Right - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Lower_Right", 100, junkData, junkData, junkData); // Mouth Left_Right - range (-1 - 0)

            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Upper_Overturn", 100, junkData, junkData, junkData); // TODO
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Lower_Overturn", 100, junkData, junkData, junkData); // TODO

            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Pout", 100, junkData, junkData, junkData); // Mouth Narrow_Pucker (but only (0-1) ??) TODO

            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Smile_Left", 100, junkData, junkData, junkData); // Left Mouth Smile_Frown - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Smile_Right", 100, junkData, junkData, junkData); // Right Mouth Smile_Frown - range (0 - 1)

            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Sad_Left", 100, junkData, junkData, junkData); // Left Mouth Smile_Frown - range (-1 - 0)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Sad_Right", 100, junkData, junkData, junkData); // Left Mouth Smile_Frown - range (-1 - 0)
            
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Upper_UpLeft", 100, junkData, junkData, junkData); // Left Upper Lip Up_Down - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Upper_UpRight", 100, junkData, junkData, junkData); // Right Upper Lip Up_Down - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Lower_DownLeft", 100, junkData, junkData, junkData); // Left Lower Lip Up_Down - range (-1 - 0)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Lower_DownRight", 100, junkData, junkData, junkData); // Right Lower Lip Up_Down - range (-1 - 0)

            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Upper_Inside", 100, junkData, junkData, junkData); // Left Upper Lip Up_Down && Right Upper Lip Up_Down - range (-1 - 0)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Lower_Inside", 100, junkData, junkData, junkData); // Left Lower Lip Up_Down && Right Lower Lip Up_Down - range (0 - 1)

            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Mouth_Lower_Overlay", 100, junkData, junkData, junkData); // Maybe Jaw Close with range - range (-1 - 0) TODO

            // Tongue
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_LongStep1", 100, junkData, junkData, junkData); // Zunge leicht anheben?? TODO
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_LongStep2", 100, junkData, junkData, junkData); // Tongue Out - range (0 - 1)

            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_Left", 100, junkData, junkData, junkData); // Tongue Left_Right - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_Right", 100, junkData, junkData, junkData); // Tongue Left_Right - range (-1 - 0)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_Up", 100, junkData, junkData, junkData); // Tongue Up_Down - range (-1 - 0)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_Down", 100, junkData, junkData, junkData); // Tongue Up_Down - range (1 - 0)

            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_Roll", 100, junkData, junkData, junkData); // Tongue Curl - range (0-1)
            
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_UpLeft_Morph", 100, junkData, junkData, junkData); // Zunge leicht raus oben links TODO
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_UpRight_Morph", 100, junkData, junkData, junkData); // Zunge leich raus oben rechts TODO

            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_DownLeft_Morph", 100, junkData, junkData, junkData); // Zunge leicht raus unten links TODO
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Tongue_DownRight_Morph", 100, junkData, junkData, junkData); // Zunge leicht raus unten rechts TODO

            // Cheeks
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Cheek_Puff_Left", 100, junkData, junkData, junkData); // Left Cheek Puff_Squint - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Cheek_Puff_Right", 100, junkData, junkData, junkData); // Right Cheek Puff_Squint - range (0 - 1)
            targetSkinnedRenderer.sharedMesh.AddBlendShapeFrame("Cheek_Suck", 100, junkData, junkData, junkData); // Maybe "Left Cheek Puff_Squint && Right Cheek Puff_Squint" - range (-1 - 0) TODO
            
        }

        private void Update()
        {
            throw new NotImplementedException();
        }
        
        
        public void ResetUMABlendShapes(int value)
        {
            int blendShapeCount = 36;
            int blendShapeValue = value;
            for (int blendShapeIndex = 0; blendShapeIndex < blendShapeCount; blendShapeIndex++)
            {
                targetSkinnedRenderer.SetBlendShapeWeight(blendShapeIndex, blendShapeValue);
            }
        }
        private float ValueConverter(float param)//Convert range(0 - 100) to range(-1 to +1)
        {
            param = param * 0.02f;
            param = param + -1;
            return param;
        }
        
        private float ValueConverter2(float param)//Convert range(0 - 100) to range(-1 to +1) [0 is eyes open, 100 is now eyes closed]
        {
            param = -param;
            param = param * 0.02f;
            param = param + 1f;
            return param;
        }
        
        private float ValueConverter3(float param)//Convert range(0 - 100) to range(-1 to +1) [0 to 100 left]
        {
            param = param * 0.01f;
            return param;
        }
        
        private float ValueConverter4(float param)//Convert range(0 - 100) to range(-1 to +1) [0 to 100 right]
        {
            param = -param;
            param = param * 0.01f;
            //param = param + 1f;
            return param;
        }
        
        
        
        public int BlendShapeByString(string arg)
        {
            int blendIndex;
            int blendShapeLength;
            blendShapeLength = targetSkinnedRenderer.sharedMesh.blendShapeCount;
            for (int i = 0; i < blendShapeLength; i++)
            {
                if(targetSkinnedRenderer.sharedMesh.GetBlendShapeName(i) == arg)
                {
                    return i;
                }
            }
            return -1;
        }
        
    }

    
    
    

}