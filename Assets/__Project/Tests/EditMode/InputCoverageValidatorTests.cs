using GameInput.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// The coverage rule itself (Input Foundation R2): these tests run against the REAL catalog, so a
    /// newly added action with a missing source binding — or a stale allowlist entry — fails the build,
    /// not a code review.
    /// </summary>
    [TestFixture]
    public class InputCoverageValidatorTests
    {
        private readonly InputCoverageValidator _validator = new InputCoverageValidator();
        private readonly InputBindingCatalog _catalog = new InputBindingCatalog();

        [Test]
        public void EveryActionSourcePair_IsBoundOrConsciouslyDeferred()
        {
            var problems = _validator.FindUncoveredPairs(_catalog);

            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void NoAllowlistEntry_IsStale()
        {
            var problems = _validator.FindStaleAllowlistEntries(_catalog);

            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
