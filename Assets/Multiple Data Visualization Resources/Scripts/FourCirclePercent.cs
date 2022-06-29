 


 
using UnityEngine;
using UnityEngine.UI;

public class FourCirclePercent : MonoBehaviour
{
    [Range(0, 9999)]
    public float no1, no2, no3,no4;

    public Image a, b, c,d;
    public Text t1, t2, t3,t4;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        UpdatePercent(no1, no2, no3,no4);
    }
    public void UpdatePercent(float n1, float n2, float n3, float n4)
    {
        float sum = n1 + n2 + n3+n4;
        float p1 = n1 / sum;
        float p2 = n2 / sum;
        float p3 = n3 / sum;
        float p4 = n4 / sum;

        a.fillAmount = p1;
        b.fillAmount = p2;
        b.transform.localEulerAngles = -new Vector3(0, 0, 360 * p1);

        c.fillAmount = p3;
        c.transform.localEulerAngles = -new Vector3(0, 0, 360 * p1) - new Vector3(0, 0, 360 * p2);


        d.fillAmount = p4;
        d.transform.localEulerAngles = -new Vector3(0, 0, 360 * p1) - new Vector3(0, 0, 360 * p2)- new Vector3(0, 0, 360 * p3);

        //a.transform.localScale = Vector3.one * 0.8f + Vector3.one * p1 * 0.5f;
        //b.transform.localScale = Vector3.one * 0.8f + Vector3.one * p2 * 0.5f;
        //c.transform.localScale = Vector3.one * 0.8f + Vector3.one * p3 * 0.5f;
        //d.transform.localScale = Vector3.one * 0.8f + Vector3.one * p4 * 0.5f;



        t1.text = p1 * 100 + "%";
        t2.text = p2 * 100 + "%";
        t3.text = p3 * 100 + "%";
        t4.text = p4 * 100 + "%";
    }
}
