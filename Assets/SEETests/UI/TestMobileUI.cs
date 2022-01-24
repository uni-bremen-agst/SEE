using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEE.Controls.Actions;
using SEE.Game.UI.Menu;
using UnityEngine;
using UnityEngine.Events;

namespace SEETests.UI
{
    public class TestMobileUI
    {
        [Test]
        public void TestMobileActionStateCount()
        {
            Assert.IsTrue(MobileActionStateType.AllTypes.Count == 21);
        }

    }
}


