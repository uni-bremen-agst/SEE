using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game.Operator;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;
using SEE.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions.AttractFunction;
using static Assets.SEE.Tools.ReflexionAnalysis.CandidateRecommendation;

namespace Assets.SEE.Tools.ReflexionAnalysis
{
    public class CandidateRecommendationVisualization : MonoBehaviour, IObserver<ChangeEvent>
    {
        /// <summary>
        /// 
        /// </summary>
        private CandidateRecommendation candidateRecommendation;

        CandidateRecommendation CandidateRecommendation
        {
            get 
            {
                if (candidateRecommendation == null)
                {
                    candidateRecommendation = new CandidateRecommendation();
                }
                return candidateRecommendation; 
            }
        }

        [SerializeField]
        [OnValueChanged("UseCDAPropertyChanged")]
        public bool useCDA;

        private void UseCDAPropertyChanged()
        {
            CandidateRecommendation.UseCDA = useCDA;
        }

        [SerializeField]
        public bool RecordStatistic = true;

        [SerializeField]
        [OnValueChanged("AttractFunctionTypePropertyChanged")]
        public AttractFunctionType attractFunctionType = AttractFunctionType.CountAttract;

        private void AttractFunctionTypePropertyChanged()
        {
            CandidateRecommendation.AttractFunctionType = attractFunctionType;
        }

        [SerializeField]
        [OnValueChanged("AttractFunctionTypePropertyChanged")]
        public string targetType = "Class";

        private void TargetTypePropertyChanged()
        {
            CandidateRecommendation.TargetType = targetType;
        }

        [SerializeField]
        public FilePath csvPath;

        [SerializeField]
        public FilePath xmlPath;

        public ReflexionGraph ReflexionGraph
        {
            set 
            {
                if (CandidateRecommendation.ReflexionGraph == null)
                {
                    Debug.Log("created attract function.");
                    CandidateRecommendation.ReflexionGraph = value;
                    CandidateRecommendation.TargetType = targetType;
                    CandidateRecommendation.AttractFunctionType = attractFunctionType;
                    CandidateRecommendation.UseCDA = useCDA;
                }
            }
        }

        public Graph OracleMapping
        {
            set;
            get;
        }

        private static float BLINK_EFFECT_DELAY = 0.1f;

        private Coroutine blinkEffectCoroutine;

        public void OnCompleted()
        {
            candidateRecommendation.OnCompleted();
        }

        public void OnError(Exception error)
        {
            candidateRecommendation.OnError(error);
        }

        public void OnNext(ChangeEvent value)
        {
            candidateRecommendation.OnNext(value);

            List<NodeOperator> nodeOperators = new List<NodeOperator>();
            foreach (Node cluster in candidateRecommendation.Recommendations.Keys)
            {
                NodeOperator nodeOperator;

                nodeOperator = cluster.GameObject().AddOrGetComponent<NodeOperator>();
                nodeOperators.Add(nodeOperator);

                foreach (MappingPair mappingPair in candidateRecommendation.Recommendations[cluster])
                {
                    nodeOperators.Add(mappingPair.Candidate.GameObject().AddOrGetComponent<NodeOperator>());
                }
            }
            if (blinkEffectCoroutine != null) StopCoroutine(blinkEffectCoroutine);

            // TODO: Distinction between different hypothesized entities is required
            blinkEffectCoroutine = StartCoroutine(StartBlinkEffect(nodeOperators));
        }

        private IEnumerator StartBlinkEffect(List<NodeOperator> nodeOperators)
        {
            // Wait for the delay duration
            yield return new WaitForSeconds(BLINK_EFFECT_DELAY);

            // Start blink effect
            nodeOperators.ForEach((n) => n.Blink(10, 2));
        }

        /// <summary>
        /// 
        /// </summary>
        [Button(ButtonSizes.Small)]
        public void AutomaticallyMapEntities()
        {
            CandidateRecommendationStatistics statistics = null;
            if (RecordStatistic)
            {
                if(!File.Exists(csvPath.Path)) File.Create(csvPath.Path);
                // TODO: Refactor structural relationship of CandidateRecommendationStatistics,
                // CandidateRecommendation and CandidateRecommenVisualization to make it more testable
                statistics = new CandidateRecommendationStatistics(csvPath, 
                                                                   candidateRecommendation.ReflexionGraph, 
                                                                   OracleMapping,
                                                                   candidateRecommendation.TargetType);
                statistics.StartRecording();
            }

            // While recommendation exists
            while(CandidateRecommendation.Recommendations.Count != 0)
            {
                MappingPair chosenMappingPair;
                if(CandidateRecommendation.IsRecommendationDefinite())
                {
                    chosenMappingPair = CandidateRecommendation.GetDefiniteRecommendation();
                    Debug.Log($"Automatically map candidate {chosenMappingPair.Candidate.ID} to the cluster {chosenMappingPair.Cluster.ID}");
                } 
                else
                {
                    chosenMappingPair = CandidateRecommendation.Recommendations[CandidateRecommendation.Recommendations.Keys.First<Node>()].FirstOrDefault<MappingPair>();
                    // TODO: handle ambigous mapping steps
                    Debug.Log("Warning: Ambigous recommendation.");
                }
                StartCoroutine(MapRecommendation(chosenMappingPair, statistics));
            }

            if(RecordStatistic
               && OracleMapping != null)
            {
                statistics.Flush();
                statistics.ProcessMappingData(csvPath.Path, xmlPath.Path);
            }
        }

        private IEnumerator MapRecommendation(MappingPair chosenMappingPair, CandidateRecommendationStatistics statistics)
        {
            // TODO: Implement as Commando to visualize mapping/ Trigger Animation.
            statistics.RecordMappingPairs(CandidateRecommendation.MappingPairs);
            CandidateRecommendation.ReflexionGraph.AddToMapping(chosenMappingPair.Candidate, chosenMappingPair.Cluster);
            statistics.RecordChosenMappingPair(chosenMappingPair);
            yield return new WaitForSeconds(0.3f);
        }
    }
}
