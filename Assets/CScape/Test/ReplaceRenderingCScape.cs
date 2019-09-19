using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteInEditMode]
public class ReplaceRenderingCScape : MonoBehaviour {
    public Shader shader;
    public string tag;
    public Camera cam;

    // Update is called once per frame
    private void Start()
    {
        cam.SetReplacementShader(shader, null);
    }
    void Update()
        {
            cam.SetReplacementShader(shader, null);
            cam.Render();
        }


}
