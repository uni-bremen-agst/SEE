using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand
{
    public enum AnimationPoint
    {
        start,
        end
    }

    public class AutoAnimation : MonoBehaviour
    {
        [Tooltip("This each transform that will be animated")]
        public AnimationCurve animationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public bool recordChildTransforms = true;
        public bool setPositions = true;
        public bool setRotations = true;
        public bool setScale = false;

        [SerializeField,HideInInspector]
        private Transform[] transformReferences;
        [SerializeField, HideInInspector]
        private Vector3[] maxValuePositions;
        [SerializeField, HideInInspector]
        private Quaternion[] maxValueRotations;
        [SerializeField, HideInInspector]
        private Vector3[] maxValueScales;

        [SerializeField, HideInInspector]
        private Vector3[] minValuePositions;
        [SerializeField, HideInInspector]
        private Quaternion[] minValueRotations;
        [SerializeField, HideInInspector]
        private Vector3[] minValueScales;

        Transform transformRoot;

        public void SetAnimation(float point)
        {
            if (setPositions)
                for (int i = 0; i < transformReferences.Length; i++)
                    transformReferences[i].localPosition = Vector3.Lerp(minValuePositions[i], maxValuePositions[i], animationCurve.Evaluate(point));

            if (setRotations)
                for (int i = 0; i < transformReferences.Length; i++)
                    transformReferences[i].localRotation = Quaternion.Lerp(minValueRotations[i], maxValueRotations[i], animationCurve.Evaluate(point));

            if (setScale)
                for (int i = 0; i < transformReferences.Length; i++)
                    transformReferences[i].localScale = Vector3.Lerp(minValueScales[i], maxValueScales[i], animationCurve.Evaluate(point));
        }

        internal Vector3 GetTransformPositionAtPoint(int index, float point)
        {
            return Vector3.Lerp(minValuePositions[index], maxValuePositions[index], animationCurve.Evaluate(point));
        }

        internal int GetTransformIndex(Transform transform)
        {
            for (int i = 0; i < transformReferences.Length; i++)
            {
                if (transformReferences[i].Equals(transform))
                    return i;
            }

            return -1;
        }

        [NaughtyAttributes.Button("Save Start"), ContextMenu("Save Start")]
        public void SaveAnimationStart()
        {
            Debug.Log("Saved Start Pose");
            SaveAnimation(AnimationPoint.start);
        }

        [NaughtyAttributes.Button("Save End"), ContextMenu("Save End")]
        public void SaveAnimationEnd() {
            Debug.Log("Saved End Pose");
            SaveAnimation(AnimationPoint.end);
        }

        [Range(0, 1)]
        public float setTestValue = 0;
        [NaughtyAttributes.Button("Set")]
        public void SetAnimation()
        {
            SetAnimation(setTestValue);
        }

        public void SaveAnimation(AnimationPoint animationPoint)
        {
            if(transformRoot == null)
                transformRoot = transform;

            int count = 0;
            void GetRecursiveChildCount(Transform obj){
                count++;
                if (recordChildTransforms)
                    for (int k = 0; k < obj.childCount; k++)
                        GetRecursiveChildCount(obj.GetChild(k));
            }

            GetRecursiveChildCount(transformRoot);

            transformReferences = new Transform[count];
            if (animationPoint == AnimationPoint.end)
            {
                maxValuePositions = new Vector3[count];
                maxValueRotations = new Quaternion[count];
                maxValueScales = new Vector3[count];
            }
            else if(animationPoint == AnimationPoint.start)
            {
                minValuePositions = new Vector3[count];
                minValueRotations = new Quaternion[count];
                minValueScales = new Vector3[count];
            }


            int currIndex = 0;
            AssignChildrenPose(transformRoot);


            void AssignChildrenPose(Transform obj) {

                AssignPoint(currIndex, obj.localPosition, obj.localRotation, obj.localScale, obj);
                currIndex++;

                if (recordChildTransforms)
                    for (int j = 0; j < obj.childCount; j++)
                        AssignChildrenPose(obj.GetChild(j));
            }


            void AssignPoint(int point, Vector3 pos, Quaternion rot, Vector3 scale, Transform joint)
            {
                transformReferences[point] = joint;
                if (animationPoint == AnimationPoint.end)
                {
                    maxValuePositions[point] = pos;
                    maxValueRotations[point] = rot;
                    maxValueScales[point] = scale;
                }
                else if (animationPoint == AnimationPoint.start)
                {
                    minValuePositions[point] = pos;
                    minValueRotations[point] = rot;
                    minValueScales[point] = scale;
                }
            }
        }
    }
}