#if UNITY_EDITOR

using UnityEditor;

namespace SEEEditor
{
    public class ImportProcessor : AssetPostprocessor
    {
        // This function is called just before any Asset is imported.
        // This lets us control the import settings through code.
        //void OnPreprocessAsset()
        //{
        //    // assetPath is the relative path of the imported asset
        //    Debug.Log(assetPath + "\n");
        //}
    }
}

#endif
