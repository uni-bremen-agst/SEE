using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CScape;
using System.Linq;

namespace CScape
{
    [ExecuteInEditMode]
    public class CSLightsControl : MonoBehaviour
    {

        // float lights;
        public GameObject sun;
        public AnimationCurve curveX;
        float curveVal;
        float sunVal;
        float sunOld;
        public bool manualControl;
        public float manualValue;
        public bool controlSun;
        public Color dayColor;
        public Color fogAmortizationColor;
        public Gradient dayColors;
        public Light sunLight;
        public float lightMultiplier = 1;
        public bool controlFog = true;
        public float fogDensity = 1;
        public float fogStart = 270;
        public float fogEnd = 2000;
        public ReflectionProbe[] rProbe;
        public GameObject streetLights;
        public bool reLighting;
        public Color reLightColor;
        public float reLightingDistance;
        public float lightsDistance = 0.4f;
        public float lightsContour = 1.67f;
       // public bool dynamicLightCulling = false;
        public Light[] cullLights;
        public Camera reLightingCam;
        public Vector2 size;
        public Texture ReLightingControlTex;


        // Use this for initialization


        void SetCurves(AnimationCurve xC)
        {
            curveX = xC;
        }

        void Awake()
        {
            GetSunLight();
           // cullLights = GetComponentsInChildren<Light>(true);

        }

        void Update()
        {
            if (sunLight == null) GetSunLight();
            if (!manualControl && sun != null)
            {
                curveVal = curveX.Evaluate(sun.transform.rotation.eulerAngles.x / 360f);
                //  lights = sun.transform.rotation.eulerAngles.x / 90;

                if (controlSun)
                {
                    if (sun.transform.rotation.eulerAngles.x != sunOld)
                    {

                        dayColor = dayColors.Evaluate(sun.transform.rotation.eulerAngles.x / 360f);
                        sunLight.color = dayColor * lightMultiplier;
                        if (controlFog)
                        {
                            RenderSettings.fogColor = dayColor * fogAmortizationColor;
                            RenderSettings.fogDensity = fogDensity;
                            RenderSettings.fogMode = FogMode.Linear;
                            RenderSettings.fogStartDistance = fogStart;
                            RenderSettings.fogEndDistance = fogEnd;
                            RenderSettings.sun = sunLight;
                        }

                        if (dayColor.r == 0)
                        {
                            sunLight.enabled = false;
                            foreach (Transform go in streetLights.transform.Cast<Transform>().Reverse())
                            {
                                go.gameObject.SetActive(true);
                            }
                        }
                        else
                        {
                            sunLight.enabled = true;

                            foreach (Transform go in streetLights.transform.Cast<Transform>().Reverse())
                            {
                                go.gameObject.SetActive(false);
                            }
                        }

                        sunOld = sun.transform.rotation.eulerAngles.x;

                        for (int i = 0; i < rProbe.Length; i++)
                        {
                            rProbe[i].RenderProbe();
                        }
                        DynamicGI.UpdateEnvironment();
                    }

                }
            }
            else curveVal = manualValue;

            Shader.SetGlobalFloat("_CSLights", curveVal);
            if (reLighting)
            {
                Shader.SetGlobalFloat("_CSReLight", curveVal);
                Shader.SetGlobalFloat("_CSReLightDistance", 1f / reLightingDistance);
                Shader.SetGlobalFloat("_LightsDistance", lightsDistance);
                Shader.SetGlobalColor("_reLightColor", reLightColor);
                Shader.SetGlobalFloat("_lightsContour", lightsContour);

            }
            else Shader.SetGlobalFloat("_CSReLight", 1f);

            Vector4 projection = new Vector4(reLightingCam.orthographicSize * 2, reLightingCam.orthographicSize * 2 , (reLightingCam.orthographicSize - reLightingCam.transform.position.x ) / reLightingCam.orthographicSize/2, (reLightingCam.orthographicSize - reLightingCam.transform.position.z) / reLightingCam.orthographicSize /2);
            //Debug.Log(reLightingCam.orthographicSize  - reLightingCam.transform.position.x);
            Shader.SetGlobalVector("_ReLightingProjection", projection);
            Shader.SetGlobalTexture("_ReLightingControlTex", ReLightingControlTex);

            // CullLights(1);
        }

        void GetSunLight()
        {
            if (sun == null)
                sun = GameObject.Find("Directional Light");
            if (sun == null)
            {
                Debug.Log("Please consider assigning a light to the Sun variable in the CS Lights Control script attached to your CScape City Manager!");
                return;
            }
            sunLight = sun.GetComponent<Light>();
        }

        void CullLights(float waitTime)
        {
            
            for (int i = 0; i < cullLights.Length; i++)
            {
                float distance = Vector3.Distance(cullLights[i].transform.position, Camera.main.transform.position);
                if (distance < 50)
                {
                    cullLights[i].gameObject.SetActive (true);
                }
                else cullLights[i].gameObject.SetActive(false);
            }
        }
    }
}
