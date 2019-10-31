using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enables fast and simple checks for all types.
/// If a check fails an exception is thrown.
/// </summary>
public static class CCACheckExtension
{

    /// <summary>
    /// Checks if a given object is null and throws a System.Exception
    /// containing the paramName if so.
    /// </summary>
    /// <typeparam name="T">type of the given object</typeparam>
    /// <param name="obj">object to check</param>
    /// <param name="paramName">parameter name for exception message</param>
    /// <returns>the given object to check</returns>
    public static T AssertNotNull<T>(this T obj, string paramName)
    {
        if (obj == null)
        {
            throw new System.Exception(paramName + "must not be null");
        }
        return obj;
    }

    /// <summary>
    /// Checks if a given string is null and not empty and throws a System.Exception
    /// containing the paramName if so.
    /// </summary>
    /// <param name="obj">string to check</param>
    /// <param name="paramName">parameter name for exception message</param>
    /// <returns>the given string to check</returns>
    public static string AssertNotNullOrEmpty(this string obj, string paramName)
    {
        obj.AssertNotNull(paramName);
        if (obj.Length == 0)
        {
            throw new System.Exception(paramName + "must not be empty");
        }
        return obj;
    }
}
