using System;
using CharacterSystem.Data;
using CharacterSystem.Runtime;
using Core.Logging;
using Core.Persistence;
using Zenject;

namespace CharacterSystem.Integration
{
    /// <summary>
    /// Installs the Hub-chosen starting part on the hero at run start (O1). Fresh runs only — a
    /// restored run's body snapshot already carries whatever was installed. Waits for the hero rig
    /// to assemble (the same discipline as <see cref="CharacterSystem.Runtime.HeroBodyRestorer"/>),
    /// applies exactly once, then unhooks. Starting parts are pre-filtered to base-skeleton fits
    /// (no frame-changers), so a plain <c>SwapPart</c> suffices — no body-plan coordinator hop, no
    /// shed-confirm modal; <c>PartsChanged</c> then drives the passport (the 1-marker tolerated
    /// freak) and the tasted catalog through their existing binders.
    /// </summary>
    public sealed class StartingPartApplier : IInitializable, IDisposable
    {
        private readonly ModularCharacterVisual _visual;
        private readonly IPartCatalog _partCatalog;
        private readonly RunRestoreContext _restoreContext;
        private readonly RunStartConditions _startConditions;
        private readonly IGameLogger _logger;

        private bool _applied;

        public StartingPartApplier(ModularCharacterVisual visual, IPartCatalog partCatalog,
            RunRestoreContext restoreContext, RunStartConditions startConditions,
            IGameLogger logger = null)
        {
            _visual = visual;
            _partCatalog = partCatalog;
            _restoreContext = restoreContext;
            _startConditions = startConditions;
            _logger = logger;
        }

        public void Initialize()
        {
            if (_visual == null || !ShouldApply(_restoreContext, _startConditions))
            {
                return;
            }

            _visual.CharacterAssembled += HandleAssembled;
            if (_visual.Character != null)
            {
                HandleAssembled(_visual.Character);
            }
        }

        public void Dispose() => Unhook();

        /// <summary>Pure core: a starting part applies only to a fresh run that actually chose one.</summary>
        public static bool ShouldApply(RunRestoreContext restoreContext, RunStartConditions conditions)
        {
            if (restoreContext != null && restoreContext.IsRestoring)
            {
                return false;
            }

            return !string.IsNullOrEmpty(conditions?.StartingPartId);
        }

        /// <summary>Pure core: resolves and installs the part; missing/failing parts degrade to a
        /// bare launch (FR14 tolerance), never a broken run start.</summary>
        public static void Apply(string partId, IPartCatalog partCatalog, IModularCharacter character,
            IGameLogger logger = null)
        {
            if (character == null || partCatalog == null || string.IsNullOrEmpty(partId))
            {
                return;
            }

            if (!partCatalog.TryGet(partId, out var part))
            {
                logger?.Warning(LogCategory.Persistence,
                    $"[StartingPartApplier] Starting part '{partId}' is missing from the part catalog; " +
                    "launching bare (FR14 tolerance).");
                return;
            }

            if (!character.SwapPart(part))
            {
                logger?.Warning(LogCategory.Persistence,
                    $"[StartingPartApplier] Starting part '{partId}' failed to install on the hero; " +
                    "launching bare.");
                return;
            }

            logger?.Info(LogCategory.Persistence,
                $"[StartingPartApplier] Starting part '{partId}' installed on the hero (O1 launch choice).");
        }

        private void HandleAssembled(IModularCharacter character)
        {
            if (_applied || character == null)
            {
                return;
            }

            // Set BEFORE the swap: SwapPart raises PartsChanged, and a rebuilt visual would re-fire
            // CharacterAssembled — the launch choice installs exactly once.
            _applied = true;
            Unhook();
            Apply(_startConditions.StartingPartId, _partCatalog, character, _logger);
        }

        private void Unhook()
        {
            if (_visual != null)
            {
                _visual.CharacterAssembled -= HandleAssembled;
            }
        }
    }
}
