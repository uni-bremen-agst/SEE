using NUnit.Framework;
using UnityEngine;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Provides the common UI environment required by EditMode tests of
    /// Drawable menus.
    /// </summary>
    public abstract class DrawableMenuTestBase
    {
        /// <summary>
        /// The UI canvas required by Drawable menus when instantiating their
        /// prefabs.
        /// </summary>
        protected GameObject UICanvasObject { get; private set; }

        /// <summary>
        /// Creates the UI environment required by Drawable menu tests.
        /// </summary>
        [SetUp]
        public void SetUpDrawableMenuTest()
        {
            UICanvasObject = new GameObject(
                "UI Canvas",
                typeof(RectTransform),
                typeof(Canvas));
        }

        /// <summary>
        /// Destroys the UI environment created for the current test.
        /// </summary>
        [TearDown]
        public void TearDownDrawableMenuTest()
        {
            if (UICanvasObject != null)
            {
                Object.DestroyImmediate(UICanvasObject);
            }
        }
    }
}
