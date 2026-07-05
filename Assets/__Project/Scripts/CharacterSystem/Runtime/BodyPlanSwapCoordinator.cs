using System;
using System.Collections.Generic;
using CharacterSystem.Core;
using CharacterSystem.Data;
using CharacterSystem.Data.Definitions;
using Core.Logging;
using UnityEngine;
using Zenject;

namespace CharacterSystem.Runtime
{
    /// <summary>
    /// Orchestrates transitions BETWEEN bodies (the assembly controller owns one body on one
    /// rig): routes every gameplay part-install through the pure body-plan planner, keeps the
    /// cheap same-frame swap untouched, and executes the rare frame change as an all-or-nothing
    /// transaction — the new body is fully staged (inactive) before the live one is torn down,
    /// so a failed build leaves the character, the blank, and the inventory untouched.
    /// Pure C# class; never a MonoBehaviour.
    /// </summary>
    public class BodyPlanSwapCoordinator
    {
        private readonly IPartCatalog _partCatalog;
        private readonly IModularCharacterFactory _factory;
        private readonly ModularCharacterVisual _visual;
        private readonly IGameLogger _logger;
        private readonly IBodyPlanConfirmPrompt _confirmPrompt;
        private readonly IShedPartSink _shedPartSink;
        private readonly BodyPlanChangePlanner _planner = new BodyPlanChangePlanner();

        private bool _transactionPending;

        /// <summary>Final result of a request that returned
        /// <see cref="BodyPlanInstallOutcome.PendingConfirmation"/>: true = installed,
        /// false = declined or failed (body unchanged).</summary>
        public event Action<bool> InstallResolved;

        public BodyPlanSwapCoordinator(
            IPartCatalog partCatalog,
            IModularCharacterFactory factory,
            ModularCharacterVisual visual,
            IGameLogger logger,
            [InjectOptional] IBodyPlanConfirmPrompt confirmPrompt = null,
            [InjectOptional] IShedPartSink shedPartSink = null)
        {
            _partCatalog = partCatalog;
            _factory = factory;
            _visual = visual;
            _logger = logger;
            _confirmPrompt = confirmPrompt;
            _shedPartSink = shedPartSink;
        }

        /// <summary>True when the part could be installed on the current body right now —
        /// used to filter unseal offers so incompatible parts are never presented.</summary>
        public bool CanInstall(string partId)
        {
            if (!TryBuildContext(partId, out var context))
            {
                return false;
            }

            return context.Plan.Kind != BodyPlanChangeKind.Incompatible;
        }

        public BodyPlanInstallOutcome RequestInstall(string slotId, string partId)
        {
            if (_transactionPending)
            {
                _logger.Warning(LogCategory.CharacterSystem,
                    "[BodyPlanSwap] An install is already awaiting confirmation; ignoring the new request.");
                return BodyPlanInstallOutcome.Rejected;
            }

            if (!TryBuildContext(partId, out var context))
            {
                return BodyPlanInstallOutcome.Rejected;
            }

            if (context.Part.Slot == null || !string.Equals(context.Part.Slot.Id, slotId, StringComparison.Ordinal))
            {
                _logger.Error(LogCategory.CharacterSystem,
                    $"[BodyPlanSwap] Part '{partId}' belongs to slot '{context.Part.Slot?.Id}', not '{slotId}'.");
                return BodyPlanInstallOutcome.Rejected;
            }

            switch (context.Plan.Kind)
            {
                case BodyPlanChangeKind.InstantSwap:
                    // The ~80% path: untouched same-frame swap, no prompt, no rebuild.
                    return context.Character.SwapPart(context.Part)
                        ? BodyPlanInstallOutcome.Applied
                        : BodyPlanInstallOutcome.Rejected;

                case BodyPlanChangeKind.DormantInstall:
                    _logger.Info(LogCategory.CharacterSystem,
                        $"[BodyPlanSwap] '{context.Part.Id}' loses the body-plan priority to '{context.Plan.GoverningPartId}' and is carried dormant.");
                    return context.Character.EquipDormant(context.Part)
                        ? BodyPlanInstallOutcome.Applied
                        : BodyPlanInstallOutcome.Rejected;

                case BodyPlanChangeKind.FrameChange:
                    return HandleFrameChange(context);

                default:
                    _logger.Warning(LogCategory.CharacterSystem,
                        $"[BodyPlanSwap] Part '{partId}' cannot be installed on the current body (incompatible with skeleton '{context.Plan.GoverningSkeletonId}').");
                    return BodyPlanInstallOutcome.Rejected;
            }
        }

        private BodyPlanInstallOutcome HandleFrameChange(InstallContext context)
        {
            if (!context.Plan.RequiresConfirmation)
            {
                // FR7: a frame change that sheds nothing needs no prompt.
                return ExecuteFrameChange(context)
                    ? BodyPlanInstallOutcome.Applied
                    : BodyPlanInstallOutcome.Rejected;
            }

            if (_confirmPrompt == null)
            {
                _logger.Warning(LogCategory.CharacterSystem,
                    "[BodyPlanSwap] No confirm prompt is bound; auto-confirming a shedding body-plan change (demo-scene fallback).");
                return ExecuteFrameChange(context)
                    ? BodyPlanInstallOutcome.Applied
                    : BodyPlanInstallOutcome.Rejected;
            }

            _transactionPending = true;
            _confirmPrompt.Request(BuildSummary(context), confirmed =>
            {
                _transactionPending = false;
                var installed = confirmed && ExecuteFrameChange(context);
                InstallResolved?.Invoke(installed);
            });

            return BodyPlanInstallOutcome.PendingConfirmation;
        }

        /// <summary>Build-before-destroy: every fallible step happens against a staged,
        /// inactive rig; only after the factory returns a complete body does the live rig
        /// get torn down (nothing to roll back on failure).</summary>
        private bool ExecuteFrameChange(InstallContext context)
        {
            var governingSkeleton = context.SkeletonDefinitionsById[context.Plan.GoverningSkeletonId];

            var activeParts = ResolveDefinitions(context, context.Plan.ActiveParts);
            var dormantParts = ResolveDefinitions(context, context.Plan.DormantParts);

            var staging = new GameObject("BodyPlanStaging");
            staging.SetActive(false);
            staging.transform.SetParent(_visual.transform, false);

            var newCharacter = _factory.Create(governingSkeleton, activeParts, dormantParts, null, staging.transform);
            if (newCharacter == null)
            {
                UnityEngine.Object.Destroy(staging);
                _logger.Error(LogCategory.CharacterSystem,
                    $"[BodyPlanSwap] Staged build on skeleton '{governingSkeleton.Id}' failed; the live body is untouched.");
                return false;
            }

            // Commit point — only non-fallible operations from here on.
            var oldCharacter = context.Character as ModularCharacter;
            newCharacter.transform.SetParent(_visual.transform, false);
            UnityEngine.Object.Destroy(staging);

            _visual.ReplaceCharacter(newCharacter, governingSkeleton);

            if (oldCharacter != null)
            {
                // Attachments and sockets die with the old rig (R9-style policy on a frame change).
                UnityEngine.Object.Destroy(oldCharacter.gameObject);
            }

            StoreShedParts(context.Plan.ShedParts);
            _logger.Info(LogCategory.CharacterSystem,
                $"[BodyPlanSwap] Body re-formed on '{governingSkeleton.Id}' (governor '{context.Plan.GoverningPartId}', shed {context.Plan.ShedParts.Count} part(s)).");
            return true;
        }

        private void StoreShedParts(IReadOnlyList<PartData> shedParts)
        {
            if (shedParts.Count == 0)
            {
                return;
            }

            var shedIds = new List<string>(shedParts.Count);
            foreach (var part in shedParts)
            {
                shedIds.Add(part.PartId);
            }

            if (_shedPartSink == null)
            {
                _logger.Warning(LogCategory.CharacterSystem,
                    $"[BodyPlanSwap] No shed-part sink is bound; {shedIds.Count} shed part(s) are dropped: {string.Join(", ", shedIds)}.");
                return;
            }

            _shedPartSink.Store(shedIds);
        }

        private BodyPlanChangeSummary BuildSummary(InstallContext context)
        {
            var frameName = context.SkeletonDefinitionsById.TryGetValue(context.Plan.GoverningSkeletonId, out var skeleton)
                ? skeleton.DisplayName
                : context.Plan.GoverningSkeletonId;

            var shedNames = new List<string>(context.Plan.ShedParts.Count);
            foreach (var shed in context.Plan.ShedParts)
            {
                shedNames.Add(ResolveDisplayName(context, shed.PartId));
            }

            return new BodyPlanChangeSummary(frameName, shedNames);
        }

        private string ResolveDisplayName(InstallContext context, string partId)
        {
            if (context.PartDefinitionsById.TryGetValue(partId, out var part))
            {
                return string.IsNullOrEmpty(part.DisplayName) ? part.name : part.DisplayName;
            }

            return partId;
        }

        private List<PartDefinition> ResolveDefinitions(InstallContext context, IReadOnlyList<PartData> parts)
        {
            var definitions = new List<PartDefinition>(parts.Count);
            foreach (var part in parts)
            {
                if (context.PartDefinitionsById.TryGetValue(part.PartId, out var definition))
                {
                    definitions.Add(definition);
                }
            }

            return definitions;
        }

        private bool TryBuildContext(string partId, out InstallContext context)
        {
            context = null;

            if (!_partCatalog.TryGet(partId, out var incomingPart))
            {
                _logger.Error(LogCategory.CharacterSystem,$"[BodyPlanSwap] Unknown part id '{partId}'.");
                return false;
            }

            var character = _visual != null ? _visual.Character : null;
            if (character == null)
            {
                _logger.Warning(LogCategory.CharacterSystem,
                    "[BodyPlanSwap] The character is not assembled yet; install requests are rejected.");
                return false;
            }

            var baseSkeleton = _visual.Assembly != null ? _visual.Assembly.Skeleton : null;
            if (baseSkeleton == null)
            {
                _logger.Error(LogCategory.CharacterSystem,"[BodyPlanSwap] The visual has no base assembly skeleton.");
                return false;
            }

            // The SO graph already links every reachable skeleton: the base assembly's plus each
            // (potential) governor's TargetSkeleton. No separate skeleton catalog needed (KISS).
            var partDefinitions = new Dictionary<string, PartDefinition>(StringComparer.Ordinal);
            var skeletonDefinitions = new Dictionary<string, SkeletonDefinition>(StringComparer.Ordinal);
            RegisterSkeleton(skeletonDefinitions, baseSkeleton);

            var equipped = new List<PartData>();
            CollectParts(character.EquippedPartDefinitions, partDefinitions, skeletonDefinitions, equipped);
            CollectParts(character.DormantParts, partDefinitions, skeletonDefinitions, equipped);

            partDefinitions[incomingPart.Id] = incomingPart;
            RegisterSkeleton(skeletonDefinitions, incomingPart.TargetSkeleton);
            var incoming = DefinitionMapper.ToPartData(incomingPart);

            var skeletonDataById = new Dictionary<string, SkeletonData>(StringComparer.Ordinal);
            var plan = _planner.Plan(
                character.SkeletonId,
                baseSkeleton.Id,
                equipped,
                incoming,
                skeletonId =>
                {
                    if (skeletonDataById.TryGetValue(skeletonId, out var cached))
                    {
                        return cached;
                    }

                    var data = skeletonDefinitions.TryGetValue(skeletonId, out var definition)
                        ? DefinitionMapper.ToSkeletonData(definition)
                        : null;
                    skeletonDataById[skeletonId] = data;
                    return data;
                });

            context = new InstallContext(incomingPart, character, plan, partDefinitions, skeletonDefinitions);
            return true;
        }

        private static void CollectParts(
            IReadOnlyCollection<PartDefinition> definitions,
            Dictionary<string, PartDefinition> partDefinitions,
            Dictionary<string, SkeletonDefinition> skeletonDefinitions,
            List<PartData> equipped)
        {
            foreach (var definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                partDefinitions[definition.Id] = definition;
                RegisterSkeleton(skeletonDefinitions, definition.TargetSkeleton);
                equipped.Add(DefinitionMapper.ToPartData(definition));
            }
        }

        private static void RegisterSkeleton(
            Dictionary<string, SkeletonDefinition> skeletonDefinitions, SkeletonDefinition skeleton)
        {
            if (skeleton != null && !string.IsNullOrEmpty(skeleton.Id))
            {
                skeletonDefinitions[skeleton.Id] = skeleton;
            }
        }

        private sealed class InstallContext
        {
            public PartDefinition Part { get; }
            public IModularCharacter Character { get; }
            public BodyPlanChangePlan Plan { get; }
            public Dictionary<string, PartDefinition> PartDefinitionsById { get; }
            public Dictionary<string, SkeletonDefinition> SkeletonDefinitionsById { get; }

            public InstallContext(
                PartDefinition part,
                IModularCharacter character,
                BodyPlanChangePlan plan,
                Dictionary<string, PartDefinition> partDefinitionsById,
                Dictionary<string, SkeletonDefinition> skeletonDefinitionsById)
            {
                Part = part;
                Character = character;
                Plan = plan;
                PartDefinitionsById = partDefinitionsById;
                SkeletonDefinitionsById = skeletonDefinitionsById;
            }
        }
    }
}
