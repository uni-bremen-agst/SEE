using SEE.Game.Avatars;
using UMA.PoseTools;
using UnityEngine;
using ViveSR.anipal.Lip;

// FIXME: This class was merged from the Facial Tracker branch.
// Changes necessary to conform to our coding styles should be
// made there and then merged. I do not want to make the changes
// here because that may result in conflicts.

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
