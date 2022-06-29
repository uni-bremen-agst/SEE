 
using UnityEngine;
using UnityEngine.UI;

public class FourPercent : MonoBehaviour
{

    [Range(0, 100)]
    public float no1, no2, no3, no4=1;
    public RectTransform a, b, c,d;
    public Text t1, t2, t3,t4;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePercent(no1, no2, no3, no4);
    }

    public void UpdatePercent(float n1, float n2, float n3, float n4)
    {
        float sum = n1 + n2 + n3 + no4;
        float p1 = n1 / sum;
        float p2 = n2 / sum;
        float p3 = n3 / sum;
        float p4 = n4 / sum;

        a.sizeDelta = new Vector2(380,p1*400);
        b.sizeDelta = new Vector2(380,(p2+p1) * 400);
        c.sizeDelta = new Vector2(380, (p1+p2+p3) * 400);

 


        t1.text =Mathf.Round( p1 * 100) + "%";
        t2.text = Mathf.Round(p2 * 100) + "%";
        t3.text = Mathf.Round(p3 * 100) + "%";
        t4.text = Mathf.Round(p4 * 100) + "%";
    }
}
