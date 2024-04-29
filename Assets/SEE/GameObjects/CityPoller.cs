using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SEE.Game.City;
using SEE.UI.RuntimeConfigMenu;
using SEE.Utils.Paths;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace SEE.GO
{
    [Serializable]
    public class CityPoller : MonoBehaviour
    {
        /// <summary>
        /// The inverval in ms for runnig fetch
        /// </summary>
        ///
        [ShowInInspector]
        public float Interval { get; set; } = 0.5f;

        [ShowInInspector]
        public AbstractSEECity City;

        private float _current;
        
        [OdinSerialize]
        [ShowInInspector, Tooltip("Paths to the git repositories that should be watched for changes"),
         HideReferenceObjectPicker,
         RuntimeTab("Data")]
        public HashSet<DirectoryPath> WatchedRepoPaths { get; set; }


        private void Start()
        {
            if (TryGetComponent(out AbstractSEECity city))
            {
                City = city;
            }

            //StartCoroutine("RunCoroutine");
        }

        private IEnumerable<WaitForSeconds> RunCoroutine()
        {
            yield return new WaitForSeconds(5);
            Debug.Log("Running fetch");
        }

      
    }
}