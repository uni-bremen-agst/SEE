using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The interface for the version control systems, which holds
/// the methods every version control system that we use must implement.
/// </summary>
public interface IVersionControl
    {
        /// <summary>
        /// Returns the source code from the revision being compared against.
        /// </summary>
        /// <returns>the source code from the revision being compared against.</returns>
        string Show(string repositoryPath, string fileName, string oldCommitID);

        /// <summary>
        /// Returns the source code from the revision, that gets compared.
        /// </summary>
        /// <returns>the source code from the revision, that gets compared.</returns>
        string ShowOriginal(string repositoryPath, string fileName, string oldCommitID, string newCommitID);

        /// <summary>
        /// Returns the updated filename.
        /// When a renaming took place, the new name gets returned.
        /// When a file got deleted, the original filename gets returned.
        /// Otherwise null gets returned.
        /// </summary>
        /// <returns>the updated filename</returns>
        string ShowName();
        // Add other version control operations as needed
    }

