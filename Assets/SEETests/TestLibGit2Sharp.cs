using UnityEngine;
using NUnit.Framework;
using LibGit2Sharp;
using System.IO;
using System.Text;
using SEE.Utils.Paths;
using DiffMatchPatch;

namespace SEETests
{
    /// <summary>
    /// Test cases (also used for exploration) for LibGit2Sharp.
    /// </summary>
    public class TestLibGit2Sharp
    {
        /// <summary>
        /// Shows the difference for all modified files of our SEE repository.
        /// </summary>
        [Test]
        public void TestCatFile()
        {
            Repository repo = new(DataPath.ProjectFolder());
            foreach (StatusEntry item in repo.RetrieveStatus())
            {
                if (item.State == FileStatus.ModifiedInWorkdir)
                {
                    Debug.Log(item.FilePath + " was modified.\n");
                    Blob blob = repo.Head.Tip[item.FilePath].Target as Blob;
                    string commitContent;
                    using (StreamReader content = new(blob.GetContentStream(), Encoding.UTF8))
                    {
                        commitContent = content.ReadToEnd();
                    }
                    string workingContent;
                    using (StreamReader content = new(repo.Info.WorkingDirectory + Path.DirectorySeparatorChar + item.FilePath, Encoding.UTF8))
                    {
                        workingContent = content.ReadToEnd();
                    }

                    diff_match_patch differ = new();
                    foreach (DiffMatchPatch.Diff diff in differ.diff_main(commitContent, workingContent))
                    {
                        if (diff.operation != Operation.EQUAL)
                        {
                            Debug.Log((diff.operation == Operation.INSERT ? "+ " : "- ") + diff.text + "\n");
                        }
                    }
                }
            }
        }
    }
}
