using System;

public class UnknownAttribute : Exception
{
    public UnknownAttribute()
    {
    }

    public UnknownAttribute(string message)
        : base(message)
    {
    }

    public UnknownAttribute(string message, Exception inner)
        : base(message, inner)
    {
    }
}
