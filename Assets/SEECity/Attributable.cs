using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implements IAttributable providing named toggle, int, float, and string attributes.
/// </summary>
public abstract class Attributable : IAttributable
{
    private HashSet<string> toggleAttributes = new HashSet<string>();

    void IAttributable.SetToggle(string attributeName)
    {
        toggleAttributes.Add(attributeName);
    }

    bool IAttributable.HasToggle(string attributeName)
    {
        return toggleAttributes.Contains(attributeName);
    }

    private Dictionary<string, string> stringAttributes = new Dictionary<string, string>();

    public void SetString(string attributeName, string value)
    {
        stringAttributes[attributeName] = value;
    }

    bool IAttributable.TryGetString(string attributeName, out string value)
    {
        return stringAttributes.TryGetValue(attributeName, out value);
    }

    public string GetString(string attributeName)
    {
        if (stringAttributes.TryGetValue(attributeName, out string value))
        {
            return value;
        }
        else
        {
            throw new UnknownAttribute(attributeName);
        }
    }

    private Dictionary<string, float> floatAttributes = new Dictionary<string, float>();

    void IAttributable.SetFloat(string attributeName, float value)
    {
        floatAttributes[attributeName] = value;
    }

    float IAttributable.GetFloat(string attributeName)
    {
        if (floatAttributes.TryGetValue(attributeName, out float value))
        {
            return value;
        }
        else
        {
            throw new UnknownAttribute(attributeName);
        }
    }

    bool IAttributable.TryGetFloat(string attributeName, out float value)
    {
        return floatAttributes.TryGetValue(attributeName, out value);
    }

    private Dictionary<string, int> intAttributes = new Dictionary<string, int>();

    void IAttributable.SetInt(string attributeName, int value)
    {
        intAttributes[attributeName] = value;
    }

    int IAttributable.GetInt(string attributeName)
    {
        if (intAttributes.TryGetValue(attributeName, out int value))
        {
            return value;
        }
        else
        {
            throw new UnknownAttribute(attributeName);
        }
    }

    bool IAttributable.TryGetInt(string attributeName, out int value)
    {
        return intAttributes.TryGetValue(attributeName, out value);
    }

    bool IAttributable.TryGetNumeric(string attributeName, out float value)
    {

        if (intAttributes.TryGetValue(attributeName, out int intValue))
        {
            value = intValue;
            return true;
        }
        else
        {
            return floatAttributes.TryGetValue(attributeName, out value);
        }
    }

    public override string ToString()
    {
        string result = "";

        foreach (var attr in toggleAttributes)
        {
            result += " \"" + attr + "\": true,\n";
        }

        foreach (var attr in stringAttributes)
        {
            result += " \"" + attr.Key + "\": \"" + attr.Value + "\",\n";
        }

        foreach (var attr in intAttributes)
        {
            result += " \"" + attr.Key + "\": " + attr.Value + ",\n";
        }

        foreach (var attr in floatAttributes)
        {
            result += " \"" + attr.Key + "\": " + attr.Value + ",\n";
        }
        return result;
    }
}

