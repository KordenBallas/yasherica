using System;
using System.Collections.Generic;

namespace CharacterSystem.Runtime
{
    /// <summary>
    /// What the player must confirm before a body-plan change that sheds parts
    /// (body-plan-skeleton-swap.md FR7): the frame the body will re-form into and the
    /// friendly names of the parts that will come off (in stable slot order).
    /// </summary>
    public sealed class BodyPlanChangeSummary
    {
        public string TargetFrameName { get; }
        public IReadOnlyList<string> ShedPartNames { get; }

        public BodyPlanChangeSummary(string targetFrameName, IReadOnlyList<string> shedPartNames)
        {
            TargetFrameName = targetFrameName;
            ShedPartNames = shedPartNames ?? Array.Empty<string>();
        }
    }
}
