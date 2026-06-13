using System;
using CharacterSystem.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class BoneMapResolverTests
    {
        private BoneMapResolver _resolver;
        private SkeletonData _skeleton;

        [SetUp]
        public void SetUp()
        {
            _resolver = new BoneMapResolver();
            _skeleton = new SkeletonData(
                "skeleton.placeholder",
                new[] { "Root", "Pelvis", "Spine", "Chest", "Head" },
                Array.Empty<SocketInfo>());
        }

        [Test]
        public void Resolve_AllBonesPresent_IsValidAndPreservesOrder()
        {
            var result = _resolver.Resolve(new[] { "Chest", "Spine", "Head" }, _skeleton);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.MissingBoneNames.Count);
            CollectionAssert.AreEqual(new[] { "Chest", "Spine", "Head" }, result.BoneNames);
        }

        [Test]
        public void Resolve_MissingBones_ReportedByName()
        {
            var result = _resolver.Resolve(new[] { "Spine", "Tail.0", "Wing.L" }, _skeleton);

            Assert.IsFalse(result.IsValid);
            CollectionAssert.AreEqual(new[] { "Tail.0", "Wing.L" }, result.MissingBoneNames);
        }

        [Test]
        public void Resolve_MatchingIsCaseSensitive()
        {
            var result = _resolver.Resolve(new[] { "spine" }, _skeleton);

            Assert.IsFalse(result.IsValid);
            CollectionAssert.AreEqual(new[] { "spine" }, result.MissingBoneNames);
        }

        [Test]
        public void Resolve_EmptyBoneList_IsInvalid()
        {
            var result = _resolver.Resolve(Array.Empty<string>(), _skeleton);

            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void Resolve_NullBoneList_IsInvalid()
        {
            var result = _resolver.Resolve(null, _skeleton);

            Assert.IsFalse(result.IsValid);
        }
    }
}
