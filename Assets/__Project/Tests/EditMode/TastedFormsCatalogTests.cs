using System.Collections.Generic;
using System.Linq;
using CharacterSystem.Integration;
using Combat.Arena.Data;
using Narrative.Facts.Core;
using Narrative.Runtime.Snapshots;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// The tasted-forms catalog cores (P4-5 reqs 1–3): the Journey-side recorder marks carried
    /// part ids as Meta facts idempotently; the Arena-side extraction reads exactly the
    /// <c>world.&lt;partId&gt;.arena_tasted</c> entries back out of a fact snapshot.
    /// </summary>
    [TestFixture]
    public class TastedFormsCatalogTests
    {
        private FactStore _facts;

        [SetUp]
        public void SetUp()
        {
            _facts = new FactStore();
        }

        // ---- recorder core ----

        [Test]
        public void Record_MarksEveryCarriedPartId()
        {
            TastedFormsRecorder.Record(new[] { "part.head.a", "part.tail.a" }, _facts);

            Assert.IsTrue(_facts.GetBool(WorldFacts.ArenaTasted, "part.head.a"));
            Assert.IsTrue(_facts.GetBool(WorldFacts.ArenaTasted, "part.tail.a"));
        }

        [Test]
        public void Record_IsIdempotent_AndNeverUnmarks()
        {
            TastedFormsRecorder.Record(new[] { "part.head.a" }, _facts);
            TastedFormsRecorder.Record(new[] { "part.torso.a" }, _facts);

            Assert.IsTrue(_facts.GetBool(WorldFacts.ArenaTasted, "part.head.a"));
            Assert.IsTrue(_facts.GetBool(WorldFacts.ArenaTasted, "part.torso.a"));
        }

        [Test]
        public void Record_IgnoresNullAndEmptyIds()
        {
            TastedFormsRecorder.Record(new[] { null, string.Empty, "part.head.a" }, _facts);

            Assert.AreEqual(1, _facts.Snapshot().Count);
        }

        // ---- reader core ----

        [Test]
        public void Extract_ReturnsOnlyTastedSubjects()
        {
            TastedFormsRecorder.Record(new[] { "part.head.a", "part.arm.b" }, _facts);
            _facts.SetBool(WorldFacts.BarnRaided, true);
            _facts.SetBool(WorldFacts.SpineSeen, true, "story.mirror");

            var snapshot = FactStoreSnapshotMapper.Capture(_facts);
            var tasted = ArenaTastedCatalogReader.ExtractTastedPartIds(snapshot);

            Assert.AreEqual(
                new[] { "part.arm.b", "part.head.a" },
                tasted.OrderBy(id => id).ToArray());
        }

        [Test]
        public void Extract_SkipsFalseEntriesAndEmptySubjects()
        {
            var snapshot = new FactStoreSnapshot
            {
                Entries = new List<FactEntryDto>
                {
                    new FactEntryDto
                    {
                        Namespace = FactNamespace.World, Subject = "part.head.a",
                        Key = "arena_tasted", Type = FactValueType.Bool, BoolValue = false
                    },
                    new FactEntryDto
                    {
                        Namespace = FactNamespace.World, Subject = "",
                        Key = "arena_tasted", Type = FactValueType.Bool, BoolValue = true
                    }
                }
            };

            Assert.IsEmpty(ArenaTastedCatalogReader.ExtractTastedPartIds(snapshot));
        }

        [Test]
        public void Extract_EmptyOrNullSnapshot_YieldsEmptyCatalog()
        {
            Assert.IsEmpty(ArenaTastedCatalogReader.ExtractTastedPartIds(null));
            Assert.IsEmpty(ArenaTastedCatalogReader.ExtractTastedPartIds(new FactStoreSnapshot()));
        }
    }
}
