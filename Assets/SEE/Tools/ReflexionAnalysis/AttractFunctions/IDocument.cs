using System.Collections.Generic;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    public interface IDocument : IEnumerable<string>
    {
        public IEnumerable<string> GetContainedWords();

        public int WordCount { get; }

        public IDocument Clone();

        public Dictionary<string, int> GetWordFrequencies();

        public void AddWords(Document document);

        public void RemoveWords(Document document);

        public void AddWords(IEnumerable<string> words);

        public void RemoveWords(IEnumerable<string> words);

        public void AddWord(string word);

        public void AddWord(string word, int count);

        public void RemoveWord(string word);

        public void RemoveWord(string word, int count);

        public int GetFrequency(string word);

        public int GetTotalWordFrequencies();

        public string ToString();

        //public Document MergeDocuments(Document doc1, Document doc2, DocumentMergingType type = DocumentMergingType.Union);

        //public double CosineSimilarityByFrequency(Document doc1, Document doc2);

        //public double DotProduct(Document doc1, Document doc2);

        //public double[] ToFrequencyArray();

        //public int[] ToOccurenceArray();
    }
}
