using UnityEngine;

namespace Combat.Data.Definitions
{
    /// <summary>
    /// A passive (always-on) ability granted by a body part. Unlike active abilities it is
    /// never queued or aimed: the referenced status effect is applied to the unit as a
    /// standing modifier for the whole combat (see CharacterCombatInitializer, which applies
    /// it with an infinite duration).
    ///
    /// Contains ONLY configuration data - NO logic. The standing modifier is built from the
    /// referenced StatusEffectDefinition via StatusEffectFactory.
    /// </summary>
    [CreateAssetMenu(fileName = "PassiveAbilityDefinition", menuName = "Combat/Abilities/Passive Ability")]
    public class PassiveAbilityDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private int _id;
        [SerializeField] private string _name;
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        [Header("Standing modifier applied for the whole combat")]
        [Tooltip("The Buff/Debuff status effect this passive applies. Its authored duration is ignored; passives last the entire combat.")]
        [SerializeField] private StatusEffectDefinition _modifier;

        [Header("Visual")]
        [SerializeField] private Sprite _icon;

        public int Id => _id;
        public string Name => _name;
        public string Description => _description;
        public StatusEffectDefinition Modifier => _modifier;
        public Sprite Icon => _icon;
    }
}
