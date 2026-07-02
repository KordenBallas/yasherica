using System.Collections.Generic;
using CharacterProgression.Core;
using Core.Logging;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class RunConditionEvaluatorTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public readonly List<string> Warnings = new();
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) => Warnings.Add(message);
            public void Error(LogCategory category, string message) { }
        }

        private FakeLogger _logger;
        private RunConditionEvaluator _evaluator;
        private RunProgressionRecord _record;

        [SetUp]
        public void SetUp()
        {
            _logger = new FakeLogger();
            _evaluator = new RunConditionEvaluator(_logger);
            _record = new RunProgressionRecord();
        }

        [Test]
        public void EmptyOrNullCondition_Passes()
        {
            Assert.IsTrue(_evaluator.Evaluate(null, _record));
            Assert.IsTrue(_evaluator.Evaluate("", _record));
            Assert.IsTrue(_evaluator.Evaluate("   ", _record));
        }

        [Test]
        public void QuestCompleted_TrueOnlyWhenCompleted()
        {
            Assert.IsFalse(_evaluator.Evaluate("quest_completed:q1", _record));

            _record.StartQuest("q1");
            Assert.IsFalse(_evaluator.Evaluate("quest_completed:q1", _record));

            _record.CompleteQuest("q1");
            Assert.IsTrue(_evaluator.Evaluate("quest_completed:q1", _record));
        }

        [Test]
        public void QuestActive_TrueOnlyWhenActive()
        {
            _record.StartQuest("q1");
            Assert.IsTrue(_evaluator.Evaluate("quest_active:q1", _record));

            _record.CompleteQuest("q1");
            Assert.IsFalse(_evaluator.Evaluate("quest_active:q1", _record));
        }

        [Test]
        public void QuestFailed_TrueOnlyWhenFailed()
        {
            _record.FailQuest("q1");
            Assert.IsTrue(_evaluator.Evaluate("quest_failed:q1", _record));
            Assert.IsFalse(_evaluator.Evaluate("quest_failed:other", _record));
        }

        [Test]
        public void NpcEncountered_TrueOnlyAfterEncounter()
        {
            Assert.IsFalse(_evaluator.Evaluate("npc_encountered:elder", _record));

            _record.RecordNpcEncounter("elder");
            Assert.IsTrue(_evaluator.Evaluate("npc_encountered:elder", _record));
        }

        [Test]
        public void Condition_IsWhitespaceTrimmed()
        {
            _record.RecordNpcEncounter("elder");
            Assert.IsTrue(_evaluator.Evaluate("  npc_encountered : elder  ", _record));
        }

        [Test]
        public void UnknownPrefix_FailsClosedAndWarns()
        {
            Assert.IsFalse(_evaluator.Evaluate("has_item:sword", _record));
            Assert.AreEqual(1, _logger.Warnings.Count);
        }

        [Test]
        public void MalformedCondition_FailsClosedAndWarns()
        {
            Assert.IsFalse(_evaluator.Evaluate("quest_completed", _record));
            Assert.IsFalse(_evaluator.Evaluate(":q1", _record));
            Assert.IsFalse(_evaluator.Evaluate("quest_completed:", _record));
            Assert.AreEqual(3, _logger.Warnings.Count);
        }
    }
}
