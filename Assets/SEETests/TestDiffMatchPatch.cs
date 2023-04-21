using DiffMatchPatch;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace SEETests
{
    class TestDiffMatchPatch
    {
        [Test]
        public void Simple()
        {
            diff_match_patch diff = new diff_match_patch();
            List<Diff> result = diff.diff_main("jumps over the lazy", "jumped over a lazy");
            foreach (Diff d in result)
            {
                Debug.Log(d);
            }
            Debug.Log(diff.diff_prettyHtml(result) + "\n");
        }
    }
}
