using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShaderDetails : MonoBehaviour {

    public bool useParralaxMapping = true;

	// Use this for initialization
	void Start () {
        if (useParralaxMapping == true)
            Shader.DisableKeyword("_CSCAPE_DESKTOP_ON");
        else Shader.EnableKeyword("_CSCAPE_DESKTOP_ON");
    }
	
	// Update is called once per frame

}
