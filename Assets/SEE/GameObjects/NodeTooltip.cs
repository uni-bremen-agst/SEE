using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SEE.GO
{
    public class NodeTooltip : MonoBehaviour
    {
        private TextMesh tooltipTextMesh;
        private Camera camera;

        [SerializeField]
        public float yOffset = 0.0f;

        [SerializeField]
        public bool FacingCamera = true;

        [SerializeField]
        public string FacingCameraName = "DesktopPlayer";


        public static NodeTooltip CurrentNodeTooltip { get; set; }

        // Start is called before the first frame update
        void Start()
        {
            camera = Camera.allCameras.Where(c => c.name == FacingCameraName).FirstOrDefault();
            CurrentNodeTooltip = this;
        }

        private void LateUpdate()
        {
            if (FacingCamera)
                transform.LookAt(transform.position + camera.transform.rotation * Vector3.forward, camera.transform.rotation * Vector3.up);
        }

        private void Awake()
        {
            tooltipTextMesh = transform.Find("Text").GetComponent<TextMesh>();
        }

        private void ShowToolTip(string text)
        {
            gameObject.SetActive(true);

            tooltipTextMesh.text = text;
        }

        private void ShowTooltipAtNewPosition(string text, Vector3 newPosition)
        {
            gameObject.SetActive(true);

            tooltipTextMesh.text = text;

            transform.position = new Vector3(newPosition.x, newPosition.y + yOffset, newPosition.z);
        }

        private void SetTooltipToInactive()
        {
            gameObject.SetActive(false);
        }

        public static void ShowTooltip(string text, Vector3 newPosition = default)
        {
            CurrentNodeTooltip.ShowTooltipAtNewPosition(!string.IsNullOrEmpty(text) ? text : "No Name found!", newPosition != default ? newPosition : CurrentNodeTooltip.transform.position);
        }

        public static void HideToolTip()
        {
            CurrentNodeTooltip.SetTooltipToInactive();
        }
    }
}

