using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CScape;
[CreateAssetMenu(fileName = "CScape City Colors", menuName = "CScape City Colors")]

public class ColorCombinations : ScriptableObject
{
    public Vector4[] colorBorderArray = new Vector4[11];
    public Vector4[] buildingLightsColors = new Vector4[11];
    public float[] textureScales = new float[21];
    public Vector4[] concreteColors = new Vector4[11];
    public Vector4[] glassColors = new Vector4[11];
    public Vector4[] advertisingPairs = new Vector4[21];
    public Vector4[] buildingColorPairs = new Vector4[41];




}
