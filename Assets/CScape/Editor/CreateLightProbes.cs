using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class CreateLightProbes : EditorWindow
{

    GameObject LightProbeGameObj;

    /// <summary>
    /// 
    /// </summary>
    public void UpdateLightprobes() {
    // Get _LightProbes game object
    LightProbeGameObj = GameObject.Find("CS_LightProbes");
        if(LightProbeGameObj == null) return;
 
        // Get light probe group component
        LightProbeGroup LPGroup = LightProbeGameObj.GetComponent("LightProbeGroup") as LightProbeGroup;
        if(LPGroup == null) return;
 
        // Create lightprobe positions
        //Vector3[] ProbePos = new Vector3[VertPositions.Count];
        //for(int i = 0; i<VertPositions.Count; i++){
        //    ProbePos[i] = VertPositions[i];
        //}

        // Set new light probes
       // LPGroup.probePositions = ProbePos;
    }
}