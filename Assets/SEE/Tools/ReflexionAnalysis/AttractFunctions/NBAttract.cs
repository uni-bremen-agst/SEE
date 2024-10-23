using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Tools.ReflexionAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions
{
    /// <summary>
    /// This class implements the <see cref="AttractFunction"/> NBAttract.
    /// It does save the words used in nodes to the cluster, 
    /// they are mapped to. It does uses a multinomial naive bayes classifier 
    /// to calculate the attraction between a given candiate node and a cluster 
    /// based on the words used in the source file of the candidate node.
    /// 
    /// TODO: cite Olsson et. al
    /// 
    /// </summary>
    public class NBAttract : LanguageAttract
    {
        /// <summary>
        /// ITextClassifier object used for calculating the attract value
        /// </summary>
        private ITextClassifier naiveBayes;

        /// <summary>
        /// This bool determines if cda terms should be generated after a node was mapped.
        /// </summary>
        private bool useCDA;

        /// <summary>
        ///This bool determines if standard terms should be generated based on a node which was mapped.
        /// </summary>
        private bool useStandardTerms;

        /// <summary>
        /// TODO: cda not working properly at the moment
        /// </summary>
        private Dictionary<string, IDocument> cdaDocuments = new();

        /// <summary>
        /// This constructor initializes a new instance of <see cref="NBAttract"/>.
        /// </summary>
        /// <param name="reflexionGraph">Reflexion graph this attraction function is reading on.</param>
        /// <param name="candidateRecommendation">CandidateRecommendation object which uses and is used by the created attract function.</param>
        /// <param name="config">Configuration objects containing parameters to configure this attraction function</param>
        public NBAttract(ReflexionGraph reflexionGraph, 
               CandidateRecommendation candidateRecommendation, 
               NBAttractConfig config) : base(reflexionGraph, candidateRecommendation, config, useDocumentsAsSet: true)
        {
            // this.useCDA = config.UseCDA;
            this.useStandardTerms = config.UseStandardTerms;
            this.naiveBayes = new NaiveBayesIncremental(config.AlphaSmoothing);   
        }
        /// <summary>
        /// This method returns the current words contained in each class of the classifier
        /// as a formatted string, representing the data the attract function is currently holding. 
        /// 
        /// Can be used for debug and logging purposes.
        /// 
        /// </summary>
        /// <returns>a formatted string representing the words add to each class</returns>
        public override string DumpTrainingData()
        {
            if (naiveBayes == null) return string.Empty;
            StringBuilder sb = new StringBuilder();
            string indent = "\t";
            foreach (string clazz in naiveBayes.Classes)
            {
                sb.Append(clazz);
                sb.Append(" {");
                sb.Append(Environment.NewLine);

                Dictionary<string, int> wordFrequencies = naiveBayes.GetTrainingsData(clazz);
                foreach (string word in wordFrequencies.Keys)
                {
                    sb.Append($"{indent}{word}".PadRight(10));
                    sb.Append($": {wordFrequencies[word]}{Environment.NewLine}");
                }

                sb.Append("}");
                sb.Append(Environment.NewLine);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns true if the classifier contains no clusters or no words for all clusters.
        /// </summary>
        /// <returns></returns>
        public override bool EmptyTrainingData()
        {
            return naiveBayes.IsEmpty();
        }

        /// <summary>
        /// This method calculates the attract value for a given candidate node and a given cluster node.
        /// The words of the given candidate are wrapped in a document and the classifier will compare 
        /// the document to the document of the given cluster.
        /// 
        /// TODO: cda terms
        /// 
        /// </summary>
        /// <param name="candidate">given candidate</param>
        /// <param name="cluster">given cluster</param>
        /// <returns>attraction value</returns>
        public override double GetAttractionValue(Node candidate, Node cluster)
        {
            Document doc = new Document();
            doc.UseAsSet = this.useDocumentsAsSet;

            if (this.useStandardTerms)
            {
                this.AddStandardTerms(candidate, doc);
                this.AddWordsOfAscendants(candidate, doc);
            }

            // Dictionary<string, IDocument> docCdaTerms = new Dictionary<string, IDocument>();

            //if (this.useCDA)
            //{
            //    this.CreateCdaTerms(cluster, candidate, docCdaTerms);    

            //    if (docCdaTerms.Count > 0)
            //    {
            //        doc.AddWords(docCdaTerms[cluster.ID]); 
            //    }
            //}

            // TODO : check and formulate cda problems
            // add cda terms to naiveBayes classifier
            //foreach (string clusterID in docCdaTerms.Keys)
            //{

            //}

            double attraction = naiveBayes.ProbabilityForClass(cluster.ID, doc);

            // remove cda terms from naiveBayes classifier
            //foreach (string clusterID in docCdaTerms.Keys)
            //{

            //}

            return attraction;
        }

        /// <summary>
        /// This method is called if a node was add or removed from a given cluster. 
        /// This call updates the classifier with the words contained by the mapped node.
        /// The document of the node is add or removed from the classifier regarding the 
        /// change type.
        /// 
        /// if <see cref="useStandardTerms"/> is set to true, the standard words by
        /// defined in LanguageAttract are add.
        /// 
        /// if <see cref="useCDA"/> is set to true, cda terms are generated add to the 
        /// corresponding clusters. // TODO: Cda is currently not working properly
        /// 
        /// </summary>
        /// <param name="cluster">Cluster node from which the changedNode was add or removed.</param>
        /// <param name="changedNode">Candidate node which was add or removed from the cluster</param>
        /// <param name="changeType">given change type</param>
        public override void HandleChangedCandidate(Node cluster, Node nodeChangedInMapping, ChangeType changeType)
        {
            if (!this.HandlingRequired(nodeChangedInMapping.ID, changeType, updateHandling: true))
            {
                return;
            }

            Document docStandardTerms = new Document();

            if (this.useStandardTerms)
            {
                this.AddStandardTerms(nodeChangedInMapping, docStandardTerms);
            }

            // Dictionary<string, IDocument> docCdaTerms = new Dictionary<string, IDocument>();

            //if (this.useCDA)
            //{
            //    this.CreateCdaTerms(cluster, nodeChangedInMapping, docCdaTerms);
            //    UpdateCdaDocuments(docCdaTerms, changeType);
            //}

            if (changeType == ChangeType.Addition)
            {
                naiveBayes.AddDocument(cluster.ID, docStandardTerms);
            }
            else if(changeType == ChangeType.Removal) 
            {
                naiveBayes.RemoveDocument(cluster.ID, docStandardTerms);
            }

            this.AddAllClusterToUpdate();
            this.AddAllCandidatesToUpdate();
        }

        ///// <summary>
        ///// TODO: cda is currently not working properly 
        ///// </summary>
        ///// <param name="changedCdaTerms"></param>
        ///// <param name="changeType"></param>
        //private void UpdateCdaDocuments(Dictionary<string, IDocument> changedCdaTerms, ChangeType changeType)
        //{
        //    foreach (string clusterID in changedCdaTerms.Keys)
        //    {
        //        if (!this.cdaDocuments.TryAdd(clusterID, changedCdaTerms[clusterID]))
        //        {
        //            if (this.cdaDocuments[clusterID].WordCount > 0)
        //            {
        //                naiveBayes.RemoveDocument(clusterID, this.cdaDocuments[clusterID]);
        //            }
        //            if (changeType == ChangeType.Addition)
        //            {
        //                this.cdaDocuments[clusterID].AddWords(changedCdaTerms[clusterID]);
        //            }
        //            else if (changeType == ChangeType.Removal)
        //            {
        //                this.cdaDocuments[clusterID].RemoveWords(changedCdaTerms[clusterID]);
        //            }
        //            if (this.cdaDocuments[clusterID].WordCount > 0)
        //            {
        //                naiveBayes.AddDocument(clusterID, this.cdaDocuments[clusterID]);
        //            }
        //        }
        //        else if (changeType == ChangeType.Addition)
        //        {
        //            this.cdaDocuments[clusterID] = changedCdaTerms[clusterID];
        //            if (this.cdaDocuments[clusterID].WordCount > 0)
        //            {
        //                naiveBayes.AddDocument(clusterID, this.cdaDocuments[clusterID]);
        //            }
        //        }
        //    }
        //}

        /// <summary>
        /// Resets the attract function,
        /// by resetting the classifier and clearing the 
        /// document and edge state cache.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            this.naiveBayes.Reset();
            this.edgeStateCache.ClearCache();
            this.ClearDocumentCache();
        }

        /// <summary>
        /// Handles a cluster node which was add. 
        /// Forwards the call to the base class.
        /// </summary>
        /// <param name="cluster">given cluster node</param>
        public override void HandleAddCluster(Node cluster)
        {
            base.HandleAddCluster(cluster);
        }

        /// <summary>
        /// Handles a cluster node which was removed. 
        /// Forwards the call to the base class and 
        /// removes the given cluster from the classifier.
        /// </summary>
        /// <param name="cluster">given cluster node</param>
        public override void HandleRemovedCluster(Node cluster)
        {
            base.HandleRemovedCluster(cluster); 
            naiveBayes.DeleteClass(cluster.ID);
        }

        /// <summary>
        /// Handles an architecture edge which was add to the reflexion graph.
        /// Forwards the call to the base class.
        /// </summary>
        /// <param name="archEdge">given architecture edge</param>
        public override void HandleAddArchEdge(Edge archEdge)
        {
            base.HandleAddArchEdge(archEdge);
        }

        /// <summary>
        /// Handles an architecture edge which was removed from the reflexion graph.
        /// Forwards the call to the base class.
        /// </summary>
        /// <param name="archEdge">given architecture edge</param>
        public override void HandleRemovedArchEdge(Edge archEdge)
        {
            base.HandleRemovedArchEdge(archEdge);
        }

        /// <summary>
        /// This method does not need to be handled.
        /// </summary>
        /// <param name="edgeChange"></param>
        public override void HandleChangedState(EdgeChange edgeChange)
        {
            // No handling necessary
        }
    }
}
