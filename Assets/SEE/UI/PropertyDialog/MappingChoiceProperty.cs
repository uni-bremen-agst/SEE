using Assets.SEE.Tools.ReflexionAnalysis.AttractFunctions;
using Michsky.UI.ModernUIPack;
using Newtonsoft.Json.Converters;
using OpenAI.Chat;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.UI.PopupMenu;
using SEE.UI.PropertyDialog;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.SEE.UI.PropertyDialog
{
    internal class MappingChoiceProperty : Property<bool>
    {
        public override bool Value { get; set; }

        public MappingPair mappingPair;

        private const string mappingChoicePrefab = "Prefabs/UI/MappingChoice";

        private GameObject inputGroup;

        private GameObject parent;

        private SwitchManager switchManager;

        public override void SetParent(GameObject parent)
        {
            UnityEngine.Debug.Log($"Setting parent of mapping choice property: name={parent.name}");
            if (inputGroup == null)
            {
                this.parent = parent;
            } 
            else
            {
                inputGroup.transform.SetParent(parent.transform);
            }
        }

        private void SetOnOff(bool toggled)
        {
            switchManager.isOn = toggled;
            this.OnCheck(toggled);
        }

        protected override void StartDesktop()
        {
            inputGroup = PrefabInstantiator.InstantiatePrefab(mappingChoicePrefab, instantiateInWorldSpace: false);

            if(parent != null) 
            {
                this.SetParent(parent);
            }

            ButtonManagerBasic buttonCandidate = inputGroup.transform.Find("ButtonCandidate").gameObject.MustGetComponent<ButtonManagerBasic>();
            ButtonManagerBasic buttonCluster = inputGroup.transform.Find("ButtonCluster").gameObject.MustGetComponent<ButtonManagerBasic>();
            buttonCandidate.buttonText = mappingPair.Candidate.ToShortString();
            buttonCluster.buttonText = mappingPair.Cluster.ToShortString();

            buttonCandidate.clickEvent.AddListener(() => OnClickNode(mappingPair.Candidate));
            buttonCluster.clickEvent.AddListener(() => OnClickNode(mappingPair.Cluster));

            switchManager = inputGroup.transform.Find("Switch/SwitchManager").gameObject.MustGetComponent<SwitchManager>();

            switchManager.OnEvents.AddListener(() => OnCheck(true));
            switchManager.OffEvents.AddListener(() => OnCheck(false));

            this.SetOnOff(true);

            void OnClickNode(Node node)
            {
                // TODO: check blink count
                node.Operator().Blink(10);
            }
        }

        private void OnCheck(bool isChecked)
        {
            if (isChecked)
            {
                IEnumerable<MappingChoiceProperty> properties = GetAllPropertiesOfParent();
                UntoggleConflictingChoices(this, properties);
            }

            this.Value = isChecked;
        }

        private IEnumerable<MappingChoiceProperty> GetAllPropertiesOfParent()
        {
            // TODO: does not work!
            return parent.GetComponents<MappingChoiceProperty>();
        }

        private void UntoggleConflictingChoices(MappingChoiceProperty toggledProperty, IEnumerable<MappingChoiceProperty> properties)
        {
            UnityEngine.Debug.Log($"Try to uncheck conflicting choices: toggledProperty:{toggledProperty.mappingPair.ToShortString()} number other properties:{properties.Count()}");
            foreach (MappingChoiceProperty property in properties)
            {
                if(property != toggledProperty && 
                   property.mappingPair.CandidateID.Equals(toggledProperty.mappingPair.CandidateID))
                {
                    property.SetOnOff(false);
                }
            }
        }
    }
}
