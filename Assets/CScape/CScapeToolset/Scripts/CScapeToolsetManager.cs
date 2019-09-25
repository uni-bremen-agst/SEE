using System.Collections;
using System.Collections.Generic;
using System.Linq;
//using UnityEditor;
using UnityEngine;
using CSToolset;

namespace CSToolset
{
    [ExecuteInEditMode]
    public class CScapeToolsetManager : MonoBehaviour
    {

        public Camera rtCam;
        public Camera rtCam2;
        public static Transform[] tiles;
        public static Transform thisObject;
        public static int activeTile;
        public static Transform rtCamT;
        public static Transform rtCamT2;
        public static Transform rtCamT3;
        public static Transform rtCamT4;
        public static Transform rtCamT5;
        public bool bakeAll = false;
        public Baker bakerMask;
        public Baker bakerNormal;
        public Baker bakerDepth;
        public Baker bakerOcclusion;
        public int tileCount;
        public bool CScapeFormat = true;
        public Material maskMat;
        public GameObject tilesHolder;
        public int progressCounter;

        public void UpdateMe()
        {

            thisObject = tilesHolder.transform;
            FetchTiles();
            rtCamT = rtCam.transform;
            tileCount = tiles.Length;

            
            if (bakeAll)
            {
                Debug.Log("Baking Textures - this can take some time, please wait");
                for (int i = 0; i < tileCount; i++)
                {

#if UNITY_EDITOR
                    if (i < tileCount)
                    {

                        if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(
                                "Rendering Textures",
                                "Rendering Textures",
                                (float)(i ) / (float)tileCount) )
                        {
                            Debug.Log("Baking canceled by the user");
                            UnityEditor.EditorUtility.ClearProgressBar();
                            bakeAll = false;
                            return;

                        }

                        // else UnityEditor.EditorUtility.ClearProgressBar();
                    }
                    
#endif

                    activeTile = i;
                    rtCamT.position = tiles[activeTile].position;
                    rtCamT.Translate(new Vector3(0, 0, -10));
                    rtCam.Render();

                    bakerMask.fileName = "Assets/CScapeCDK/Editor/Resources/" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "/Mask/diff_" + (i + 1).ToString("000") + ".png";
                    bakerMask.bake = true;
                    bakerMask.Bake();


                    bakerNormal.fileName = "Assets/CScapeCDK/Editor/Resources/" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "/Normal/Normal_" + (i + 1).ToString("000") + ".png";
                    bakerNormal.bake = true;
                    bakerNormal.Bake();


                    bakerDepth.fileName = "Assets/CScapeCDK/Editor/Resources/" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "/Depth/depth_" + (i + 1).ToString("000") + ".png";
                    bakerDepth.bake = true;
                    bakerDepth.Bake();


                    bakerOcclusion.fileName = "Assets/CScapeCDK/Editor/Resources/" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "/AO/ao_" + (i + 1).ToString("000") + ".png";
                    bakerOcclusion.bake = true;
                    bakerOcclusion.Bake();

                    

                }


#if UNITY_EDITOR
                UnityEditor.EditorUtility.ClearProgressBar();
                UpdateAssets();
             ///   UnityEditor.AssetDatabase.Refresh();
#endif

                Debug.Log("Baking Done");
                bakeAll = false;
            }
            //NextTile();
        }

        private void UpdateAssets()
        {
#if UNITY_EDITOR
            for (int i = 0; i < tileCount; i++)
            {

                if (i < tileCount)
                {

                    if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(
                            "Updating Textures",
                            "Reading Texture files",
                            (float)(i) / (float)tileCount))
                    {
                        Debug.Log("Baking canceled by the user");
                        UnityEditor.EditorUtility.ClearProgressBar();
                        bakeAll = false;
                        return;

                    }

                    // else UnityEditor.EditorUtility.ClearProgressBar();
                }

                bakerMask.fileName = "Assets/CScapeCDK/Editor/Resources/" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "/Mask/diff_" + (i + 1).ToString("000") + ".png";
                bakerNormal.fileName = "Assets/CScapeCDK/Editor/Resources/" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "/Normal/Normal_" + (i + 1).ToString("000") + ".png";
                bakerDepth.fileName = "Assets/CScapeCDK/Editor/Resources/" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "/Depth/depth_" + (i + 1).ToString("000") + ".png";
                bakerOcclusion.fileName = "Assets/CScapeCDK/Editor/Resources/" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "/AO/ao_" + (i + 1).ToString("000") + ".png";

                UnityEditor.TextureImporter A = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(bakerNormal.fileName);
                A.isReadable = true;
                A.sRGBTexture = false;
                A.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
                UnityEditor.AssetDatabase.ImportAsset(bakerNormal.fileName, UnityEditor.ImportAssetOptions.ForceUpdate);

                UnityEditor.TextureImporter B = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(bakerDepth.fileName);
                B.isReadable = true;
                B.sRGBTexture = false;
                B.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
                UnityEditor.AssetDatabase.ImportAsset(bakerDepth.fileName, UnityEditor.ImportAssetOptions.ForceUpdate);

                UnityEditor.TextureImporter C = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(bakerMask.fileName);
                C.isReadable = true;
                C.sRGBTexture = false;
                C.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
                UnityEditor.AssetDatabase.ImportAsset(bakerMask.fileName, UnityEditor.ImportAssetOptions.ForceUpdate);

                UnityEditor.TextureImporter D = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(bakerOcclusion.fileName);
                D.isReadable = true;
                D.sRGBTexture = false;
                D.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
                UnityEditor.AssetDatabase.ImportAsset(bakerOcclusion.fileName, UnityEditor.ImportAssetOptions.ForceUpdate);
            }
            UnityEditor.EditorUtility.ClearProgressBar();
#endif
        }
        public static void PreviousTile()
        {
            {
                
                FetchTiles();
                if (activeTile > 0)
                {
                    activeTile = activeTile - 1;
                }

                else activeTile = tiles.Length - 1;

                //Debug.Log(activeTile);
                rtCamT.position = tiles[activeTile].position;
                rtCamT.Translate(new Vector3(0, 0, -10));
                Focus();
            }
        }

       public static void NextTile()
        {
            FetchTiles();
            if (activeTile < tiles.Length -1)
            {
                activeTile = activeTile + 1;
            }
            else activeTile = 0;

            //Debug.Log(activeTile);
            rtCamT.position = tiles[activeTile].position;
            rtCamT.Translate(new Vector3(0, 0, -10));
            Focus();
        }
        public static void FetchTiles()
        {
            
            System.Array.Resize(ref tiles, 0);

            foreach (Transform t in thisObject)
            {
                System.Array.Resize(ref tiles, tiles.Length + 1);
                tiles[tiles.Length - 1] = t;
            }

        }
        public static void Focus()
        {
#if UNITY_EDITOR
            UnityEditor.SceneView sv = UnityEditor.SceneView.sceneViews[0] as UnityEditor.SceneView;
            sv.in2DMode = true;
            sv.orthographic = true;
            UnityEditor.EditorGUIUtility.PingObject(CScapeToolsetManager.tiles[activeTile]);
            var currentlActive = UnityEditor.Selection.activeGameObject;
            UnityEditor.Selection.activeGameObject = tiles[activeTile].gameObject;
            UnityEditor.SceneView.lastActiveSceneView.FrameSelected();
            UnityEditor.Selection.activeGameObject = currentlActive;
#endif

        }
    }
}
