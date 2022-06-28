 
using UnityEngine;
using UnityEngine.UI;

public class ThreePercent : MonoBehaviour
{
    [Range(0,9999)]
    public float no1, no2, no3;

    public Image a, b, c;
    public Text t1, t2, t3;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePercent(no1,no2,no3);
    }
    public void UpdatePercent(float n1,float n2,float n3)
    {
        float sum = n1 + n2 + n3;
        float p1 = n1 / sum;
        float p2 = n2 / sum;
        float p3 = n3 / sum;

        a.fillAmount = p1;
        b.fillAmount = p2;
        b.transform.localEulerAngles = -new Vector3(0, 0, 360 * p1);

        c.fillAmount = p3;
        c.transform.localEulerAngles = -new Vector3(0, 0, 360 * p1) - new Vector3(0, 0, 360 * p2);


        t1.text = p1 * 100 + "%";
        t2.text = p2 * 100 + "%";
        t3.text = p3 * 100 + "%";
    }
}
