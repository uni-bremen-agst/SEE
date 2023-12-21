using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchVersionControlSystems
{
    public static IVersionControl CreateVersionControl(string system)
    {
        switch (system.ToLower())
        {
            case "git":
                return new VersionControlSystems.GitVersionControl();
            case "svn":
                return new VersionControlSystems.SvnVersionControl();
            // Add cases for other version control systems
            default:
                throw new ArgumentException("Unsupported version control system", nameof(system));
        }
    }
}
