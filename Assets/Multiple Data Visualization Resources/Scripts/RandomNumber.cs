using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomNumber : MonoBehaviour
{
    public Text numText;

    public float interval;
    public float initInterval;
    // Start is called before the first frame update
    void Start()
    {
        initInterval = Random.Range(0.2f, 0.5f);
        numText = GetComponent<Text>();
    }
    
    // Update is called once per frame
    void Update()
    {
        interval -= Time.deltaTime;
        if(interval<=0f)
        {
            numText.text = Random.Range(0, 10).ToString();
            interval = initInterval;
        }

    }
}
