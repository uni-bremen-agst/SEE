﻿using UnityEngine;

namespace Crosstales.Common.Util
{
   /// <summary>Random rotation changer.</summary>
   [DisallowMultipleComponent]
   public class RandomRotator : MonoBehaviour
   {
      #region Variables

      ///<summary>Use intervals to change the rotation (default: true).</summary>
      [Tooltip("Use intervals to change the rotation (default: true).")] public bool UseInterval = true;

      ///<summary>Random change interval between min (= x) and max (= y) in seconds (default: x = 10, y = 20).</summary>
      [Tooltip("Random change interval between min (= x) and max (= y) in seconds (default: x = 10, y = 20).")]
      public Vector2 ChangeInterval = new Vector2(10, 20);

      ///<summary>Minimum rotation speed per axis (default: 5 for all axis).</summary>
      [Tooltip("Minimum rotation speed per axis (default: 5 for all axis).")] public Vector3 SpeedMin = new Vector3(5, 5, 5);

      ///<summary>Maximum rotation speed per axis (default: 15 for all axis).</summary>
      [Tooltip("Minimum rotation speed per axis (default: 15 for all axis).")] public Vector3 SpeedMax = new Vector3(15, 15, 15);

      ///<summary>Set the object to a random rotation at Start (default: false).</summary>
      [Tooltip("Set the object to a random rotation at Start (default: false).")] public bool RandomRotationAtStart;

      ///<summary>Random change interval per axis (default: true).</summary>
      [Tooltip("Random change interval per axis (default: true).")] public bool RandomChangeIntervalPerAxis = true;

      private Transform tf;
      private Vector3 speed;
      private float elapsedTime;
      private float changeTime;

      private Vector3 elapsedTimeAxis = Vector3.zero;
      private Vector3 changeTimeAxis;

      #endregion


      #region MonoBehaviour methods

      private void Start()
      {
         tf = transform;

         if (RandomChangeIntervalPerAxis)
         {
            elapsedTimeAxis.x = changeTimeAxis.x = Random.Range(ChangeInterval.x, ChangeInterval.y);
            elapsedTimeAxis.y = changeTimeAxis.y = Random.Range(ChangeInterval.x, ChangeInterval.y);
            elapsedTimeAxis.z = changeTimeAxis.z = Random.Range(ChangeInterval.x, ChangeInterval.y);
         }
         else
         {
            elapsedTime = changeTime = Random.Range(ChangeInterval.x, ChangeInterval.y);
         }

         if (RandomRotationAtStart)
            tf.localRotation = Random.rotation;
      }

      private void Update()
      {
         if (UseInterval)
         {
            if (RandomChangeIntervalPerAxis)
            {
               elapsedTimeAxis.x += Time.deltaTime;
               elapsedTimeAxis.y += Time.deltaTime;
               elapsedTimeAxis.z += Time.deltaTime;

               if (elapsedTimeAxis.x > changeTimeAxis.x)
               {
                  elapsedTimeAxis.x = 0f;

                  speed.x = Random.Range(Mathf.Abs(SpeedMin.x), Mathf.Abs(SpeedMax.x)) * (Random.Range(0, 2) == 0 ? 1 : -1);
                  changeTimeAxis.x = Random.Range(ChangeInterval.x, ChangeInterval.y);
               }

               if (elapsedTimeAxis.y > changeTimeAxis.y)
               {
                  elapsedTimeAxis.y = 0f;

                  speed.y = Random.Range(Mathf.Abs(SpeedMin.y), Mathf.Abs(SpeedMax.y)) * (Random.Range(0, 2) == 0 ? 1 : -1);
                  changeTimeAxis.y = Random.Range(ChangeInterval.x, ChangeInterval.y);
               }

               if (elapsedTimeAxis.z > changeTimeAxis.z)
               {
                  elapsedTimeAxis.z = 0f;

                  speed.z = Random.Range(Mathf.Abs(SpeedMin.z), Mathf.Abs(SpeedMax.z)) * (Random.Range(0, 2) == 0 ? 1 : -1);
                  changeTimeAxis.z = Random.Range(ChangeInterval.x, ChangeInterval.y);
               }
            }
            else
            {
               elapsedTime += Time.deltaTime;

               if (elapsedTime > changeTime)
               {
                  elapsedTime = 0f;

                  speed.x = Random.Range(Mathf.Abs(SpeedMin.x), Mathf.Abs(SpeedMax.x)) * (Random.Range(0, 2) == 0 ? 1 : -1);
                  speed.y = Random.Range(Mathf.Abs(SpeedMin.y), Mathf.Abs(SpeedMax.y)) * (Random.Range(0, 2) == 0 ? 1 : -1);
                  speed.z = Random.Range(Mathf.Abs(SpeedMin.z), Mathf.Abs(SpeedMax.z)) * (Random.Range(0, 2) == 0 ? 1 : -1);
                  changeTime = Random.Range(ChangeInterval.x, ChangeInterval.y);
               }
            }

            tf.Rotate(speed.x * Time.deltaTime, speed.y * Time.deltaTime, speed.z * Time.deltaTime);
         }
      }

      #endregion
   }
}
// © 2015-2021 crosstales LLC (https://www.crosstales.com)