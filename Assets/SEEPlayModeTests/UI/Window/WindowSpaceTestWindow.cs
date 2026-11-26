
using System;
using UnityEngine;

namespace SEE.UI.Window
{
    /// <summary>
    /// Provides a minimal window for PlayMode tests of WindowSpace.
    /// Unlike regular SEE windows, this component does not instantiate
    /// the BaseWindow prefab or initialize unrelated game systems.
    /// </summary>
    public sealed class WindowSpaceTestWindow : BaseWindow
    {
        /// <summary>
        /// Creates only the RectTransform needed by DynamicPanels.
        /// </summary>
        protected override void StartDesktop()
        {
            Window = new GameObject(
                Title ?? "WindowSpace Test Window",
                typeof(RectTransform));

            Window.transform.SetParent(Canvas.transform, false);
        }

        /// <summary>
        /// Rebuilds the window layout.
        /// The minimal test window does not require layout updates.
        /// </summary>
        public override void RebuildLayout()
        {
        }

        /// <summary>
        /// Initializes a window from serialized values.
        /// This operation is not supported by the test window.
        /// </summary>
        /// <param name="valueObject">The serialized window values.</param>
        /// <exception cref="NotSupportedException">
        /// Always thrown because the test window does not support serialization.
        /// </exception>
        protected override void InitializeFromValueObject(
            WindowValues valueObject)
        {
            throw new NotSupportedException(
                "The test window does not support deserialization.");
        }

        /// <summary>
        /// Updates a window from values received over the network.
        /// This operation is not supported by the test window.
        /// </summary>
        /// <param name="valueObject">The received window values.</param>
        /// <exception cref="NotSupportedException">
        /// Always thrown because the test window does not support networking.
        /// </exception>
        public override void UpdateFromNetworkValueObject(
            WindowValues valueObject)
        {
            throw new NotSupportedException(
                "The test window does not support network updates.");
        }

        /// <summary>
        /// Converts the window into serialized values.
        /// This operation is not supported by the test window.
        /// </summary>
        /// <returns>
        /// No value is returned because serialization is unsupported.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Always thrown because the test window does not support serialization.
        /// </exception>
        public override WindowValues ToValueObject()
        {
            throw new NotSupportedException(
                "The test window does not support serialization.");
        }
    }
}
