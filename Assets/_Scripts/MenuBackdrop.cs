using UnityEngine;
using UnityEngine.UI;

namespace SEE
{

    public class MenuBackdrop : MonoBehaviour
    {
        void Start()
        {
            RawImage rawImage = GetComponent<RawImage>();
            foreach (Camera c in Camera.allCameras)
            {
                MenuBackdropGenerator mbg = c.GetComponent<MenuBackdropGenerator>();
                if (mbg != null)
                    rawImage.texture = mbg.renderTexture;
            }
        }
    }

}// namespace SEE
