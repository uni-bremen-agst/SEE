using System;

namespace SEE.Utils.LiveDocumentation
{
    public class ClassNotFoundException : Exception
    {
        public ClassNotFoundException(string className, string fileName) : base($"Class with name {className} cant be found in file {fileName}")
        {
        }
    }
}