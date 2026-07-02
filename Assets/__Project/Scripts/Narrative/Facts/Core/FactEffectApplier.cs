using System.Collections.Generic;
using Core.Logging;

namespace Narrative.Facts.Core
{
    /// <summary>
    /// Default <see cref="IFactEffectApplier"/>. Order of guards per write:
    /// <list type="number">
    /// <item><b>Footprint (W2-1/W3-3/W4-1):</b> the effect's shape — compared by namespace + key +
    /// the *unresolved* subject token — must be permitted by the writing fragment's footprint, else
    /// skip + warn. Detached quest/combat effects carry their own footprint, so they validate even
    /// after a dialogue session ends.</item>
    /// <item><b>op×type legality (B5):</b> Add is numeric-only, Toggle is Bool-only; illegal → skip + warn.</item>
    /// <item><b>Resolve + write:</b> resolve the subject token, then Set/Add/Toggle/Remove.</item>
    /// </list>
    /// A play-time <paramref name="runtimeOverride"/> supplies the exact value while the declared
    /// footprint still gates the target (R7).
    /// </summary>
    public sealed class FactEffectApplier : IFactEffectApplier
    {
        private readonly ISubjectResolver _subjects;
        private readonly IGameLogger _logger;

        public FactEffectApplier(ISubjectResolver subjects, IGameLogger logger = null)
        {
            _subjects = subjects;
            _logger = logger;
        }

        public void ApplyAll(IReadOnlyList<FactEffectCore> effects, IFactStore store, ISubjectContext context,
            IReadOnlyCollection<FactKeyShapeCore> footprint)
        {
            if (effects == null)
            {
                return;
            }

            for (int i = 0; i < effects.Count; i++)
            {
                Apply(effects[i], store, context, footprint);
            }
        }

        public bool Apply(FactEffectCore effect, IFactStore store, ISubjectContext context,
            IReadOnlyCollection<FactKeyShapeCore> footprint, FactValue? runtimeOverride = null)
        {
            if (effect == null || store == null)
            {
                return false;
            }

            if (!IsWithinFootprint(effect.Shape, footprint))
            {
                _logger?.Warning(LogCategory.Narrative,
                    $"[FactEffectApplier] Write '{effect.Shape}' is outside the fragment footprint - rejected (fail closed).");
                return false;
            }

            if (!IsLegal(effect.Op, effect.Value.Type))
            {
                _logger?.Warning(LogCategory.Narrative,
                    $"[FactEffectApplier] Illegal op {effect.Op} for type {effect.Value.Type} on '{effect.Key}' - skipped.");
                return false;
            }

            var value = effect.Value;
            if (runtimeOverride.HasValue)
            {
                if (runtimeOverride.Value.Type != effect.Value.Type)
                {
                    _logger?.Warning(LogCategory.Narrative,
                        $"[FactEffectApplier] Runtime override type {runtimeOverride.Value.Type} disagrees with declared " +
                        $"{effect.Value.Type} on '{effect.Key}' - skipped.");
                    return false;
                }

                value = runtimeOverride.Value;
            }

            if (!_subjects.TryResolve(effect.SubjectToken, context, out var subject))
            {
                return false; // resolver already warned
            }

            var key = new FactKey(effect.Namespace, subject, effect.Key);

            switch (effect.Op)
            {
                case FactEffectOp.Set:
                    store.Set(key, value);
                    return true;
                case FactEffectOp.Add:
                    return ApplyAdd(store, key, value);
                case FactEffectOp.Toggle:
                    store.Set(key, FactValue.FromBool(!store.GetOrDefault(key, FactValue.FromBool(false)).AsBool()));
                    return true;
                case FactEffectOp.Remove:
                    store.Remove(key);
                    return true;
                default:
                    return false;
            }
        }

        private static bool ApplyAdd(IFactStore store, FactKey key, FactValue delta)
        {
            if (delta.Type == FactValueType.Int)
            {
                long current = store.GetOrDefault(key, FactValue.FromInt(0)).AsInt();
                store.Set(key, FactValue.FromInt(current + delta.AsInt()));
                return true;
            }

            double currentF = store.GetOrDefault(key, FactValue.FromFloat(0d)).AsFloat();
            store.Set(key, FactValue.FromFloat(currentF + delta.AsFloat()));
            return true;
        }

        private static bool IsWithinFootprint(FactKeyShapeCore write, IReadOnlyCollection<FactKeyShapeCore> footprint)
        {
            if (footprint == null)
            {
                return false;
            }

            foreach (var shape in footprint)
            {
                if (shape.Permits(write))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsLegal(FactEffectOp op, FactValueType type)
        {
            switch (op)
            {
                case FactEffectOp.Set:
                case FactEffectOp.Remove:
                    return true;
                case FactEffectOp.Add:
                    return type == FactValueType.Int || type == FactValueType.Float;
                case FactEffectOp.Toggle:
                    return type == FactValueType.Bool;
                default:
                    return false;
            }
        }
    }
}
