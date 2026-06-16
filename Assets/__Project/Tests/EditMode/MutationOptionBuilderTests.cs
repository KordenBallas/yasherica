using System.Collections.Generic;
using Mutation.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class MutationOptionBuilderTests
    {
        private sealed class FakeOptionProvider : IMutationOptionProvider
        {
            private readonly Dictionary<string, IReadOnlyList<MutationOption>> _byArchetype =
                new Dictionary<string, IReadOnlyList<MutationOption>>();

            public FakeOptionProvider With(string archetypeId, params MutationOption[] options)
            {
                _byArchetype[archetypeId] = options;
                return this;
            }

            public IReadOnlyList<MutationOption> OptionsFor(string archetypeId)
            {
                return _byArchetype.TryGetValue(archetypeId, out var options)
                    ? options
                    : System.Array.Empty<MutationOption>();
            }
        }

        private static MutationOption Option(string slot, string part, string archetype)
        {
            return new MutationOption(slot, part, archetype, part);
        }

        private static HashSet<string> Equipped(params string[] partIds)
        {
            return new HashSet<string>(partIds);
        }

        private readonly MutationOptionBuilder _builder = new MutationOptionBuilder();

        [Test]
        public void Build_GathersAcrossDominantArchetypesInOrder()
        {
            var provider = new FakeOptionProvider()
                .With("reptile", Option("slot.head", "part.head.r", "reptile"))
                .With("insect", Option("slot.tail", "part.tail.i", "insect"));

            var result = _builder.Build(new[] { "reptile", "insect" }, provider, Equipped(), 3);

            CollectionAssert.AreEqual(
                new[] { "part.head.r", "part.tail.i" },
                ToPartIds(result));
        }

        [Test]
        public void Build_RespectsAuthoredOrderWithinAnArchetype()
        {
            var provider = new FakeOptionProvider()
                .With("reptile",
                    Option("slot.head", "part.head.r", "reptile"),
                    Option("slot.tail", "part.tail.r", "reptile"));

            var result = _builder.Build(new[] { "reptile" }, provider, Equipped(), 3);

            CollectionAssert.AreEqual(
                new[] { "part.head.r", "part.tail.r" },
                ToPartIds(result));
        }

        [Test]
        public void Build_CapsAtMaxOptions()
        {
            var provider = new FakeOptionProvider()
                .With("reptile",
                    Option("slot.head", "part.a", "reptile"),
                    Option("slot.tail", "part.b", "reptile"),
                    Option("slot.torso", "part.c", "reptile"));

            var result = _builder.Build(new[] { "reptile" }, provider, Equipped(), 2);

            Assert.AreEqual(2, result.Count);
            CollectionAssert.AreEqual(new[] { "part.a", "part.b" }, ToPartIds(result));
        }

        [Test]
        public void Build_ExcludesEquippedParts()
        {
            var provider = new FakeOptionProvider()
                .With("reptile",
                    Option("slot.head", "part.head.r", "reptile"),
                    Option("slot.tail", "part.tail.r", "reptile"));

            var result = _builder.Build(new[] { "reptile" }, provider, Equipped("part.head.r"), 3);

            CollectionAssert.AreEqual(new[] { "part.tail.r" }, ToPartIds(result));
        }

        [Test]
        public void Build_DedupesPartSharedByTwoArchetypes_FirstWins()
        {
            var provider = new FakeOptionProvider()
                .With("reptile", Option("slot.head", "shared.part", "reptile"))
                .With("aquatic", Option("slot.head", "shared.part", "aquatic"));

            var result = _builder.Build(new[] { "reptile", "aquatic" }, provider, Equipped(), 3);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("reptile", result[0].ArchetypeId);
        }

        [Test]
        public void Build_SkipsOptionsWithEmptyPartId()
        {
            var provider = new FakeOptionProvider()
                .With("reptile",
                    Option("slot.head", "", "reptile"),
                    Option("slot.tail", "part.tail.r", "reptile"));

            var result = _builder.Build(new[] { "reptile" }, provider, Equipped(), 3);

            CollectionAssert.AreEqual(new[] { "part.tail.r" }, ToPartIds(result));
        }

        [Test]
        public void Build_NoData_ReturnsEmpty()
        {
            var provider = new FakeOptionProvider();

            var result = _builder.Build(new[] { "reptile" }, provider, Equipped(), 3);

            CollectionAssert.IsEmpty(result);
        }

        [Test]
        public void Build_NonPositiveMax_ReturnsEmpty()
        {
            var provider = new FakeOptionProvider()
                .With("reptile", Option("slot.head", "part.head.r", "reptile"));

            CollectionAssert.IsEmpty(_builder.Build(new[] { "reptile" }, provider, Equipped(), 0));
            CollectionAssert.IsEmpty(_builder.Build(new[] { "reptile" }, provider, Equipped(), -1));
        }

        [Test]
        public void Build_NullArguments_ReturnEmpty()
        {
            var provider = new FakeOptionProvider();

            CollectionAssert.IsEmpty(_builder.Build(null, provider, Equipped(), 3));
            CollectionAssert.IsEmpty(_builder.Build(new[] { "reptile" }, null, Equipped(), 3));
        }

        [Test]
        public void Build_IsDeterministicAcrossRuns()
        {
            var provider = new FakeOptionProvider()
                .With("reptile", Option("slot.head", "part.head.r", "reptile"))
                .With("insect", Option("slot.tail", "part.tail.i", "insect"));

            var first = ToPartIds(_builder.Build(new[] { "reptile", "insect" }, provider, Equipped(), 3));
            var second = ToPartIds(_builder.Build(new[] { "reptile", "insect" }, provider, Equipped(), 3));

            CollectionAssert.AreEqual(first, second);
        }

        private static List<string> ToPartIds(IReadOnlyList<MutationOption> options)
        {
            var ids = new List<string>(options.Count);
            foreach (var option in options)
            {
                ids.Add(option.PartId);
            }

            return ids;
        }
    }
}
