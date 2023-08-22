using System;

namespace SEE.Utils.LiveDocumentation
{
    /// <summary>
    /// Exception which is called when a class can not be found in a specific file
    /// </summary>
    public class ClassNotFoundException : Exception
    {
        public ClassNotFoundException(string className, string fileName) : base($"Class with name {className} cant be found in file {fileName}")
        {
        }
    }
}
