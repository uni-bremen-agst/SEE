 
using UnityEngine;
using UnityEngine.UI;

public class HillPercent : MonoBehaviour
{
    [Range(0, 100)]
    public float no1= 1;
    public Transform a;
    public Text t1;

    void Update()
    {
        UpdatePercent(no1);
    }

    public void UpdatePercent(float n1)
    {
 
        float p1 = n1 / 100;
 

        a.localScale = new Vector3(1, p1,1);
 
        t1.text = Mathf.Round(p1 * 100).ToString();
   
    }
}
