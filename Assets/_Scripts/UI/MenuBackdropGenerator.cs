using UnityEngine;

namespace SEE
{

    public class MenuBackdropGenerator : MonoBehaviour
    {
        public RenderTexture Backdrop { get; private set; }

        public void Initialize()
        {
            Backdrop = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.RGB111110Float);
            RenderTexture.active = Backdrop;
            GetComponent<Camera>().targetTexture = Backdrop;
        }
    }

}
