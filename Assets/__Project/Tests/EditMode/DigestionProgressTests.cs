using Mutation.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class DigestionProgressTests
    {
        [Test]
        public void Constructor_NonPositiveThreshold_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new DigestionProgress(0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new DigestionProgress(-1));
        }

        [Test]
        public void New_IsEmptyAndNotReady()
        {
            var progress = new DigestionProgress(3);

            Assert.AreEqual(0, progress.Fed);
            Assert.AreEqual(3, progress.Threshold);
            Assert.AreEqual(0f, progress.Normalized);
            Assert.IsFalse(progress.IsReadyToMutate);
        }

        [Test]
        public void AddArtifact_IncrementsAndRaisesChanged()
        {
            var progress = new DigestionProgress(4);
            var changed = 0;
            progress.OnChanged += () => changed++;

            progress.AddArtifact();

            Assert.AreEqual(1, progress.Fed);
            Assert.AreEqual(0.25f, progress.Normalized);
            Assert.AreEqual(1, changed);
        }

        [Test]
        public void Normalized_ClampsToOnePastThreshold()
        {
            var progress = new DigestionProgress(2);

            progress.AddArtifact();
            progress.AddArtifact();
            progress.AddArtifact();

            Assert.AreEqual(3, progress.Fed);
            Assert.AreEqual(1f, progress.Normalized);
        }

        [Test]
        public void IsReadyToMutate_FlipsAtThreshold()
        {
            var progress = new DigestionProgress(2);

            progress.AddArtifact();
            Assert.IsFalse(progress.IsReadyToMutate);

            progress.AddArtifact();
            Assert.IsTrue(progress.IsReadyToMutate);
        }

        [Test]
        public void Reset_ClearsAndRaisesChanged_WhenNonEmpty()
        {
            var progress = new DigestionProgress(2);
            progress.AddArtifact();
            var changed = 0;
            progress.OnChanged += () => changed++;

            progress.Reset();

            Assert.AreEqual(0, progress.Fed);
            Assert.IsFalse(progress.IsReadyToMutate);
            Assert.AreEqual(1, changed);
        }

        [Test]
        public void Reset_WhenAlreadyEmpty_DoesNotRaiseChanged()
        {
            var progress = new DigestionProgress(2);
            var changed = 0;
            progress.OnChanged += () => changed++;

            progress.Reset();

            Assert.AreEqual(0, changed);
        }
    }
}
