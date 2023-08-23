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
using UnityEngine;
using static Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions.AttractFunction;

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
        public FilePath OracleMapping;

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

                foreach (Node entity in candidateRecommendation.Recommendations[cluster])
                {
                    nodeOperators.Add(entity.GameObject().AddOrGetComponent<NodeOperator>());
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
        /// Saves the settings of this code city to <see cref="ConfigurationPath"/>.
        /// </summary>
        [Button(ButtonSizes.Small)]
        public void AutomaticallyMapEntities()
        {
            // TODO: iterate recommendations properly
            // for... StartCoroutine(MapRecommendation(entity, cluster));
        }

        private IEnumerator MapRecommendation(Node entity, Node cluster)
        {
            // TODO: Implement as Commando?
            CandidateRecommendation.ReflexionGraph.AddToMapping(entity, cluster);
            yield return new WaitForSeconds(0.3f);
        }
    }
}
