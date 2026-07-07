using Combat.Core;
using Combat.Data.Definitions;

namespace UI.AbilityPreview
{
    /// <summary>
    /// What the shared ability-preview popover shows (G4 req 12 / mutation-cards FR5): the
    /// text pair plus everything the 3D stage needs to demonstrate the cast — shape for the
    /// cell sweep, the animator trigger for the pose. A passive shows an idle hero, no sweep.
    /// </summary>
    public class AbilityPreviewData
    {
        public string Name { get; }
        public string Description { get; }
        public bool IsPassive { get; }
        public bool IsLine { get; }
        public int LineLength { get; }
        public int RingRadius { get; }
        public string AnimationTrigger { get; }

        public AbilityPreviewData(
            string name,
            string description,
            bool isPassive,
            bool isLine,
            int lineLength,
            int ringRadius,
            string animationTrigger)
        {
            Name = name;
            Description = description;
            IsPassive = isPassive;
            IsLine = isLine;
            LineLength = lineLength;
            RingRadius = ringRadius;
            AnimationTrigger = animationTrigger;
        }

        public static AbilityPreviewData FromAbility(AbilityDefinition ability)
        {
            // FR9: the popover names the status the ability applies — same grammar everywhere.
            var description = ability.Description;
            if (Combat.Data.StatusEffectPreviewText.TryDescribeApplied(ability, out var statusLine))
                description = string.IsNullOrEmpty(description) ? statusLine : $"{description}\n{statusLine}";

            return new AbilityPreviewData(
                ability.Name,
                description,
                isPassive: false,
                isLine: ability.Shape == AbilityShapeType.Line,
                lineLength: ability.LineLength,
                ringRadius: ability.RingRadius,
                animationTrigger: ability.AnimationTrigger);
        }

        public static AbilityPreviewData FromPassive(PassiveAbilityDefinition passive)
        {
            var description = passive.Description;
            if (Combat.Data.StatusEffectPreviewText.TryDescribeStanding(passive, out var statusLine))
                description = string.IsNullOrEmpty(description) ? statusLine : $"{description}\n{statusLine}";

            return new AbilityPreviewData(
                passive.Name,
                description,
                isPassive: true,
                isLine: false,
                lineLength: 0,
                ringRadius: 0,
                animationTrigger: string.Empty);
        }
    }
}
