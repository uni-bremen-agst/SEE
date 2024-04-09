using Assets.SEE.Tools.ReflexionAnalysis;
using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.SEETests
{
    internal class TestDocument
    {
        /// <summary>
        /// Tests cosine similarity for two documents
        /// </summary>
        [Test]
        public void TestDocumentCosineSimilarity()
        {
            Document doc1 = new Document();
            Document doc2 = new Document();

            doc1.AddWord("word1");
            doc2.AddWord("word1");

            double cosineSimilarity = Document.CosineSimilarity(doc1, doc2);
            Assert.AreEqual(1, cosineSimilarity, 0.00000001);

            cosineSimilarity = Document.CosineSimilarity(doc2, doc1);
            Assert.AreEqual(1, cosineSimilarity, 0.00000001);

            doc2.RemoveWord("word1");
            doc2.AddWord("word2");

            cosineSimilarity = Document.CosineSimilarity(doc1, doc2);

            cosineSimilarity = Document.CosineSimilarity(doc1, doc2);
            Assert.AreEqual(0, cosineSimilarity, 0.00000001);

            cosineSimilarity = Document.CosineSimilarity(doc2, doc1);
            Assert.AreEqual(0, cosineSimilarity, 0.00000001);

            doc2.AddWord("word1");

            cosineSimilarity = Document.CosineSimilarity(doc1, doc2);
            Assert.AreEqual(0.707107, cosineSimilarity, 0.000001);

            cosineSimilarity = Document.CosineSimilarity(doc2, doc1);
            Assert.AreEqual(0.707107, cosineSimilarity, 0.000001);

            IEnumerable<string> words = new string[] { "word1", "word1", "word1", "word2", "word3" };
            Document doc3 = new Document();
            doc3.AddWords(words);

            doc2.AddWords(doc3);

            /**
             * Current State:
             * 
             * doc2[word1] = 4
             * doc2[word2] = 2
             * doc2[word3] = 1
             * 
             * doc3[word1] = 3
             * doc3[word2] = 1
             * doc3[word3] = 1
             * 
             * doc1[word1] = 1
             * doc1[word2] = 0
             * doc1[word3] = 0
             * 
             * expected cosine_distance(doc2, doc3) = 0.986928
             * 
             * expected cosine_distance(doc2, doc1) = 0.872872
            **/

            cosineSimilarity = Document.CosineSimilarity(doc2, doc3);
            Assert.AreEqual(0.986928, cosineSimilarity, 0.000001);

            cosineSimilarity = Document.CosineSimilarity(doc3, doc2);
            Assert.AreEqual(0.986928, cosineSimilarity, 0.000001);

            cosineSimilarity = Document.CosineSimilarity(doc2, doc1);
            Assert.AreEqual(0.872872, cosineSimilarity, 0.000001);

            cosineSimilarity = Document.CosineSimilarity(doc1, doc2);
            Assert.AreEqual(0.872872, cosineSimilarity, 0.000001);

            // return to previous state
            doc2.RemoveWords(doc3);

            cosineSimilarity = Document.CosineSimilarity(doc1, doc2);
            Assert.AreEqual(0.707107, cosineSimilarity, 0.000001);

            cosineSimilarity = Document.CosineSimilarity(doc2, doc1);
            Assert.AreEqual(0.707107, cosineSimilarity, 0.000001);
        }
    }
}
