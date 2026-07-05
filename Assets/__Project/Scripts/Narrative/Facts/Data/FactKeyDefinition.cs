using Narrative.Facts.Core;
using UnityEngine;

namespace Narrative.Facts.Data
{
    /// <summary>
    /// ScriptableObject declaring one fact key — the single source of truth for the fact vocabulary
    /// (R13). Configuration data only; no logic beyond producing its Core <see cref="FactKeyInfo"/>.
    /// Namespace is a grouping label; subject arity comes from <see cref="FactScope"/> (A1).
    /// </summary>
    [CreateAssetMenu(fileName = "FactKey", menuName = "Narrative/Facts/Fact Key")]
    public class FactKeyDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private FactNamespace _namespace = FactNamespace.World;
        [Tooltip("Subject arity. Global = no subject; PerActor/PerFaction/PerLocation = scoped by that id.")]
        [SerializeField] private FactScope _scope = FactScope.Global;
        [Tooltip("Bare key name, e.g. 'pass_cleared' (no namespace prefix)")]
        [SerializeField] private string _key = string.Empty;
        [Tooltip("Lifetime horizon (D20). Run = resets on death (default); Meta = persists across runs.")]
        [SerializeField] private FactHorizon _horizon = FactHorizon.Run;

        [Header("Typing")]
        [SerializeField] private FactValueType _valueType = FactValueType.Bool;
        [SerializeField] private bool _defaultBool;
        [SerializeField] private int _defaultInt;
        [SerializeField] private float _defaultFloat;
        [SerializeField] private string _defaultString = string.Empty;

        [Header("Documentation")]
        [TextArea]
        [SerializeField] private string _description = string.Empty;

        public FactNamespace Namespace => _namespace;
        public FactScope Scope => _scope;
        public string Key => _key;
        public FactValueType ValueType => _valueType;
        public FactHorizon Horizon => _horizon;

        public FactValue DefaultValue =>
            FactValueConversion.Build(_valueType, _defaultBool, _defaultInt, _defaultFloat, _defaultString);

        public FactKeyInfo ToInfo() => new FactKeyInfo(_namespace, _key, _scope, _valueType, DefaultValue, _horizon);
    }
}
