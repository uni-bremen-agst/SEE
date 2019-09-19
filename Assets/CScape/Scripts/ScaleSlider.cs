// Name this script "ScaleSlider"
using UnityEngine;
[ExecuteInEditMode]
public class ScaleSlider : MonoBehaviour
{
    public float scale = 1;
    public void Update()
    {
        transform.localScale = new Vector3(scale, 1, 1);
    }
}