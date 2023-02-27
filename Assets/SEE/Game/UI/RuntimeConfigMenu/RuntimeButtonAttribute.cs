using System;

public class RuntimeButtonAttribute : Attribute
{
    public string name;
    public RuntimeButtonAttribute(string name)
    { 
        this.name = name;
    }
}
