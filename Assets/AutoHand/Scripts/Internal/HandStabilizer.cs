using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Autohand{
    //This script is used to hide rigidbody physics instabilitites by
    //putting the hand where it visually should be on prerender
    //and putting it where it physically should be on post render
    [DefaultExecutionOrder(-5)]
    public class HandStabilizer : MonoBehaviour{
        public HandBase hand = null;

        void Start(){
            if (!GetComponent<Camera>().enabled || hand == null)
                enabled = false;
        }

        void OnEnable(){
            if(GraphicsSettings.renderPipelineAsset != null){
                RenderPipelineManager.beginCameraRendering += OnPreRender;
                RenderPipelineManager.endCameraRendering += OnPostRender;
            }
        }

        void OnDisable(){
            if(GraphicsSettings.renderPipelineAsset != null){
                RenderPipelineManager.beginCameraRendering -= OnPreRender;
                RenderPipelineManager.endCameraRendering -= OnPostRender;
            }
        }

        private void Update() {
            if(hand == null)
                enabled = false;
        }

        private void OnPreRender() {
            if (hand.gameObject.activeInHierarchy)
                hand.OnPreRender();
        }

        private void OnPostRender() {
            if(hand.gameObject.activeInHierarchy)
                hand.OnPostRender();
        }



        private void OnPreRender(ScriptableRenderContext src, Camera cam) {
            if (hand.gameObject.activeInHierarchy)
                hand.OnPreRender();
        }

        private void OnPostRender(ScriptableRenderContext src, Camera cam) {
            if (hand.gameObject.activeInHierarchy)
                hand.OnPostRender();
        }
        
    }
}
