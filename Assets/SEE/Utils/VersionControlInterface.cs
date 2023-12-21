using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    public interface IVersionControl
    {
        string Show(string repositoryPath, string branchName, string fileName, string commitIdentifier);
        // Add other version control operations as needed
    }

