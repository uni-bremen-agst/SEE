using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The interface for the version control systems, which holds
/// the methods every version control system that we use must implement.
/// </summary>
public interface IVersionControl
    {
        string Show(string repositoryPath, string fileName, string oldCommitID, string newCommitID);
        string ShowName();
        // Add other version control operations as needed
    }

