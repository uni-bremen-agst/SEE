using UnityEngine;

namespace SEE
{

    public class MenuBackdropGenerator : MonoBehaviour
    {
        public RenderTexture renderTexture;

        void Start()
        {
            renderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.RGB111110Float);
            RenderTexture.active = renderTexture;

            Camera c = GetComponent<Camera>();
            c.targetTexture = renderTexture;
        }
    }

}// namespace SEE
