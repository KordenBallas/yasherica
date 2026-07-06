using System.Collections.Generic;
using CharacterProgression.Core;
using Core.Logging;
using Inventory.Core;
using Loot.Core;
using Mutation.Core;
using Narrative.Actors.Core;
using Narrative.Casting.Core;
using Narrative.Dialogue;
using Narrative.Quests.Core;
using Narrative.Runtime.Snapshots;

namespace Core.Persistence
{
    /// <summary>
    /// Explicit whole-run aggregator (D4): one constructor-injected class composes every section of
    /// the continue image, because restore ORDER matters (facts before actors before quests before
    /// stuff) and an explicit sequence is simpler to read and test than a contributor plugin scheme.
    /// The wide dependency list is this class's single responsibility: it is the one place that
    /// knows what "the whole run" is.
    ///
    /// Crafting normalization (D10): a mid-staging cauldron session is captured as plain inventory
    /// items (staged + uncollected result) WITHOUT mutating the live session — an invisible autosave
    /// must never change what the player sees (A3); on continue the session simply resumes Idle with
    /// the items back in the stash (FR7: mid-action state re-begins).
    /// </summary>
    public sealed class RunStateService : IRunStateService
    {
        private readonly INarrativeSaveService _narrativeSave;
        private readonly ILiveActorRegistry _actors;
        private readonly ILiveQuestRegistry _quests;
        private readonly IFragmentLibrary _fragments;
        private readonly IRunProgressionRecord _progressionRecord;
        private readonly IRunProgressionRecorder _progressionRecorder;
        private readonly IInventoryModel _inventory;
        private readonly IPartInventoryModel _partStash;
        private readonly IBlankRack _blankRack;
        private readonly ISocketingModel _socketing;
        private readonly ICraftingSession _crafting;
        private readonly IRunSeedProvider _runSeed;
        private readonly IHeroBodyPersistence _heroBody;
        private readonly IWorldStatePersistence _world;
        private readonly IGameLogger _logger;
        private readonly RunStartConditions _startConditions;

        public RunStateService(
            INarrativeSaveService narrativeSave,
            ILiveActorRegistry actors,
            ILiveQuestRegistry quests,
            IFragmentLibrary fragments,
            IRunProgressionRecord progressionRecord,
            IRunProgressionRecorder progressionRecorder,
            IInventoryModel inventory,
            IPartInventoryModel partStash,
            IBlankRack blankRack,
            ISocketingModel socketing,
            ICraftingSession crafting,
            IRunSeedProvider runSeed = null,
            IHeroBodyPersistence heroBody = null,
            IWorldStatePersistence world = null,
            IGameLogger logger = null,
            RunStartConditions startConditions = null)
        {
            _narrativeSave = narrativeSave;
            _actors = actors;
            _quests = quests;
            _fragments = fragments;
            _progressionRecord = progressionRecord;
            _progressionRecorder = progressionRecorder;
            _inventory = inventory;
            _partStash = partStash;
            _blankRack = blankRack;
            _socketing = socketing;
            _crafting = crafting;
            _runSeed = runSeed;
            _heroBody = heroBody;
            _world = world;
            _logger = logger;
            _startConditions = startConditions;
        }

        public bool TryCaptureAll(DialogueRunnerState dialogueState, out RunSaveSnapshot snapshot)
        {
            snapshot = null;
            if (!_narrativeSave.TryCapture(dialogueState, out var narrative))
            {
                return false;
            }

            narrative.Actors = ActorSnapshotMapper.Capture(_actors);
            narrative.Quests = QuestSnapshotMapper.Capture(_quests);

            snapshot = new RunSaveSnapshot
            {
                RunSeed = _runSeed?.RunSeed ?? 0,
                // The Hub's window-0 override must ride every savepoint (O1): the biome journey
                // replays from the seed on resume, so this is the only carrier of the chosen entry.
                StartingBiome = _startConditions?.StartingBiomeName ?? string.Empty,
                Narrative = narrative,
                World = _world?.Capture() ?? new WorldStateSnapshot(),
                Body = _heroBody?.Capture() ?? new HeroBodySnapshot(),
                Stuff = CaptureStuff()
            };
            return true;
        }

        public void RestoreAll(RunSaveSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            // 1. Facts (both horizons as saved), thread + story ledgers, PRNG state.
            _narrativeSave.Restore(snapshot.Narrative);

            // 2. Minted actors — before quests/castings so id lookups resolve.
            ActorSnapshotMapper.Restore(snapshot.Narrative.Actors, _actors);

            // 3. Quest lifecycle replay (repopulates the progression record's quest statuses).
            QuestSnapshotMapper.Restore(snapshot.Narrative.Quests, _quests, _fragments,
                _progressionRecorder, _logger);

            // 4. Progression extras the quest replay does not cover.
            RestoreProgressionExtras(snapshot.Stuff);

            // 5. Inventory, part stash, blank rack + re-socketing.
            RestoreStuff(snapshot.Stuff);

            // 6. Hero body (applied when the rig assembles).
            _heroBody?.Restore(snapshot.Body);
        }

        private PlayerStuffSnapshot CaptureStuff()
        {
            var stuff = new PlayerStuffSnapshot { NextArtifactInstanceId = _inventory.NextInstanceId };

            foreach (var item in _inventory.Items)
            {
                stuff.Artifacts.Add(ToDto(item));
            }

            // Crafting normalization: staged items and an uncollected result ride as plain items.
            foreach (var staged in _crafting.StagedItems)
            {
                stuff.Artifacts.Add(ToDto(staged));
            }

            if (_crafting.PendingResult != null)
            {
                stuff.Artifacts.Add(ToDto(_crafting.PendingResult));
            }

            stuff.PartIds.AddRange(_partStash.PartIds);

            stuff.NextBlankInstanceId = _blankRack.NextInstanceId;
            foreach (var blank in _blankRack.Blanks)
            {
                stuff.Blanks.Add(new BlankInstanceDto
                {
                    InstanceId = blank.InstanceId,
                    DefinitionId = blank.DefinitionId
                });

                var socketed = _socketing.SocketedArtifacts(blank.InstanceId);
                if (socketed.Count == 0)
                {
                    continue;
                }

                var dto = new SocketedBlankDto { BlankInstanceId = blank.InstanceId };
                foreach (var artifact in socketed)
                {
                    dto.Artifacts.Add(ToDto(artifact));
                }

                stuff.SocketedByBlank.Add(dto);
            }

            stuff.EncounteredNpcs.AddRange(_progressionRecord.EncounteredNpcs);
            foreach (var choice in _progressionRecord.Choices)
            {
                stuff.Choices.Add(new ChoiceDto { Key = choice.Key, Value = choice.Value });
            }

            return stuff;
        }

        private void RestoreProgressionExtras(PlayerStuffSnapshot stuff)
        {
            if (stuff == null)
            {
                return;
            }

            foreach (var npcId in stuff.EncounteredNpcs)
            {
                _progressionRecorder.RecordNpcEncounter(npcId);
            }

            foreach (var choice in stuff.Choices)
            {
                _progressionRecorder.RecordChoice(choice.Key, choice.Value);
            }
        }

        private void RestoreStuff(PlayerStuffSnapshot stuff)
        {
            if (stuff == null)
            {
                return;
            }

            _inventory.RestoreFrom(ToInstances(stuff.Artifacts), stuff.NextArtifactInstanceId);
            _partStash.RestoreFrom(stuff.PartIds);

            var blanks = new List<BlankInstance>(stuff.Blanks.Count);
            foreach (var blank in stuff.Blanks)
            {
                blanks.Add(new BlankInstance(blank.InstanceId, blank.DefinitionId));
            }

            _blankRack.RestoreFrom(blanks, stuff.NextBlankInstanceId);

            // Re-socket: each socketed artifact passes through the inventory (Return) and is pulled
            // back out by TrySocket, replaying the exact live flow. A blank whose definition is gone
            // leaves its artifacts in the inventory — degraded, never lost (FR14 tolerance).
            foreach (var socketedBlank in stuff.SocketedByBlank)
            {
                foreach (var artifactDto in socketedBlank.Artifacts)
                {
                    var artifact = new ArtifactInstance(artifactDto.InstanceId, artifactDto.DefinitionId);
                    _inventory.Return(artifact);
                    if (!_socketing.TrySocket(socketedBlank.BlankInstanceId, artifact.InstanceId))
                    {
                        _logger?.Warning(LogCategory.Persistence,
                            $"[RunStateService] Could not re-socket artifact {artifact.InstanceId} " +
                            $"into blank {socketedBlank.BlankInstanceId}; it stays in the inventory.");
                    }
                }
            }
        }

        private static ArtifactInstanceDto ToDto(ArtifactInstance instance) => new ArtifactInstanceDto
        {
            InstanceId = instance.InstanceId,
            DefinitionId = instance.DefinitionId
        };

        private static List<ArtifactInstance> ToInstances(List<ArtifactInstanceDto> dtos)
        {
            var instances = new List<ArtifactInstance>(dtos.Count);
            foreach (var dto in dtos)
            {
                instances.Add(new ArtifactInstance(dto.InstanceId, dto.DefinitionId));
            }

            return instances;
        }
    }
}
