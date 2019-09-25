using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CScape;

[ExecuteInEditMode]
public class CityColors : MonoBehaviour {
    public Vector4[] colorBorderArray = new Vector4[11];
    public Vector4[] buildingLightsColors;
    public float[] textureScales;
    public Vector4[] concreteColors;
    public Vector4[] glassColors;
    public Vector4[] advertisingPairs;
    public Vector4[] buildingColorPairs = new Vector4[41];
    public ColorCombinations cc;
    public bool controlFromTemplate = true;
    //public Vector4[] tempBorderArray = new Vector4[10];

    // Use this for initialization
    void Awake()
    {
        if (controlFromTemplate && cc != null)
        {
            colorBorderArray = cc.colorBorderArray;
            buildingLightsColors = cc.buildingLightsColors;
            textureScales = cc.textureScales;
            concreteColors = cc.concreteColors;
            glassColors = cc.glassColors;
            advertisingPairs = cc.advertisingPairs;
            buildingColorPairs = cc.buildingColorPairs;
        }
        else
        {
            // tempBorderArray = colorBorderArray;
            Shader.SetGlobalVectorArray("_borderArray", colorBorderArray);
            Shader.SetGlobalVectorArray("_buildingLightsColors", buildingLightsColors);
            Shader.SetGlobalFloatArray("_textureScales", textureScales);
            Shader.SetGlobalVectorArray("_concreteColors", concreteColors);
            Shader.SetGlobalVectorArray("_glassColors", glassColors);
            Shader.SetGlobalVectorArray("_shopColors", advertisingPairs);
            Shader.SetGlobalVectorArray("_faccadeColors", buildingColorPairs);
        }
    }

    void Update()
    {
        if (controlFromTemplate && cc != null)
        {
            colorBorderArray = cc.colorBorderArray;
            buildingLightsColors = cc.buildingLightsColors;
            textureScales = cc.textureScales;
            concreteColors = cc.concreteColors;
            glassColors = cc.glassColors;
            advertisingPairs = cc.advertisingPairs;
            buildingColorPairs = cc.buildingColorPairs;

            Shader.SetGlobalVectorArray("_borderArray", colorBorderArray);
            Shader.SetGlobalVectorArray("_buildingLightsColors", buildingLightsColors);
            Shader.SetGlobalFloatArray("_textureScales", textureScales);
            Shader.SetGlobalVectorArray("_concreteColors", concreteColors);
            Shader.SetGlobalVectorArray("_glassColors", glassColors);
            Shader.SetGlobalVectorArray("_shopColors", advertisingPairs);
            Shader.SetGlobalVectorArray("_faccadeColors", buildingColorPairs);
        }
        else
        {
            // tempBorderArray = colorBorderArray;
            Shader.SetGlobalVectorArray("_borderArray", colorBorderArray);
            Shader.SetGlobalVectorArray("_buildingLightsColors", buildingLightsColors);
            Shader.SetGlobalFloatArray("_textureScales", textureScales);
            Shader.SetGlobalVectorArray("_concreteColors", concreteColors);
            Shader.SetGlobalVectorArray("_glassColors", glassColors);
            Shader.SetGlobalVectorArray("_shopColors", advertisingPairs);
            Shader.SetGlobalVectorArray("_faccadeColors", buildingColorPairs);
        }

    }

    // Update is called once per frame
    public void UpdateColors()
    {
        
        Shader.SetGlobalVectorArray("_borderArray", colorBorderArray);
        Shader.SetGlobalVectorArray("_buildingLightsColors", buildingLightsColors);
        Shader.SetGlobalFloatArray("_textureScales", textureScales);
        Shader.SetGlobalVectorArray("_concreteColors", concreteColors);
        Shader.SetGlobalVectorArray("_glassColors", glassColors);
        Shader.SetGlobalVectorArray("_shopColors", advertisingPairs);
        Shader.SetGlobalVectorArray("_faccadeColors", buildingColorPairs);

       
            cc.colorBorderArray = colorBorderArray;
            cc.buildingLightsColors = buildingLightsColors;
            cc.textureScales = textureScales;
            cc.concreteColors = concreteColors;
            cc.glassColors = glassColors;
            cc.advertisingPairs = advertisingPairs;
            cc.buildingColorPairs = buildingColorPairs;


        
    }
}
