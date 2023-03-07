using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Autohand{
public class PhysicsGadgetSlider : PhysicsGadgetConfigurableLimitReader{
    [Min(0.01f), Tooltip("The percentage (0-1) from the required value needed to call the event, if threshold is 0.1 OnMax will be called at 0.9, OnMin at -0.9, and OnMiddle at -0.1 or 0.1")]
    public float threshold = 0.05f;

    [Min(0)]
    public int stepCount = 0;
    public int startStep = 0;
    private int prevStepCount = -1;

    public UnityEvent OnMax;
    public UnityEvent OnMid;
    public UnityEvent OnMin;
    public StepEvent[] stepEvents;
        
    bool min = false;
    bool max = false;
    bool mid = true;

    private int currStep = -1;
    private int prevStep = -1;
    
    private float minimum;
    private float maximum;
    float[] stepMarkers;

    protected void FixedUpdate(){
            var value = GetValue();
        if(!max && mid && value+threshold >= 1) {
            Max();
        }

        if(!min && mid && value-threshold <= -1){
            Min();
        }
        
        if (value <= threshold && max && !mid) {
            Mid();
        }

        if (value >= -threshold && min && !mid) {
            Mid();
        }
    }

    protected override void Start() {
        base.Start();
        if(startStep <= 0) return;

        FindSteps();
        SetSpring(startStep - 1);
    }

    void Update() {
        AdjustStep();
    }
    
    void AdjustStep() {
        if(stepCount <= 0) return;
        
        FindSteps();
        SetSpring(FindCurrentStep());

    }

    bool FindSteps() {
        if(prevStepCount == stepCount) return false;

        prevStepCount = stepCount;

        stepMarkers = new float[stepCount];

        minimum = -(GetJoint().linearLimit.limit);
        maximum = (GetJoint().linearLimit.limit);
        
        float step = GetStep();

        for(int i = 0; i < stepCount; i++) {
            stepMarkers[i] = minimum + (i * step);
        }
        return true;
    }

    public void SetSpring(int step)
    {
        currStep = step;
        Vector3 limitAxis = new Vector3(joint.xMotion == ConfigurableJointMotion.Locked ? 0 : stepMarkers[step], 
                                        joint.yMotion == ConfigurableJointMotion.Locked ? 0 : stepMarkers[step], 
                                        joint.zMotion == ConfigurableJointMotion.Locked ? 0 : stepMarkers[step]);
        
        GetJoint().transform.localPosition = limitAxis;
        GetJoint().targetPosition = limitAxis;
    }

    public void SetSpring(float stepRotation)
    {
        Vector3 limitAxis = new Vector3(joint.xMotion == ConfigurableJointMotion.Locked ? 0 : stepRotation, 
                                        joint.yMotion == ConfigurableJointMotion.Locked ? 0 : stepRotation, 
                                        joint.zMotion == ConfigurableJointMotion.Locked ? 0 : stepRotation);
        
        GetJoint().targetPosition = limitAxis;
    }

    float FindCurrentStep() {
        float checkValue = GetValue() / GetScalar();
        for(int i = 0; i < stepCount; i++)
            if(checkValue >= GetMinimumStep(i) && checkValue <= GetMaximumStep(i)) {
                currStep = i;
                if(currStep != prevStep) {
                    Step();
                    prevStep = currStep;
                }

                return stepMarkers[i];
            }

        return 0;
    }

    float GetStep() => (Mathf.Abs(minimum) + Mathf.Abs(maximum)) / (stepCount - 1);
    float GetScalar() => (1 / Mathf.Abs(minimum));
    float GetMinimumStep(int index) => stepMarkers[index] - (GetStep() / 2);
    float GetMaximumStep(int index) => stepMarkers[index] + (GetStep() / 2);

    void Max() {
        mid = false;
        max = true;
        OnMax?.Invoke();
    }

    void Mid() {
        min = false;
        max = false;
        mid = true;
        OnMid?.Invoke();
    }

    void Min() {
        min = true;
        mid = false;
        OnMin?.Invoke();
    }
    void Step() {
        for(int i = 0; i < stepEvents.Length; i++) {
            if(stepEvents[i].step == currStep + 1) {
                stepEvents[i].OnStepEnter?.Invoke();
            }
            else if(stepEvents[i].step == prevStep + 1) {
                stepEvents[i].OnStepExit?.Invoke();
            }
        }
    }
}
}
