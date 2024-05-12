using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.GO
{
    public class GitPoller : MonoBehaviour
    {
        public List<string> WatchedRepositories = new();

        private void Start()
        {
            InvokeRepeating("PollRepos", 5, 5);
        }


        void PollRepos()
        {
            Debug.Log("Poll");
        }
    }
}