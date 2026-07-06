using System;
using System.Collections.Generic;
using CharacterSystem.Data;
using CharacterSystem.Data.Definitions;
using Core.Logging;
using Core.Persistence;
using Zenject;

namespace CharacterSystem.Runtime
{
    /// <summary>
    /// The character-system side of <see cref="IHeroBodyPersistence"/>. Capture reads the live
    /// hero's public surface (governing skeleton + equipped/dormant slot→part). Restore STAGES the
    /// saved body and applies it once the hero rig assembles (the rig builds in the visual's
    /// <c>Start()</c>, after the restore coordinator runs) — the same rebuild transaction a frame
    /// change uses, via the factory's staged-build overload. A body identical to the authored
    /// initial assembly is left untouched (no rebuild, keeps its default attachments); attachments
    /// on a rebuilt body follow the frame-change policy (they die with the old rig).
    ///
    /// Skeleton lookup follows the body-plan precedent: the SO graph already links every reachable
    /// skeleton (the authored assembly's plus each frame-changer part's TargetSkeleton), so no
    /// separate skeleton catalog asset is needed (KISS).
    /// </summary>
    public sealed class HeroBodyRestorer : IHeroBodyPersistence, IInitializable, IDisposable
    {
        private readonly ModularCharacterVisual _visual;
        private readonly IModularCharacterFactory _factory;
        private readonly IPartCatalog _partCatalog;
        private readonly IGameLogger _logger;

        private HeroBodySnapshot _pending;

        public HeroBodyRestorer(ModularCharacterVisual visual, IModularCharacterFactory factory,
            IPartCatalog partCatalog, IGameLogger logger = null)
        {
            _visual = visual;
            _factory = factory;
            _partCatalog = partCatalog;
            _logger = logger;
        }

        public void Initialize()
        {
            if (_visual != null)
            {
                _visual.CharacterAssembled += OnCharacterAssembled;
            }
        }

        public void Dispose()
        {
            if (_visual != null)
            {
                _visual.CharacterAssembled -= OnCharacterAssembled;
            }
        }

        public HeroBodySnapshot Capture()
        {
            var character = _visual != null ? _visual.Character : null;
            if (character == null)
            {
                return null;
            }

            var snapshot = new HeroBodySnapshot { SkeletonId = character.SkeletonId };
            foreach (var pair in character.EquippedParts)
            {
                snapshot.EquippedSlotIds.Add(pair.Key);
                snapshot.EquippedPartIds.Add(pair.Value);
            }

            foreach (var part in character.DormantParts)
            {
                snapshot.DormantSlotIds.Add(part.Slot != null ? part.Slot.Id : string.Empty);
                snapshot.DormantPartIds.Add(part.Id);
            }

            return snapshot;
        }

        public void Restore(HeroBodySnapshot snapshot)
        {
            if (snapshot == null || string.IsNullOrEmpty(snapshot.SkeletonId))
            {
                return;
            }

            _pending = snapshot;
            if (_visual != null && _visual.Character != null)
            {
                Apply();
            }
        }

        private void OnCharacterAssembled(IModularCharacter character)
        {
            if (_pending != null)
            {
                Apply();
            }
        }

        private void Apply()
        {
            // Cleared BEFORE the rebuild: ReplaceCharacter re-fires CharacterAssembled and this
            // must not re-enter.
            var snapshot = _pending;
            _pending = null;

            var character = _visual.Character;
            if (MatchesCurrentBody(snapshot, character))
            {
                return;
            }

            var skeleton = FindSkeleton(snapshot.SkeletonId);
            if (skeleton == null)
            {
                _logger?.Warning(LogCategory.Persistence,
                    $"[HeroBodyRestorer] Saved skeleton '{snapshot.SkeletonId}' is not reachable from the " +
                    "part/assembly graph; keeping the authored initial body (FR14 tolerance).");
                return;
            }

            var activeParts = ResolveParts(snapshot.EquippedPartIds);
            var dormantParts = ResolveParts(snapshot.DormantPartIds);

            // Frame-change policy: staged all-or-nothing build; attachments die with the old rig.
            var next = _factory.Create(skeleton, activeParts, dormantParts, null, _visual.transform);
            if (next == null)
            {
                _logger?.Warning(LogCategory.Persistence,
                    $"[HeroBodyRestorer] Staged rebuild of the saved body on '{skeleton.Id}' failed; " +
                    "keeping the authored initial body.");
                return;
            }

            var old = character as ModularCharacter;
            _visual.ReplaceCharacter(next, skeleton);
            if (old != null)
            {
                UnityEngine.Object.Destroy(old.gameObject);
            }

            _logger?.Info(LogCategory.Persistence,
                $"[HeroBodyRestorer] Hero body restored on '{skeleton.Id}' " +
                $"({snapshot.EquippedPartIds.Count} part(s), {snapshot.DormantPartIds.Count} dormant).");
        }

        private static bool MatchesCurrentBody(HeroBodySnapshot snapshot, IModularCharacter character)
        {
            if (character == null
                || !string.Equals(character.SkeletonId, snapshot.SkeletonId, StringComparison.Ordinal)
                || character.EquippedParts.Count != snapshot.EquippedSlotIds.Count
                || character.DormantParts.Count != snapshot.DormantPartIds.Count)
            {
                return false;
            }

            for (int i = 0; i < snapshot.EquippedSlotIds.Count; i++)
            {
                if (!character.EquippedParts.TryGetValue(snapshot.EquippedSlotIds[i], out var partId)
                    || !string.Equals(partId, snapshot.EquippedPartIds[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            foreach (var dormant in character.DormantParts)
            {
                if (!snapshot.DormantPartIds.Contains(dormant.Id))
                {
                    return false;
                }
            }

            return true;
        }

        private List<PartDefinition> ResolveParts(List<string> partIds)
        {
            var parts = new List<PartDefinition>(partIds.Count);
            foreach (var partId in partIds)
            {
                if (_partCatalog.TryGet(partId, out var part))
                {
                    parts.Add(part);
                }
                else
                {
                    _logger?.Warning(LogCategory.Persistence,
                        $"[HeroBodyRestorer] Saved part '{partId}' is missing from the catalog; skipped.");
                }
            }

            return parts;
        }

        private SkeletonDefinition FindSkeleton(string skeletonId)
        {
            // The authored assembly's skeleton plus every frame-changer's target skeleton — the
            // same reachable-graph rule BodyPlanSwapCoordinator uses.
            var baseSkeleton = _visual.Assembly != null ? _visual.Assembly.Skeleton : null;
            if (baseSkeleton != null && string.Equals(baseSkeleton.Id, skeletonId, StringComparison.Ordinal))
            {
                return baseSkeleton;
            }

            foreach (var part in _partCatalog.All)
            {
                var target = part != null ? part.TargetSkeleton : null;
                if (target != null && string.Equals(target.Id, skeletonId, StringComparison.Ordinal))
                {
                    return target;
                }
            }

            return null;
        }
    }
}
