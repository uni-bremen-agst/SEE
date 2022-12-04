using SEE.Game.Avatars;
using UMA.PoseTools;
using UnityEngine;
using ViveSR.anipal.Lip;

/// <summary>
/// Searches for UMARenderer in parent gameObject and adds AvatarBlendshapeExpression and SRanipalLip Tracker script to it.
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
        Transform transform = gameObject.transform.Find("UMARenderer");
        if (transform != null)
        {
            transform.gameObject.AddComponent<AvatarBlendshapeExpressions>();
            transform.gameObject.AddComponent<AvatarSRanipalLipV2>();
            Destroy(this);
        }
    }
}
