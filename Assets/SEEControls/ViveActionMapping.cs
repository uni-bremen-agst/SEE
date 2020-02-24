using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls
{
    public class ViveActionMapping : ActionMapping
    {
        public enum Inputs { OnLeftTrigger, OnLeftTriggerAxis, OnRightTrigger, OnRightTriggerAxis};
        private Event[] Mapping;
        private int NumberOfInputs = 4;
        private string SetName;

        public ViveActionMapping(string name)
        {
            SetName = name;
            Mapping = new Event[NumberOfInputs];
        }

        public void SetAction(Inputs input, Event action)
        {
            Mapping[(int)input] = action;
        }

        public Event GetEvent(Inputs input)
        {
            return Mapping[(int)input];
        }

        public Event[] GetSet()
        {
            return Mapping;
        }
    }
}
