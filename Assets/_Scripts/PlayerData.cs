using UnityEngine;

namespace SEE
{

    public static class PlayerData
    {
        public static readonly float DEFAULT_TEXTURE_SCALE_X = 3.0f;
        public static readonly float DEFAULT_TEXTURE_SCALE_Y = 1.1f;
        public static readonly float DEFAULT_TEXTURE_OFFSET_X = -1.0f;
        public static readonly float DEFAULT_TEXTURE_OFFSET_Y = -0.19f;

        public static readonly Vector2 DEFAULT_TEXTURE_SCALE = new Vector2(DEFAULT_TEXTURE_SCALE_X, DEFAULT_TEXTURE_SCALE_Y);
        public static readonly Vector2 DEFAULT_TEXTURE_OFFSET = new Vector2(DEFAULT_TEXTURE_OFFSET_X, DEFAULT_TEXTURE_OFFSET_Y);

        private static GameObject playerHeadPrefab = LoadPlayerHeadPrefab();

        public static GameObject GetPlayerHeadPrefab()
        {
            return playerHeadPrefab;
        }

        public static GameObject LoadPlayerHeadPrefab()
        {
            GameObject prefab = (GameObject)Resources.Load("Prefabs/PlayerHead", typeof(GameObject));
            Material prefabMaterial = prefab.GetComponentInChildren<MeshRenderer>().sharedMaterial;
            prefabMaterial.mainTextureScale = DEFAULT_TEXTURE_SCALE;
            prefabMaterial.mainTextureOffset = DEFAULT_TEXTURE_OFFSET;
            Object.DontDestroyOnLoad(prefab);
            return prefab;
        }
    }

}
