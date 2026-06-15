using System;
using System.Collections.Generic;
using Mutation.Data;
using Mutation.Data.Definitions;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class ArchetypeCatalogTests
    {
        private readonly List<ArchetypeDefinition> _created = new List<ArchetypeDefinition>();

        [TearDown]
        public void TearDown()
        {
            foreach (var definition in _created)
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }

            _created.Clear();
        }

        private ArchetypeDefinition Archetype(string id)
        {
            var definition = ScriptableObject.CreateInstance<ArchetypeDefinition>();
            // _id is a private serialized field; set it via reflection for the test.
            typeof(ArchetypeDefinition)
                .GetField("_id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(definition, id);
            _created.Add(definition);
            return definition;
        }

        [Test]
        public void TryGet_ExistingId_ReturnsDefinition()
        {
            var catalog = new ArchetypeCatalog(new[] { Archetype("reptile"), Archetype("aquatic") });

            Assert.IsTrue(catalog.TryGet("reptile", out var definition));
            Assert.AreEqual("reptile", definition.Id);
        }

        [Test]
        public void TryGet_UnknownId_ReturnsFalse()
        {
            var catalog = new ArchetypeCatalog(new[] { Archetype("reptile") });

            Assert.IsFalse(catalog.TryGet("mammal", out _));
        }

        [Test]
        public void Contains_ReflectsKnownIds()
        {
            var catalog = new ArchetypeCatalog(new[] { Archetype("reptile") });

            Assert.IsTrue(catalog.Contains("reptile"));
            Assert.IsFalse(catalog.Contains("avian"));
            Assert.IsFalse(catalog.Contains(null));
        }

        [Test]
        public void All_ExposesEveryDefinition()
        {
            var catalog = new ArchetypeCatalog(new[] { Archetype("reptile"), Archetype("aquatic") });

            Assert.AreEqual(2, catalog.All.Count);
        }

        [Test]
        public void Constructor_DuplicateId_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                new ArchetypeCatalog(new[] { Archetype("reptile"), Archetype("reptile") }));
        }

        [Test]
        public void Constructor_EmptyId_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                new ArchetypeCatalog(new[] { Archetype(string.Empty) }));
        }

        [Test]
        public void Constructor_NullDefinitions_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ArchetypeCatalog(null));
        }
    }
}
