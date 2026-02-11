using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DG.Tweening;

namespace DoTween
{
    /// <summary>
    /// Tests to assert our assumptions about DOTween.
    /// </summary>
    internal class TestDoTween
    {
        /// <summary>
        /// The object treated by a tween.
        /// </summary>
        private GameObject tweenTarget;

        /// <summary>
        /// Original value of <see cref="DOTween.defaultAutoKill"/>.
        /// </summary>
        private AutoPlay previous;

        [SetUp]
        public void Setup()
        {
            // 1. Create a clean GameObject for every test
            tweenTarget = new GameObject("TweenTarget");

            // 2. Initialize DOTween (safe measure)
            DOTween.Init();
            previous = DOTween.defaultAutoPlay;
            DOTween.defaultAutoPlay = AutoPlay.None;
        }

        [TearDown]
        public void Teardown()
        {
            // 3. Clean up all tweens to prevent side effects between tests
            DOTween.KillAll();

            if (tweenTarget != null)
            {
                Object.Destroy(tweenTarget);
            }
            // Restore original value.
            DOTween.defaultAutoPlay = previous;
        }

        // ---------------------------------------------------------
        // Natural Completion (Should fire BOTH)
        // ---------------------------------------------------------
        [UnityTest]
        public IEnumerator OnComplete_Fires_Then_OnKill_Fires()
        {
            // Note according to the DOTween documentation:
            // By default tweens are automatically killed at completion,
            // but one can change the default behaviour in DOTween's Utility panel.
            // We have autokill enabled.

            bool onCompleteCalled = false;
            bool onKillCalled = false;
            int order = 0;

            // Create a short tween (0.1s)
            tweenTarget.transform.DOMoveX(10, 0.1f)
                .OnComplete(() => { onCompleteCalled = true; order = 1; })
                .OnKill(() => { onKillCalled = true; order = 2; })
                .Play(); // Manually play since we set defaultAutoPlay to None

            // Wait for tween to finish (0.1s + buffer)
            yield return new WaitForSeconds(0.2f);

            // Assertions
            Assert.IsTrue(onCompleteCalled, "OnComplete should have been called.");
            Assert.IsTrue(onKillCalled, "OnKill should have been called.");
            Assert.IsTrue(order == 2, "OnKill should have been called after completion.");
        }

        // ---------------------------------------------------------
        // Manual Kill (Should fire ONLY OnKill)
        // ---------------------------------------------------------
        [UnityTest]
        public IEnumerator ManualKill_Fires_Only_OnKill()
        {
            bool onCompleteCalled = false;
            bool onKillCalled = false;

            // Create a long tween (2s) so it doesn't finish naturally
            Tween myTween = tweenTarget.transform.DOMoveX(10, 2f)
                .OnComplete(() => onCompleteCalled = true)
                .OnKill(() => onKillCalled = true)
                .Play();

            // Wait a frame to ensure it started
            yield return null;

            // Manually kill the tween
            myTween.Kill();

            // Assertions
            Assert.IsFalse(onCompleteCalled, "OnComplete should NOT be called on manual kill.");
            Assert.IsTrue(onKillCalled, "OnKill SHOULD be called on manual kill.");
        }

        // ---------------------------------------------------------
        // Object Destruction (Should fire ONLY OnKill)
        // ---------------------------------------------------------
        [UnityTest]
        public IEnumerator ObjectDestroy_Fires_Only_OnKill()
        {
            bool onCompleteCalled = false;
            bool onKillCalled = false;

            // Create a long tween attached to the object
            tweenTarget.transform.DOMoveX(10, 2f)
                .SetLink(tweenTarget) // Vital: Links tween life to GameObject life
                .OnComplete(() => onCompleteCalled = true)
                .OnKill(() => onKillCalled = true)
                .Play();

            yield return null;

            // Destroy the object
            Object.Destroy(tweenTarget);

            // Wait a frame for Unity to process the destruction
            yield return null;

            // Assertions
            Assert.IsFalse(onCompleteCalled, "OnComplete should NOT be called when object is destroyed.");
            Assert.IsTrue(onKillCalled, "OnKill SHOULD be called when object is destroyed.");
        }

        // -------------------------------------------------------------------
        // We cannot have multiple OnKill callbacks. Only the last one counts.
        // -------------------------------------------------------------------
        [UnityTest]
        public IEnumerator Multiple_OnKill()
        {
            bool onKillCalled1 = false;
            bool onKillCalled2 = false;

            // Create a short tween attached to the object
            tweenTarget.transform.DOMoveX(10, 0.1f)
                .SetLink(tweenTarget) // Vital: Links tween life to GameObject life
                .OnKill(() => onKillCalled1 = true)
                .OnKill(() => onKillCalled2 = true)
                .Play();

            // Wait for tween to finish (0.1s + buffer)
            yield return new WaitForSeconds(0.2f);

            // Assertions
            Assert.IsFalse(onKillCalled1, "First OnKill should NOT be called.");
            Assert.IsTrue(onKillCalled2, "Second OnKill should be called.");
        }

        // -----------------------------------------------------------------------
        // We cannot have multiple OnComplete callbacks. Only the last one counts.
        // -----------------------------------------------------------------------
        [UnityTest]
        public IEnumerator Multiple_OnComplete()
        {
            bool onCompleteCalled1 = false;
            bool onCompleteCalled2 = false;

            // Create a short tween attached to the object
            tweenTarget.transform.DOMoveX(10, 0.1f)
                .SetLink(tweenTarget) // Vital: Links tween life to GameObject life
                .OnKill(() => onCompleteCalled1 = true)
                .OnKill(() => onCompleteCalled2 = true)
                .Play();

            // Wait for tween to finish (0.1s + buffer)
            yield return new WaitForSeconds(0.2f);

            // Assertions
            Assert.IsFalse(onCompleteCalled1, "First OnComplete should NOT be called.");
            Assert.IsTrue(onCompleteCalled2, "Second OnComplete should be called.");
        }

    }
}
