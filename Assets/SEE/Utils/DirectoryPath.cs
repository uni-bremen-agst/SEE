using System;

namespace SEE.Utils
{
    [Serializable]
    public class DirectoryPath : DataPath
    {
        public DirectoryPath() : base()
        {
        }

        public DirectoryPath(string path) : base(path)
        {
        }
    }
}
