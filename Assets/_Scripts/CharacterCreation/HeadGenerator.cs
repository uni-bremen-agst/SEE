using UnityEngine;

namespace SEE
{

    public class HeadGenerator : MonoBehaviour
    {
        void Start()
        {
            PlayerData.playerHead.transform.position = new Vector3(0.0f, 2.0f, 0.0f);
        }

        public void SetTextureScaleX(float xScale)
        {
            PlayerData.playerHead.GetComponentInChildren<Head>().SetTextureScaleX(xScale);
        }

        public void SetTextureScaleY(float yScale)
        {
            PlayerData.playerHead.GetComponentInChildren<Head>().SetTextureScaleY(yScale);
        }

        public void SetTextureOffsetX(float xOffset)
        {
            PlayerData.playerHead.GetComponentInChildren<Head>().SetTextureOffsetX(xOffset);
        }

        public void SetTextureOffsetY(float yOffset)
        {
            PlayerData.playerHead.GetComponentInChildren<Head>().SetTextureOffsetY(yOffset);
        }

    }

}
