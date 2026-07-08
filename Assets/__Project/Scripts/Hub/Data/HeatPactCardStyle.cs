using UnityEngine;

namespace Hub.Data
{
    /// <summary>
    /// Presentation style for the Heat pact cards, mapped from the <c>HeatConfig</c> asset so the
    /// ember read is authored, not a magic colour in the presenter.
    /// </summary>
    public sealed class HeatPactCardStyle
    {
        public HeatPactCardStyle(Color pactCardTint)
        {
            PactCardTint = pactCardTint;
        }

        public Color PactCardTint { get; }
    }
}
