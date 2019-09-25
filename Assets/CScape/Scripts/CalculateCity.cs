using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalculateCity : MonoBehaviour
{
    public Texture2D tex;
    public int texSize = 64;
    public MeshRenderer mRenderer;
    public Texture mTex;
    public int maxStartIterations = 100;


    public Color activePix;
    public Color activePixW;
    // Use this for initialization
    void Start()
    {
        tex = new Texture2D(texSize, texSize);
        mRenderer = gameObject.GetComponent<MeshRenderer>();
        mRenderer.material.mainTexture = tex;
        tex.filterMode = FilterMode.Point;
        for (int i = 0; i < maxStartIterations; i++)
        {
            int randomVectorX = Random.Range(0, texSize);
            int randomVectorY = Random.Range(0, texSize);
            tex.SetPixel(randomVectorX, randomVectorY, Color.red);
        }
        for (int i = 0; i < maxStartIterations; i++)
        {
            int randomVectorX = Random.Range(0, texSize);
            int randomVectorY = Random.Range(0, texSize);
            tex.SetPixel(randomVectorX, randomVectorY, Color.green);
        }

        tex.Apply();
    }

    // Update is called once per frame
    void Update()
    {



        for (int i = 0; i < texSize; i++)
        {

            for (int j = 0; j < texSize; j++)
            {
                activePixW = tex.GetPixel(i, j);

                if (activePixW.g == 1)
                {
                    tex.SetPixel(i + 1, j, Color.green);
                //    tex.SetPixel(i - 1, j, Color.green);
                }

                if (activePixW.r == 1)
                {
                    tex.SetPixel(i, j + 1, Color.red);
                  //  tex.SetPixel(i, j -1, Color.red);
                }
            }
        }


        tex.Apply();
    }
}
