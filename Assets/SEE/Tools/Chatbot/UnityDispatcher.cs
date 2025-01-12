using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace Assets.SEE.Tools.Chatbot
{
        public class UnityDispatcher : MonoBehaviour
    {
        private static readonly ConcurrentQueue<Action> actions = new ConcurrentQueue<Action>();
        private static UnityDispatcher instance;

        public static void Initialize()
        {
            if (instance == null)
            {
                instance = new GameObject("UnityDispatcher").AddComponent<UnityDispatcher>();
                DontDestroyOnLoad(instance.gameObject);
            }
        }
        /// <summary>
        /// Creating a Queue for incoming actions
        /// </summary>
        /// <param name="action"></param>
        public static void Enqueue(Action action)
        {
            actions.Enqueue(action);
        }
        /// <summary>
        /// Running actions on main-thread if there is something present in the queue
        /// </summary>
        private void Update()
        {
            while (actions.TryDequeue(out var action))
            {
                action.Invoke();
            }
        }

    }
}