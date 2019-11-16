using UnityEngine;

namespace SEE
{

    public class HeadGenerator : MonoBehaviour
    {
        public static readonly Vector3 HEAD_POSITION = new Vector3(0.0f, 2.0f, 0.0f);

        private Material headMaterial;
        private Material prefabHeadMaterial;

        void Start()
        {
            GameObject playerHead = GameObject.Instantiate(PlayerData.GetPlayerHeadPrefab());
            playerHead.transform.position = HEAD_POSITION;

            headMaterial = playerHead.GetComponentInChildren<MeshRenderer>().material;
            prefabHeadMaterial = PlayerData.GetPlayerHeadPrefab().GetComponentInChildren<MeshRenderer>().sharedMaterial;

            headMaterial.mainTextureScale = prefabHeadMaterial.mainTextureScale;
            headMaterial.mainTextureOffset = prefabHeadMaterial.mainTextureOffset;
        }

        public void SetTextureScaleX(float xScale)
        {
            Vector2 scale = new Vector2(xScale, headMaterial.mainTextureScale.y);
            headMaterial.mainTextureScale = scale;
            prefabHeadMaterial.mainTextureScale = scale;
        }

        public void SetTextureScaleY(float yScale)
        {
            Vector2 scale = new Vector2(headMaterial.mainTextureScale.x, yScale);
            headMaterial.mainTextureScale = scale;
            prefabHeadMaterial.mainTextureScale = scale;
        }

        public void SetTextureOffsetX(float xOffset)
        {
            Vector2 offset = new Vector2(xOffset, headMaterial.mainTextureOffset.y);
            headMaterial.mainTextureOffset = offset;
            prefabHeadMaterial.mainTextureOffset = offset;
        }

        public void SetTextureOffsetY(float yOffset)
        {
            Vector2 offset = new Vector2(headMaterial.mainTextureOffset.x, yOffset);
            headMaterial.mainTextureOffset = offset;
            prefabHeadMaterial.mainTextureOffset = offset;
        }

    }

}
