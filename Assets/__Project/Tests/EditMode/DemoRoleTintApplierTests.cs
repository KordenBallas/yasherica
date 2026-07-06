using CharacterSystem.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// The alpha-0 sentinel rule: unedited assets (default tint) must stay untinted, any authored
    /// colour with alpha applies. The renderer walk itself is a thin Unity adapter, not tested here.
    /// </summary>
    public class DemoRoleTintApplierTests
    {
        [Test]
        public void ShouldTint_DefaultColor_IsFalse()
        {
            Assert.IsFalse(DemoRoleTintApplier.ShouldTint(new Color(0f, 0f, 0f, 0f)));
        }

        [Test]
        public void ShouldTint_ColouredButTransparent_IsFalse()
        {
            Assert.IsFalse(DemoRoleTintApplier.ShouldTint(new Color(0.5f, 0.2f, 0.2f, 0f)));
        }

        [Test]
        public void ShouldTint_OpaqueColour_IsTrue()
        {
            Assert.IsTrue(DemoRoleTintApplier.ShouldTint(new Color(0.35f, 0.05f, 0.12f, 1f)));
        }
    }
}
