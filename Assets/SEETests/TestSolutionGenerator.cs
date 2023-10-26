using NUnit.Framework;
using CITools;

namespace SEETests
{ 
    internal class TestSolutionGenerator
    {
        [Test]
        public void Run()
        {
            SolutionGenerator.Sync();
        }
    }
}