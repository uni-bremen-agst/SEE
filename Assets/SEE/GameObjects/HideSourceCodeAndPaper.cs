using UnityEngine;

namespace SEE.GO
{
    [ExecuteInEditMode]
    public class HideSourceCodeAndPaper : MonoBehaviour
    {
        private void Start()
        {
            GameObject textObject = transform.Find("SourceCode").gameObject;
            GameObject paperObject = transform.Find("Paper").gameObject;

            if ((textObject != null) && (paperObject != null))
            {
                // Debug.Log("Hide SourceCode.");
                //textObject.hideFlags = HideFlags.HideInHierarchy;
                //paperObject.hideFlags = HideFlags.HideInHierarchy;
            }
            else
            {
                Debug.Log("No SourceCode or Paper object found.");
            }
        }
    }
}
