using SEE.Game.Avatars;
using UnityEngine;

/// <summary>
/// Searches for UMARenderer in parent gameObject and adds AvatarBlendshapeExpression script to it.
/// </summary>
public class AvatarFindUmaRenderer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Transform transform = gameObject.transform.parent.Find("UMARenderer");
        if (transform != null)
        {
            transform.gameObject.AddComponent<AvatarBlendshapeExpressions>();
            Destroy(this);
        }
    }
}
