using System.Collections.Generic;
using Core.Logging;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class LogLevelPolicyTests
    {
        private static LogLevelPolicy Policy(
            LogLevel master,
            LogLevel defaultLevel,
            params (LogCategory category, LogLevel level)[] overrides)
        {
            var map = new Dictionary<LogCategory, LogLevel>();
            foreach (var (category, level) in overrides)
            {
                map[category] = level;
            }

            return new LogLevelPolicy(master, defaultLevel, map);
        }

        [Test]
        public void CategoryAtInfo_AllowsEverySeverity()
        {
            var policy = Policy(LogLevel.Info, LogLevel.Info, (LogCategory.Combat, LogLevel.Info));

            Assert.IsTrue(policy.ShouldLog(LogCategory.Combat, LogLevel.Info));
            Assert.IsTrue(policy.ShouldLog(LogCategory.Combat, LogLevel.Warning));
            Assert.IsTrue(policy.ShouldLog(LogCategory.Combat, LogLevel.Error));
        }

        [Test]
        public void CategoryAtOff_SilencesEverySeverity()
        {
            var policy = Policy(LogLevel.Info, LogLevel.Info, (LogCategory.Combat, LogLevel.Off));

            Assert.IsFalse(policy.ShouldLog(LogCategory.Combat, LogLevel.Info));
            Assert.IsFalse(policy.ShouldLog(LogCategory.Combat, LogLevel.Warning));
            Assert.IsFalse(policy.ShouldLog(LogCategory.Combat, LogLevel.Error));
        }

        [Test]
        public void CategoryAtError_KeepsErrorsButDropsInfoAndWarning()
        {
            var policy = Policy(LogLevel.Info, LogLevel.Info, (LogCategory.Narrative, LogLevel.Error));

            Assert.IsTrue(policy.ShouldLog(LogCategory.Narrative, LogLevel.Error));
            Assert.IsFalse(policy.ShouldLog(LogCategory.Narrative, LogLevel.Warning));
            Assert.IsFalse(policy.ShouldLog(LogCategory.Narrative, LogLevel.Info));
        }

        [Test]
        public void CategoryAtWarning_KeepsErrorsAndWarningsButDropsInfo()
        {
            var policy = Policy(LogLevel.Info, LogLevel.Info, (LogCategory.Loot, LogLevel.Warning));

            Assert.IsTrue(policy.ShouldLog(LogCategory.Loot, LogLevel.Error));
            Assert.IsTrue(policy.ShouldLog(LogCategory.Loot, LogLevel.Warning));
            Assert.IsFalse(policy.ShouldLog(LogCategory.Loot, LogLevel.Info));
        }

        [Test]
        public void UnlistedCategory_UsesDefaultLevel()
        {
            var policy = Policy(LogLevel.Info, LogLevel.Warning /* default */);

            // Mutation is not listed, so it falls back to the default ceiling (Warning).
            Assert.IsTrue(policy.ShouldLog(LogCategory.Mutation, LogLevel.Warning));
            Assert.IsFalse(policy.ShouldLog(LogCategory.Mutation, LogLevel.Info));
        }

        [Test]
        public void MasterCeiling_OverridesAMoreVerboseCategory()
        {
            // Category wants everything, but the master ceiling caps the whole game at Error.
            var policy = Policy(LogLevel.Error, LogLevel.Info, (LogCategory.Combat, LogLevel.Info));

            Assert.IsTrue(policy.ShouldLog(LogCategory.Combat, LogLevel.Error));
            Assert.IsFalse(policy.ShouldLog(LogCategory.Combat, LogLevel.Warning));
            Assert.IsFalse(policy.ShouldLog(LogCategory.Combat, LogLevel.Info));
        }

        [Test]
        public void MasterOff_SilencesEverything()
        {
            var policy = Policy(LogLevel.Off, LogLevel.Info, (LogCategory.Combat, LogLevel.Info));

            Assert.IsFalse(policy.ShouldLog(LogCategory.Combat, LogLevel.Error));
            Assert.IsFalse(policy.ShouldLog(LogCategory.General, LogLevel.Error));
        }

        [Test]
        public void AllEnabled_LogsEverythingAtInfo()
        {
            var policy = LogLevelPolicy.AllEnabled();

            Assert.IsTrue(policy.ShouldLog(LogCategory.General, LogLevel.Info));
            Assert.IsTrue(policy.ShouldLog(LogCategory.Combat, LogLevel.Error));
        }
    }
}
