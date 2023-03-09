using System;

// This class is necessary to conserve the menu structure as it is in the Unity Editor for
// the build because all Odin Attributes use the Unity Editor which is not available in build
public class RuntimeButtonAttribute : Attribute
{
    // The name of the ButtonGroup
    public string Name;

    // The label for the Button
    public string Label;
    public RuntimeButtonAttribute(string Name, string Label)
    { 
        this.Name = Name;
        this.Label = Label;
    }
}
