using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion; // Need to include the RootMotion namespace as well because of the BipedReferences
using RootMotion.FinalIK;

/// <summary>
/// Adds VRIK to a UMA character in runtime.
/// </summary>
public class UMA_VRIK : MonoBehaviour
{
    private Animator animator;
    public VRIK ik { get; private set; }

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
    // Please note that VRIK will sample the pose of the character at initiation so at the time of calling this method,
    // the limbs of the character should be bent in their natural directions.

    void AddingFBBIK(GameObject go)
    {
        // setting the references
        ik = go.AddComponent<VRIK>(); // Adding the component

        ik.AutoDetectReferences();
    }
}