using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion; // Need to include the RootMotion namespace as well because of the BipedReferences
using RootMotion.FinalIK;

/// <summary>
/// Adds FullBodyBipedIK to a UMA character in runtime.
/// </summary>
public class UMA_FBBIK : MonoBehaviour
{
    private Animator animator;
    public FullBodyBipedIK ik { get; private set; }

    void Update()
    {
        if (ik != null) return;

        // Add FBBIK as soon as an animator is present
        animator = GetComponent<Animator>();
        if (animator != null && animator.avatar != null)
        {
            AddingFBBIK(gameObject);
        }
    }

    // Call this method whenever you need in runtime.
    // Please note that FBBIK will sample the pose of the character at initiation so at the time of calling this method,
    // the limbs of the character should be bent in their natural directions.

    void AddingFBBIK(GameObject go)
    {
        var references = new BipedReferences();
        BipedReferences.AutoDetectReferences(ref references, go.transform, BipedReferences.AutoDetectParams.Default);
        
        // setting the references
        ik = go.AddComponent<FullBodyBipedIK>(); // Adding the component
        // Set the FBBIK to the references. You can leave the second parameter (root node) to null if you trust FBBIK to automatically set it to one of the bones in the spine.
        ik.SetReferences(references, null);
        // Using pre-defined limb orientations to safeguard from possible pose sampling problems (since 0.22)
        ik.solver.SetLimbOrientations(BipedLimbOrientations.UMA); // The limb orientations definition for UMA skeletons

    }
}