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

        private static float BLINK_EFFECT_DELAY = 0.1f;

        private Coroutine blinkEffectCoroutine;

        CandidateRecommendation CandidateRecommendation
        {
            get 
            {
                if (candidateRecommendation == null)
                {
                    candidateRecommendation = new CandidateRecommendation();
                    AttractFunctionTypePropertyChanged();
                    CandidateTypePropertyChanged();
                    UseCDAPropertyChanged();
                    candidateRecommendation.Statistics.CsvPath = csvPath; 
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
        [OnValueChanged("AttractFunctionTypePropertyChanged")]
        public AttractFunctionType attractFunctionType = AttractFunctionType.CountAttract;

        private void AttractFunctionTypePropertyChanged()
        {
            CandidateRecommendation.AttractFunctionType = attractFunctionType;
        }

        [SerializeField]
        [OnValueChanged("CandidateTypePropertyChanged")]
        public string candidateType = "Class";

        private void CandidateTypePropertyChanged()
        {
            CandidateRecommendation.CandidateType = candidateType;
        }

        [SerializeField]
        public FilePath csvPath;

        [SerializeField]
        public FilePath xmlPath;

        public ReflexionGraph ReflexionGraph
        {
            set 
            {
                CandidateRecommendation.ReflexionGraph = value;
            }
        }

        public Graph OracleMapping
        {
            set
            {
                CandidateRecommendation.Statistics.SetOracleMapping(value);
            }
            get
            {
                return CandidateRecommendation.Statistics.OracleGraph;
            }
        }

        public void OnCompleted()
        {
            CandidateRecommendation.OnCompleted();
        }

        public void OnError(Exception error)
        {
            CandidateRecommendation.OnError(error);
        }

        public void OnNext(ChangeEvent value)
        {
            CandidateRecommendation.OnNext(value);

            List<NodeOperator> nodeOperators = new List<NodeOperator>();
            foreach (Node cluster in CandidateRecommendation.Recommendations.Keys)
            {
                NodeOperator nodeOperator;

                nodeOperator = cluster.GameObject().AddOrGetComponent<NodeOperator>();
                nodeOperators.Add(nodeOperator);

                foreach (MappingPair mappingPair in CandidateRecommendation.Recommendations[cluster])
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

        [Button(ButtonSizes.Small)]
        public void StartRecording()
        {
            CandidateRecommendation.Statistics.StartRecording();
        }

        [Button(ButtonSizes.Small)]
        public void StopRecording()
        {
            CandidateRecommendation.Statistics.StopRecording();
        }

        [Button(ButtonSizes.Small)]
        public void ProcessRecordedData()
        {
            CandidateRecommendation.Statistics.StopRecording();
            CandidateRecommendation.Statistics.ProcessMappingData(csvPath.Path, xmlPath.Path);
        }

        /// <summary>
        /// 
        /// </summary>
        [Button(ButtonSizes.Small)]
        public void AutomaticallyMapEntities()
        {
            // While next recommendation still exists
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
                StartCoroutine(MapRecommendation(chosenMappingPair));
            }
        }

        private IEnumerator MapRecommendation(MappingPair chosenMappingPair)
        {
            // TODO: Implement as Commando to visualize mapping/ Trigger Animation.
            CandidateRecommendation.ReflexionGraph.AddToMapping(chosenMappingPair.Candidate, chosenMappingPair.Cluster);
            yield return new WaitForSeconds(0.3f);
        }
    }
}
