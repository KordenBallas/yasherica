using Combat.Battlefield;
using LevelGeneration.Surface;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class PlatformShapeSettingsTests
    {
        [Test]
        public void ShapeProfile_ClampsInvalidValues()
        {
            var profile = new ShapeProfile(minCells: 0, maxCells: -5, compactness: 99);

            Assert.AreEqual(1, profile.MinCells);
            Assert.AreEqual(1, profile.MaxCells, "max is raised to min");
            Assert.AreEqual(ShapeProfile.MaxCompactness, profile.Compactness);

            Assert.AreEqual(0, new ShapeProfile(2, 4, -3).Compactness);
        }

        [Test]
        public void Settings_ClampInvalidValues()
        {
            var settings = new PlatformShapeSettings(
                hexSize: -1f, orientation: HexOrientation.Pointy,
                empty: null, loot: null, combat: null, npc: null,
                battlefieldMinimumCells: 0,
                rimWidth: -2f, rimJitterPercent: 500, rimDropHeight: -1f,
                platformThickness: 0f, gapBetweenPlatforms: -1f, heightDeviation: -1f,
                cellInset: -0.5f);

            Assert.AreEqual(PlatformShapeSettings.DefaultHexSize, settings.HexSize);
            Assert.AreEqual(HexOrientation.Pointy, settings.Orientation);
            Assert.NotNull(settings.Empty);
            Assert.NotNull(settings.Combat);
            Assert.AreEqual(1, settings.BattlefieldMinimumCells);
            Assert.AreEqual(0f, settings.RimWidth);
            Assert.AreEqual(100, settings.RimJitterPercent);
            Assert.AreEqual(0f, settings.RimDropHeight);
            Assert.AreEqual(PlatformShapeSettings.DefaultPlatformThickness, settings.PlatformThickness);
            Assert.AreEqual(0f, settings.GapBetweenPlatforms);
            Assert.AreEqual(0f, settings.HeightDeviation);
            Assert.AreEqual(0f, settings.CellInset);
        }

        [Test]
        public void ProfileFor_MapsEveryContentKind()
        {
            var settings = PlatformShapeSettings.CreateDefault();

            Assert.AreSame(settings.Empty, settings.ProfileFor(PlatformContentKind.Empty));
            Assert.AreSame(settings.Loot, settings.ProfileFor(PlatformContentKind.Loot));
            Assert.AreSame(settings.Combat, settings.ProfileFor(PlatformContentKind.Combat));
            Assert.AreSame(settings.Npc, settings.ProfileFor(PlatformContentKind.Npc));
        }

        [Test]
        public void Defaults_MatchTheVerifiedBriefDecisions()
        {
            var settings = PlatformShapeSettings.CreateDefault();

            Assert.AreEqual(2f, settings.HexSize, "must stay in lockstep with the combat grid");
            Assert.AreEqual(HexOrientation.Flat, settings.Orientation);
            Assert.AreEqual(12, settings.BattlefieldMinimumCells, "user-confirmed arena floor");
            Assert.GreaterOrEqual(settings.Combat.MinCells, settings.BattlefieldMinimumCells);
            Assert.Less(settings.Loot.MaxCells, settings.Combat.MinCells,
                "loot platforms must read visibly smaller than combat arenas");
            Assert.Less(settings.Empty.MaxCells, settings.Combat.MinCells);
        }
    }
}
