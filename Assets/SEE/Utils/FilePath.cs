using System;

namespace SEE.Utils
{
    [Serializable]
    public class FilePath : DataPath
    {
        public FilePath() : base()
        {
        }

        public FilePath(string path) : base(path)
        {
        }
    }
}
